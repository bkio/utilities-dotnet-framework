/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Threading;
using CloudServiceUtilities;
using CommonUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebServiceUtilities.PubSubUsers;

namespace WebServiceUtilities.PubSubDatabaseUsers
{
    public class Controller_AtomicDBOperation
    {
        private class PubSubTimeoutSubscriber : IPubSubSubscriberInterface
        {
            private readonly Action<PubSubAction_OperationTimeout> OnNotificationReceivedAction;

            private PubSubTimeoutSubscriber() {}
            public PubSubTimeoutSubscriber(Action<PubSubAction_OperationTimeout> _OnNotificationReceivedAction)
            {
                OnNotificationReceivedAction = _OnNotificationReceivedAction;
            }

            public void OnMessageReceived(JObject _Message)
            {
                OnNotificationReceivedAction?.Invoke(JsonConvert.DeserializeObject<PubSubAction_OperationTimeout>(_Message.ToString()));
            }
        }

        private static Controller_AtomicDBOperation Instance = null;
        private Controller_AtomicDBOperation() { }
        public static Controller_AtomicDBOperation Get()
        {
            if (Instance == null)
            {
                Instance = new Controller_AtomicDBOperation();
            }
            return Instance;
        }

        private IMemoryServiceInterface MemoryService;
        private string MemoryScopeKey;
        public void SetMemoryService(IMemoryServiceInterface _MemoryService, string _MemoryScopeKey)
        {
            MemoryService = _MemoryService;
            MemoryScopeKey = _MemoryScopeKey;
        }
        public string GetMemoryScopeKey()
        {
            return MemoryScopeKey;
        }

        private const string ATOMIC_DB_OP_CTRL_MEM_PREFIX = "atomic-db-op-mem-check-";
        private const int TIMEOUT_TRIAL_SECONDS = 10;

        //In case of false return, operation shall be cancelled with an internal error.
        public bool GetClearanceForDBOperation(WebServiceBaseTimeoutableProcessor _ServiceProcessor, string _DBTableName, string _Identifier, Action<string> _ErrorMessageAction = null)
        {
            if (_ServiceProcessor.IsDoNotGetDBClearanceSet()) return true;

            var CreatedAction = new PubSubAction_OperationTimeout(_DBTableName, _Identifier);
            lock (_ServiceProcessor.RelevantTimeoutStructures)
            {
                bool bFound = false;
                foreach (var CTS in _ServiceProcessor.RelevantTimeoutStructures)
                {
                    if (CTS.Equals(CreatedAction))
                    {
                        CreatedAction = CTS;
                        bFound = true;
                        break;
                    }
                }
                if (!bFound)
                {
                    _ServiceProcessor.RelevantTimeoutStructures.Add(CreatedAction);
                }
            }

            var MemoryEntryValue = $"{ATOMIC_DB_OP_CTRL_MEM_PREFIX}{_DBTableName}-{_Identifier}";

            int TrialCounter = 0;

            bool bResult;
            do
            {
                bResult = MemoryService.SetKeyValueConditionally(
                    MemoryScopeKey, 
                    new Tuple<string, PrimitiveType>(MemoryEntryValue, new PrimitiveType("busy")), 
                    _ErrorMessageAction);

                if (!bResult) Thread.Sleep(1000);
            }
            while (!bResult && TrialCounter++ < TIMEOUT_TRIAL_SECONDS);

            if (TrialCounter >= TIMEOUT_TRIAL_SECONDS)
            {
                _ErrorMessageAction?.Invoke($"Atomic DB Operation Controller->GetClearanceForDBOperation: A timeout has occured for operation type { _DBTableName}, for ID { _Identifier}, existing operation has been overriden by the new request.");

                Manager_PubSubService.Get().PublishAction(PubSubActions.EAction.ACTION_OPERATION_TIMEOUT, JsonConvert.SerializeObject(new PubSubAction_OperationTimeout()
                {
                    TableName = _DBTableName,
                    EntryKey = _Identifier
                }),
                _ErrorMessageAction);

                //Timeout for other operation has occurred.
                return MemoryService.SetKeyValue(MemoryScopeKey, new Tuple<string, PrimitiveType>[]
                {
                    new Tuple<string, PrimitiveType>(MemoryEntryValue, new PrimitiveType("busy"))
                }, 
                _ErrorMessageAction);
            }

            return true;
        }

        public void SetClearanceForDBOperationForOthers(WebServiceBaseTimeoutableProcessor _ServiceProcessor, string _DBTableName, string _Identifier, Action<string> _ErrorMessageAction = null)
        {
            if (_ServiceProcessor.IsDoNotGetDBClearanceSet()) return;

            var MemoryEntryValue = $"{ATOMIC_DB_OP_CTRL_MEM_PREFIX}{_DBTableName}-{_Identifier}";

            if (!MemoryService.DeleteKey(MemoryScopeKey, MemoryEntryValue, _ErrorMessageAction))
            {
                _ErrorMessageAction?.Invoke($"Atomic DB Operation Controller->SetClearanceForDBOperationForOthers: DeleteKey failed for operation type { _DBTableName}, for ID { _Identifier}");
            }
        }

        private PubSubTimeoutSubscriber Subscriber = null;
        public void StartTimeoutCheckOperation(Action<PubSubAction_OperationTimeout> _OnNotificationReceivedAction)
        {
            Subscriber = new PubSubTimeoutSubscriber(_OnNotificationReceivedAction);
            Manager_PubSubService.Get().AddSubscriber(Subscriber, PubSubActions.EAction.ACTION_OPERATION_TIMEOUT);
        }
    }
}