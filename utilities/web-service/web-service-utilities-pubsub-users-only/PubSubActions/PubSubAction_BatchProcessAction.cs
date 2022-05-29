/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebServiceUtilities.PubSubUsers
{
    public abstract class PubSubAction_BatchProcessAction : PubSubAction
    {
    }

    public class PubSubAction_BatchProcessFailed : PubSubAction_BatchProcessAction
    {
        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_BATCH_PROCESS_FAILED;
        }

        public PubSubAction_BatchProcessFailed() { }

        [JsonProperty("modelId")]
        public string ModelId;

        [JsonProperty("modelUniqueName")]
        public string ModelName;

        [JsonProperty("revisionIndex")]
        public int RevisionIndex;

        [JsonProperty("statusMessage")]
        public string StatusMessage;

        public PubSubAction_BatchProcessFailed(string _ModelId, string _ModelName, int _RevisionIndex, string _StatusMessage)
        {
            ModelId = _ModelId;
            ModelName = _ModelName;
            RevisionIndex = _RevisionIndex;
            StatusMessage = _StatusMessage;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_BatchProcessFailed Casted &&
                    ModelId == Casted.ModelId &&
                    ModelName == Casted.ModelName &&
                    RevisionIndex == Casted.RevisionIndex &&
                    StatusMessage.Equals(Casted.StatusMessage);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelId, ModelName, RevisionIndex, StatusMessage);
        }

        //Default Instance
        public static readonly PubSubAction_BatchProcessFailed DefaultInstance = new PubSubAction_BatchProcessFailed();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_BatchProcessRerun : PubSubAction_BatchProcessAction
    {
        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_BATCH_PROCESS_RERUN;
        }

        public PubSubAction_BatchProcessRerun() { }

        [JsonProperty("modelId")]
        public string ModelId;

        [JsonProperty("modelUniqueName")]
        public string ModelName;

        [JsonProperty("revisionIndex")]
        public int RevisionIndex;

        [JsonProperty("rerunFromStage")]
        public int RerunFromStage;

        public PubSubAction_BatchProcessRerun(string _ModelId, string _ModelName, int _RevisionIndex, int _RerunFromStage)
        {
            ModelId = _ModelId;
            ModelName = _ModelName;
            RevisionIndex = _RevisionIndex;
            RerunFromStage = _RerunFromStage;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_BatchProcessRerun Casted &&
                    ModelId == Casted.ModelId &&
                    ModelName == Casted.ModelName &&
                    RevisionIndex == Casted.RevisionIndex &&
                    RerunFromStage == Casted.RerunFromStage;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelId, ModelName, RevisionIndex, RerunFromStage);
        }

        //Default Instance
        public static readonly PubSubAction_BatchProcessRerun DefaultInstance = new PubSubAction_BatchProcessRerun();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_BatchStartCADProcess : PubSubAction_BatchProcessAction
    {
        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_BATCH_START_CAD_PROCESS;
        }

        public PubSubAction_BatchStartCADProcess() { }

        [JsonProperty("modelId")]
        public string ModelId;

        [JsonProperty("fileConversionEntryStringified")]
        public string FileConversionEntry_Stringified;

        public PubSubAction_BatchStartCADProcess(string _ModelId, string _FileConversionEntry_Stringified)
        {
            ModelId = _ModelId;
            FileConversionEntry_Stringified = _FileConversionEntry_Stringified;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_BatchStartCADProcess Casted &&
                    ModelId == Casted.ModelId &&
                    FileConversionEntry_Stringified == Casted.FileConversionEntry_Stringified;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelId, FileConversionEntry_Stringified);
        }

        //Default Instance
        public static readonly PubSubAction_BatchStartCADProcess DefaultInstance = new PubSubAction_BatchStartCADProcess();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_BatchStopCADProcess : PubSubAction_BatchProcessAction
    {
        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_BATCH_STOP_CAD_PROCESS;
        }

        public PubSubAction_BatchStopCADProcess() { }

        /*
         * 0: None
         * 1: Stop all which satisfy conditionalVMStatus
         * 2: Stop by modelId-revisionIndex
         * 3: Stop by virtual machine unique id
         */
        [JsonProperty("stopChoiceToken")]
        public int StopChoiceToken = 0;

        [JsonProperty("conditionalVMStatus")]
        public List<int> ConditionalVMStatus = new List<int>();

        [JsonProperty("modelId")]
        public string ModelID;

        [JsonProperty("revisionIndex")]
        public int RevisionIndex;

        [JsonProperty("vmUniqueId")]
        public string VMUniqueId;

        public PubSubAction_BatchStopCADProcess(List<int> _ConditionalVMStatus)
        {
            StopChoiceToken = 1;
            ConditionalVMStatus = _ConditionalVMStatus;
        }
        public PubSubAction_BatchStopCADProcess(string _ModelID, int _RevisionIndex)
        {
            StopChoiceToken = 2;
            ModelID = _ModelID;
            RevisionIndex = _RevisionIndex;
        }
        public PubSubAction_BatchStopCADProcess(string _VMUniqueId)
        {
            StopChoiceToken = 3;
            VMUniqueId = _VMUniqueId;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_BatchStopCADProcess Casted &&
                    StopChoiceToken == Casted.StopChoiceToken &&
                    CheckEquality(ConditionalVMStatus, Casted.ConditionalVMStatus) &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VMUniqueId == Casted.VMUniqueId;
        }

        private static bool CheckEquality(List<int> _First, List<int> _Second)
        {
            if (_First == null)
            {
                if (_Second == null) return true;
                return false;
            }
            if (_Second == null) return false;
            return _First.OrderBy(t => t).SequenceEqual(_Second.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StopChoiceToken, ConditionalVMStatus == null ? "" : string.Join("-", ConditionalVMStatus), ModelID, RevisionIndex);
        }

        //Default Instance
        public static readonly PubSubAction_BatchStopCADProcess DefaultInstance = new PubSubAction_BatchStopCADProcess();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }
}