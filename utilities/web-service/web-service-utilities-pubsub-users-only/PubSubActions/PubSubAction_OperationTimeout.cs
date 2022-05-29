/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json;

namespace WebServiceUtilities.PubSubUsers
{
    public class PubSubAction_OperationTimeout : PubSubAction
    {
        public PubSubAction_OperationTimeout() { }
        public PubSubAction_OperationTimeout(string _TableName, string _EntryKey)
        {
            TableName = _TableName;
            EntryKey = _EntryKey;
        }

        public override bool Equals(object _Other)
        {
            PubSubAction_OperationTimeout Casted;
            try
            {
                Casted = (PubSubAction_OperationTimeout)_Other;
            }
            catch (Exception)
            {
                return false;
            }
            return
                TableName == Casted.TableName &&
                EntryKey == Casted.EntryKey;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TableName, EntryKey);
        }

        [JsonProperty("dbTableName")]
        public string TableName;

        [JsonProperty("dbEntryKey")]
        public string EntryKey;

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_OPERATION_TIMEOUT;
        }

        //Default Instance
        public static readonly PubSubAction_OperationTimeout DefaultInstance = new PubSubAction_OperationTimeout();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }
}