/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CloudServiceUtilities;
using CommonUtilities;
using Newtonsoft.Json.Linq;

namespace CloudServiceUtilitiesTest.Tests
{
    public class MemoryPubSubServiceTest
    {
        private readonly IMemoryServiceInterface SelectedMemoryService;

        private readonly string MemoryScopeKey;
        private readonly string ExpireMemoryScopeKey;

        private readonly Action<string> PrintAction;

        public MemoryPubSubServiceTest(IMemoryServiceInterface _MemoryService, Action<string> _PrintAction)
        {
            SelectedMemoryService = _MemoryService;

            MemoryScopeKey = "test";
            ExpireMemoryScopeKey = "expiretest";

            PrintAction = _PrintAction;
        }

        private void PreCleanup()
        {
            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                MemoryScopeKey,
                null);

            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                ExpireMemoryScopeKey,
                null);

            SelectedMemoryService.DeleteAllKeys(
                MemoryScopeKey,
                true,
                null,
                false);

            SelectedMemoryService.DeleteAllKeys(
                ExpireMemoryScopeKey,
                true,
                null,
                false);

            SelectedMemoryService.EmptyList(
                MemoryScopeKey, 
                "TestList1", 
                true, 
                null,
                false);

            //To not receive subscription messages for actions above
            Thread.Sleep(1000);
        }

        private List<string> SubscribeMessages = new List<string>();
        private readonly Atomicable<int> SubscribeDoneCounter = new Atomicable<int>(0, EProducerStatus.SingleProducer);
        private readonly int NumberOfExpectedSubscriptionMessages = 11;
        private readonly int MaxWaitSecondsForAllSubscriptionMessages = 10;
        private readonly Atomicable<bool> FailureStatus = new Atomicable<bool>(false, EProducerStatus.MultipleProducer);

        public bool Start()
        {
            PrintAction?.Invoke($"MemoryPubSubServiceTest->Info-> Test is starting.");

            if (SelectedMemoryService == null)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->Error-> Given SelectedMemoryService is null.");
                return false;
            }

            if (!SelectedMemoryService.HasInitializationSucceed())
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->Error-> Initialization failed.");
                return false;
            }

            if (SelectedMemoryService.GetPubSubService() == null)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->Error-> Given PubSubService is null.");
                return false;
            }

            SelectedMemoryService.GetPubSubService().EnsureUniqueMessageDelivery(SelectedMemoryService, PrintAction);

            PrintAction?.Invoke($"MemoryPubSubServiceTest->Log-> Initialization succeed.");

            PreCleanup();

            SelectedMemoryService.GetPubSubService().Subscribe(
                MemoryScopeKey,
                (string Topic, JObject Message) =>
                {
                    lock (SubscribeDoneCounter.Monitor)
                    {
                        SubscribeMessages.Add($"MemoryPubSubServiceTest->Sub-> {Topic}-> {Message}");
                        SubscribeDoneCounter.Set(SubscribeDoneCounter.Get() + 1);
                    }
                },
                (string Message) =>
                {
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->Sub->Error-> {Message}");
                    FailureStatus.Set(true);
                });

            bool bLocalResult = SelectedMemoryService.SetKeyValue(
                MemoryScopeKey,
                new Tuple<string, PrimitiveType>[]
                {
                    new Tuple<string, PrimitiveType>("TestKey1", new PrimitiveType(123)),
                    new Tuple<string, PrimitiveType>("TestKey2", new PrimitiveType(123.00400422f)),
                    new Tuple<string, PrimitiveType>("TestKey3", new PrimitiveType("TestVal3"))
                },
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-1->Error-> {Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-1 has succeed.");

            var LocalResult_1 = SelectedMemoryService.GetAllKeyValues(
                MemoryScopeKey,
                (string Message) => PrintAction?.Invoke($"MemoryPubSubServiceTest->GetAllKeyValues->Error-> { Message}"));
            if (LocalResult_1 == null || LocalResult_1.Length != 3)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetAllKeyValues-1 has failed. IsNull: {(LocalResult_1 == null)}, Length: {LocalResult_1?.Length}");
                return false;
            }
            foreach (var Tuple_1 in LocalResult_1)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetAllKeyValues-1->{ Tuple_1.Item1}, Length: {Tuple_1.Item2}");
            }

            bLocalResult = SelectedMemoryService.DeleteAllKeys(
                MemoryScopeKey,
                true,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteAllKeys-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteAllKeys-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteAllKeys-1 has succeed.");

            bLocalResult = SelectedMemoryService.SetKeyValue(
                MemoryScopeKey,
                new Tuple<string, PrimitiveType>[]
                {
                    new Tuple<string, PrimitiveType>("TestKey1", new PrimitiveType(123)),
                    new Tuple<string, PrimitiveType>("TestKey2", new PrimitiveType(123.00400422f)),
                    new Tuple<string, PrimitiveType>("TestKey3", new PrimitiveType("TestVal3"))
                },
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-2->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-2 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-2 has succeed.");

            bLocalResult = true;
            long KeysCount = SelectedMemoryService.GetKeysCount(
                MemoryScopeKey,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-1 has failed.");
                return false;
            }
            if (KeysCount != 3)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-1 expected 3, but result is: { KeysCount}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-1 has succeed.");

            bLocalResult = true;
            SelectedMemoryService.IncrementKeyValues(
                MemoryScopeKey,
                new Tuple<string, long>[]
                {
                    new Tuple<string, long>("TestKey1", 100)
                },
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->IncrementKeyValues-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->IncrementKeyValues-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->IncrementKeyValues-1 has succeed.");

            bLocalResult = true;
            var Expected1223 = SelectedMemoryService.IncrementKeyByValueAndGet(MemoryScopeKey,
                new Tuple<string, long>("TestKey1", 1000),
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->IncrementKeyByValueAndGet-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->IncrementKeyByValueAndGet-1 has failed.");
                return false;
            }
            if (Expected1223 != 1223)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->IncrementKeyByValueAndGet-1 expected 1223, but result is: { Expected1223}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->IncrementKeyByValueAndGet-1 has succeed.");

            bLocalResult = SelectedMemoryService.PushToListHead(MemoryScopeKey, "TestList1", new PrimitiveType[]
            {
                new PrimitiveType(123),
                new PrimitiveType(234)
            },
            false,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->PushToListHead-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->PushToListHead-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->PushToListHead-1 has succeed.");

            bLocalResult = SelectedMemoryService.PushToListTail(MemoryScopeKey, "TestList1", new PrimitiveType[]
            {
                new PrimitiveType(345),
                new PrimitiveType(456)
            },
            true,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->PushToListTail-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->PushToListTail-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->PushToListTail-1 has succeed.");

            bLocalResult = true;
            var GetAllElementsOfListResult_1 = SelectedMemoryService.GetAllElementsOfList(MemoryScopeKey, "TestList1",
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->GetAllElementsOfList-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetAllElementsOfList-1 has failed.");
                return false;
            }
            if (GetAllElementsOfListResult_1 == null || GetAllElementsOfListResult_1.Length != 4)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetAllElementsOfList-1 result is either null or length is unexpected. Length: {GetAllElementsOfListResult_1?.Length}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetAllElementsOfList-1 has succeed.");

            var PopFirstElementOfListResult_1 = SelectedMemoryService.PopFirstElementOfList(MemoryScopeKey, "TestList1", 
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->PopFirstElementOfList-1->Error-> { Message}");
                });
            if (!bLocalResult || PopFirstElementOfListResult_1 == null)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->PopFirstElementOfList-1 has failed. PopFirstElementOfListResult_1 null status: {(PopFirstElementOfListResult_1 == null)}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->PopFirstElementOfList-1 succeed. Result: {PopFirstElementOfListResult_1}");

            var PopLastElementOfListResult_1 = SelectedMemoryService.PopLastElementOfList(MemoryScopeKey, "TestList1",
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->PopLastElementOfList-1->Error-> { Message}");
                });
            if (!bLocalResult || PopLastElementOfListResult_1 == null)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->PopLastElementOfList-1 has failed. PopLastElementOfList_1 null status: {(PopLastElementOfListResult_1 == null)}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->PopLastElementOfList-1 succeed. Result: {PopLastElementOfListResult_1}");

            bLocalResult = true;
            var ListSizeResult_1 = SelectedMemoryService.ListSize(MemoryScopeKey, "TestList1", 
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-1 has failed.");
                return false;
            }
            if (ListSizeResult_1 != 2)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-1: Expected result is 2, but returned: { ListSizeResult_1}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-1 has succeed.");

            bLocalResult = SelectedMemoryService.ListContains(MemoryScopeKey, "TestList1", new PrimitiveType(123),
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->ListContains-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->ListContains-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->ListContains-1 has succeed.");

            bLocalResult = SelectedMemoryService.EmptyList(MemoryScopeKey, "TestList1", true,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->EmptyList-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->EmptyList-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->EmptyList-1 has succeed.");

            bLocalResult = true;
            var ListSizeResult_2 = SelectedMemoryService.ListSize(MemoryScopeKey, "TestList1",
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-2->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-2 has failed.");
                return false;
            }
            if (ListSizeResult_2 != 0)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-2: Expected result is 0, but returned: { ListSizeResult_1}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->ListSize-2 has succeed.");

            bLocalResult = true;
            var Expected1223Value = SelectedMemoryService.GetKeyValue(
                MemoryScopeKey,
                "TestKey1",
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-1 has failed.");
                return false;
            }
            if (Expected1223Value == null || Expected1223Value.Type != EPrimitiveTypeEnum.Integer || Expected1223Value.AsInteger != 1223)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-1 expected 1223, but result is: {Expected1223Value}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-1 has succeed.");

            bLocalResult = SelectedMemoryService.DeleteKey(
                MemoryScopeKey,
                "TestKey3",
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteKey-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteKey-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteKey-1 has succeed.");

            bLocalResult = SelectedMemoryService.DeleteKey(
                MemoryScopeKey,
                "TestKeyNonExistent",
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteKey-2->Error-> Expected: { Message}");
                });
            if (bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteKey-2 did not fail.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteKey-2 has successfully failed.");

            bLocalResult = true;
            KeysCount = SelectedMemoryService.GetKeysCount(
                MemoryScopeKey,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-2->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-2 has failed.");
                return false;
            }
            if (KeysCount != 2)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-2 expected 2, but result is: { KeysCount}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysCount-2 has succeed.");

            bLocalResult = true;
            var GetKeysResult = SelectedMemoryService.GetKeys(MemoryScopeKey,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeys-1->Error-> { Message}");
                });
            if (!bLocalResult || GetKeysResult == null || GetKeysResult.Length != 2)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeys-1 returned { GetKeysResult?.Length} results, but expected 2. Error status: {bLocalResult}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeys-1 has succeed.");

            bLocalResult = true;
            var GetKeysValuesResult = SelectedMemoryService.GetKeysValues(MemoryScopeKey, new List<string>(GetKeysResult),
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysValues-1->Error-> { Message}");
                });
            if (!bLocalResult || GetKeysValuesResult == null || GetKeysValuesResult.Count != 2)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysValues-1 returned {GetKeysValuesResult?.Count} results, but expected 2. Error status: {bLocalResult}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeysValues-1 has succeed.");

            int ElapsedSeconds = 0;
            while (SubscribeDoneCounter.Get() != NumberOfExpectedSubscriptionMessages && ElapsedSeconds < MaxWaitSecondsForAllSubscriptionMessages && !FailureStatus.Get())
            {
                Thread.Sleep(1000);
                ElapsedSeconds++;
            }
            if (FailureStatus.Get())
            {
                PrintAction?.Invoke($"Failure detected. Test failed.");
                return false;
            }
            if (SubscribeDoneCounter.Get() != NumberOfExpectedSubscriptionMessages)
            {
                PrintAction?.Invoke($"Subscription messages timed out or processed multiple time.");
                return false;
            }
            lock (SubscribeDoneCounter.Monitor)
            {
                SubscribeMessages = SubscribeMessages.OrderBy(q => q).ToList();
                foreach (var Message in SubscribeMessages)
                {
                    PrintAction?.Invoke($"Received message: { Message}");
                }
            }

            bLocalResult = true;
            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                MemoryScopeKey,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteTopicGlobally-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteTopicGlobally-1 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteTopicGlobally-1 has succeed.");

            bLocalResult = SelectedMemoryService.SetKeyValue(ExpireMemoryScopeKey, new Tuple<string, PrimitiveType>[]
            {
                new Tuple<string, PrimitiveType>("TestKey1", new PrimitiveType("TestValue1"))
            },
            (string Message) =>
            {
                bLocalResult = false;
                PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-ExpireTest->Error-> { Message}");
            });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-ExpireTest has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyValue-ExpireTest has succeed.");

            bLocalResult = SelectedMemoryService.GetKeyExpireTime(ExpireMemoryScopeKey, out TimeSpan TTL_1,
            (string Message) =>
            {
                bLocalResult = false;
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-1->Error-> Expected: { Message}");
            });
            if (bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-1 did not fail.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-1 successfully failed.");

            bLocalResult = SelectedMemoryService.SetKeyExpireTime(ExpireMemoryScopeKey, TimeSpan.FromSeconds(2.0f),
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyExpireTime-ExpireTest-1->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyExpireTime-ExpireTest-1 has failed.");
                return false;
            };
            PrintAction?.Invoke($"MemoryPubSubServiceTest->SetKeyExpireTime-ExpireTest-1 has succeed.");

            bLocalResult = SelectedMemoryService.GetKeyExpireTime(ExpireMemoryScopeKey, out TimeSpan TTL_2,
            (string Message) =>
            {
                bLocalResult = false;
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-2->Error-> { Message}");
            });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-2 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-2 has succeed.");

            bLocalResult = true;
            var ExpireGetKeyResult_1 = SelectedMemoryService.GetKeyValue(ExpireMemoryScopeKey, "TestKey1",
            (string Message) =>
            {
                bLocalResult = false;
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-ExpireTest-1->Error-> { Message}");
            });
            if (!bLocalResult || ExpireGetKeyResult_1 == null)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-ExpireTest-1 has failed.");
                return false;
            }
            if (ExpireGetKeyResult_1.AsString != "TestValue1")
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-ExpireTest-1 returned unexpected value: { ExpireGetKeyResult_1?.AsString}");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-ExpireTest-1 has succeed.");

            Thread.Sleep(2500);

            bLocalResult = SelectedMemoryService.GetKeyExpireTime(ExpireMemoryScopeKey, out TimeSpan TTL_3,
            (string Message) =>
            {
                bLocalResult = false;
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-3->Error-> Expected: { Message}");
            });
            if (bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-3 did not fail.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyExpireTime-ExpireTest-3 successfully failed.");

            var ExpireGetKeyResult_2 = SelectedMemoryService.GetKeyValue(ExpireMemoryScopeKey, "TestKey1",
            (string Message) =>
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-ExpireTest-2->Error-> { Message}");
            });
            if (ExpireGetKeyResult_2 != null)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-ExpireTest-2 did not fail.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->GetKeyValue-ExpireTest-2 successfully expired.");

            bLocalResult = true;
            SelectedMemoryService.GetPubSubService().DeleteTopicGlobally(
                ExpireMemoryScopeKey,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteTopicGlobally-2->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteTopicGlobally-2 has failed.");
                return false;
            }
            PrintAction?.Invoke($"MemoryPubSubServiceTest->DeleteTopicGlobally-2 has succeed.");

            return true;
        }
    }
}