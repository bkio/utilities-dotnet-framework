/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebServiceUtilities.PubSubUsers
{
    public abstract class PubSubAction_ModelAction : PubSubAction
    {
        [JsonProperty("modelId")]
        public string ModelID;
        
        [JsonProperty("modelUniqueName")]
        public string ModelName;

        [JsonProperty("userId")]
        public string UserID;

        [JsonProperty("modelSharedWithUserIds")]
        public List<string> ModelSharedWithUserIDs = new List<string>();
    }

    public class PubSubAction_ModelCreated : PubSubAction_ModelAction
    {
        [JsonProperty("authMethodKey")]
        public string AuthMethodKey;

        public PubSubAction_ModelCreated() { }
        public PubSubAction_ModelCreated(string _ModelID, string _ModelName, string _UserID, List<string> _ModelSharedWithUserIDs, string _AuthMethodKey)
        {
            ModelID = _ModelID;
            ModelName = _ModelName;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            AuthMethodKey = _AuthMethodKey;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelCreated Casted &&
                    UserID == Casted.UserID &&
                    ModelID == Casted.ModelID &&
                    ModelName == Casted.ModelName &&
                    AuthMethodKey == Casted.AuthMethodKey &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, ModelName, UserID, AuthMethodKey, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_CREATED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelCreated DefaultInstance = new PubSubAction_ModelCreated();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelDeleted : PubSubAction_ModelAction
    {
        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;

        public PubSubAction_ModelDeleted() {}
        public PubSubAction_ModelDeleted(string _ModelID, string _ModelName, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            ModelName = _ModelName;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    ModelName == Casted.ModelName &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, ModelName, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_DELETED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelDeleted DefaultInstance = new PubSubAction_ModelDeleted();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelUpdated : PubSubAction_ModelAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;

        public PubSubAction_ModelUpdated() { }
        public PubSubAction_ModelUpdated(string _ModelID, string _ModelName, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            ModelName = _ModelName;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    ModelName == Casted.ModelName &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, ModelName, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_UPDATED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelUpdated DefaultInstance = new PubSubAction_ModelUpdated();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelSharedWithUserIdsChanged : PubSubAction_ModelAction
    {
        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;

        [JsonProperty("oldModelSharedWithUserIds")]
        public List<string> OldModelSharedWithUserIDs = new List<string>();

        public PubSubAction_ModelSharedWithUserIdsChanged() { }
        public PubSubAction_ModelSharedWithUserIdsChanged(string _ModelID, string _ModelName, string _UserID, List<string> _ModelSharedWithUserIDs, List<string> _OldModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            ModelName = _ModelName;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            OldModelSharedWithUserIDs = _OldModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelSharedWithUserIdsChanged Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    ModelName == Casted.ModelName &&
                    OldModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.OldModelSharedWithUserIDs.OrderBy(t => t)) &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, ModelName, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs, OldModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelSharedWithUserIdsChanged DefaultInstance = new PubSubAction_ModelSharedWithUserIdsChanged();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public abstract class PubSubAction_ModelRevisionAction : PubSubAction_ModelAction
    {
        [JsonProperty("revisionIndex")]
        public int RevisionIndex;

        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;
    }

    public class PubSubAction_ModelRevisionCreated : PubSubAction_ModelRevisionAction
    {
        public PubSubAction_ModelRevisionCreated() { }
        public PubSubAction_ModelRevisionCreated(string _ModelID, int _RevisionIndex, string _ModelOwnerUserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _ModelOwnerUserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelRevisionCreated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_REVISION_CREATED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelRevisionCreated DefaultInstance = new PubSubAction_ModelRevisionCreated();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelRevisionDeleted : PubSubAction_ModelRevisionAction
    {
        public PubSubAction_ModelRevisionDeleted() { }
        public PubSubAction_ModelRevisionDeleted(string _ModelID, int _RevisionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelRevisionDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_REVISION_DELETED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelRevisionDeleted DefaultInstance = new PubSubAction_ModelRevisionDeleted();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelRevisionUpdated : PubSubAction_ModelRevisionAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        public PubSubAction_ModelRevisionUpdated() { }
        public PubSubAction_ModelRevisionUpdated(string _ModelID, int _RevisionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelRevisionUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_REVISION_UPDATED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelRevisionUpdated DefaultInstance = new PubSubAction_ModelRevisionUpdated();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }
        
    public class PubSubAction_ModelRevisionFileEntryDeleted : PubSubAction_ModelRevisionAction
    {
        public PubSubAction_ModelRevisionFileEntryDeleted() { }
        public PubSubAction_ModelRevisionFileEntryDeleted(
            string _ModelID, 
            int _RevisionIndex, 
            string _UserID, 
            List<string> _ModelSharedWithUserIDs, 
            string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelRevisionFileEntryDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelRevisionFileEntryDeleted DefaultInstance = new PubSubAction_ModelRevisionFileEntryDeleted();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelRevisionFileEntryUpdated : PubSubAction_ModelRevisionAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        public PubSubAction_ModelRevisionFileEntryUpdated() { }
        public PubSubAction_ModelRevisionFileEntryUpdated(string _ModelID, int _RevisionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelRevisionFileEntryUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ChangesObject, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelRevisionFileEntryUpdated DefaultInstance = new PubSubAction_ModelRevisionFileEntryUpdated();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelRevisionFileEntryDeleteAll : PubSubAction_ModelRevisionAction
    {
        [JsonProperty("fileEntry")]
        public JObject Entry;

        public PubSubAction_ModelRevisionFileEntryDeleteAll() { }
        public PubSubAction_ModelRevisionFileEntryDeleteAll(
            string _ModelID, 
            string _ModelName,
            int _RevisionIndex, 
            string _UserID,
            List<string> _ModelSharedWithUserIDs,
            string _ActionPerformedByUserID,
            JObject _Entry)
        {
            ModelID = _ModelID;
            ModelName = _ModelName;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            Entry = _Entry;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelRevisionFileEntryDeleteAll Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    ModelName == Casted.ModelName &&
                    RevisionIndex == Casted.RevisionIndex &&
                    Entry == Casted.Entry &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, ModelName, RevisionIndex, UserID, ActionPerformedByUserID, Entry, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL;
        }

        //Default Instance
        public static readonly PubSubAction_ModelRevisionFileEntryDeleteAll DefaultInstance = new PubSubAction_ModelRevisionFileEntryDeleteAll();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_ModelRevisionRawFileUploaded : PubSubAction_ModelRevisionAction
    {
        [JsonProperty("model")]
        public JObject ModelObject = new JObject();

        public PubSubAction_ModelRevisionRawFileUploaded() { }
        public PubSubAction_ModelRevisionRawFileUploaded(
            string _ModelID, 
            int _RevisionIndex, 
            string _UserID, 
            List<string> _ModelSharedWithUserIDs, 
            string _ActionPerformedByUserID, 
            JObject _ModelObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            ModelObject = _ModelObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_ModelRevisionRawFileUploaded Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelObject.ToString() == Casted.ModelObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelObject, ModelSharedWithUserIDs);
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_MODEL_REVISION_RAW_FILE_UPLOADED;
        }

        //Default Instance
        public static readonly PubSubAction_ModelRevisionRawFileUploaded DefaultInstance = new PubSubAction_ModelRevisionRawFileUploaded();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }
}