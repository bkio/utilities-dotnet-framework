/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebServiceUtilities.PubSubUsers
{
    public abstract class PubSubAction_UserAction : PubSubAction
    {
        [JsonProperty("userId")]
        public string UserID;
    }

    public class PubSubAction_UserCreated : PubSubAction_UserAction
    {
        public PubSubAction_UserCreated() {}
        public PubSubAction_UserCreated(string _UserID, string _UserEmail, string _UserName)
        {
            UserID = _UserID;
            UserEmail = _UserEmail;
            UserName = _UserName;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_UserCreated Casted &&
                       UserID == Casted.UserID &&
                       UserEmail == Casted.UserEmail &&
                       UserName == Casted.UserName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserID, UserEmail, UserName);
        }

        [JsonProperty("userEmail")]
        public string UserEmail;

        [JsonProperty("userName")]
        public string UserName;

        //Default Instance
        public static readonly PubSubAction_UserCreated DefaultInstance = new PubSubAction_UserCreated();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_USER_CREATED;
        }
    }

    public class PubSubAction_UserDeleted : PubSubAction_UserAction
    {
        public PubSubAction_UserDeleted() {}
        public PubSubAction_UserDeleted(string _UserID, string _UserEmail, string _UserName, List<string> _UserModels, List<string> _UserModelNames, List<string> _UserSharedModels, List<string> _UserSharedModelNames)
        {
            UserID = _UserID;
            UserEmail = _UserEmail;
            UserName = _UserName;
            UserModels = _UserModels;
            UserModelNames = _UserModelNames;
            UserSharedModels = _UserSharedModels;
            UserSharedModelNames = _UserSharedModelNames;
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is PubSubAction_UserDeleted Casted)) return false;

            return
                UserID == Casted.UserID &&
                UserEmail == Casted.UserEmail &&
                UserName == Casted.UserName &&
                UserModels.OrderBy(t => t).SequenceEqual(Casted.UserModels.OrderBy(t => t)) &&
                UserModelNames.OrderBy(t => t).SequenceEqual(Casted.UserModelNames.OrderBy(t => t)) &&
                UserSharedModels.OrderBy(t => t).SequenceEqual(Casted.UserSharedModels.OrderBy(t => t)) &&
                UserSharedModelNames.OrderBy(t => t).SequenceEqual(Casted.UserSharedModelNames.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            string CombinedModelIDs = "";
            foreach (var ModelID in UserModels)
            {
                CombinedModelIDs += ModelID;
            }
            string CombinedModelNames = "";
            foreach (var ModelName in UserModelNames)
            {
                CombinedModelNames += ModelName;
            }
            string CombinedSharedModelIDs = "";
            foreach (var SharedModelID in UserSharedModels)
            {
                CombinedSharedModelIDs += SharedModelID;
            }
            string CombinedSharedModelNames = "";
            foreach (var SharedModelName in UserSharedModelNames)
            {
                CombinedSharedModelNames += SharedModelName;
            }
            return HashCode.Combine(UserID, UserEmail, UserName, CombinedModelIDs, CombinedModelNames, CombinedSharedModelIDs, CombinedSharedModelNames);
        }

        [JsonProperty("userEmail")]
        public string UserEmail;

        [JsonProperty("userName")]
        public string UserName;

        [JsonProperty("userModels")]
        public List<string> UserModels = new List<string>();

        [JsonProperty("userModelNames")]
        public List<string> UserModelNames = new List<string>();

        [JsonProperty("userSharedModels")]
        public List<string> UserSharedModels = new List<string>();
        
        [JsonProperty("userSharedModelNames")]
        public List<string> UserSharedModelNames = new List<string>();

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_USER_DELETED;
        }

        //Default Instance
        public static readonly PubSubAction_UserDeleted DefaultInstance = new PubSubAction_UserDeleted();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_UserUpdated : PubSubAction_UserAction
    {
        public PubSubAction_UserUpdated() { }
        public PubSubAction_UserUpdated(
            string _UserID, 
            string _OldUserEmail, 
            string _NewUserEmail, 
            string _OldUserName, 
            string _NewUserName,
            JObject _ChangesObject)
        {
            UserID = _UserID;
            OldUserEmail = _OldUserEmail ?? "";
            NewUserEmail = _NewUserEmail ?? "";
            OldUserName = _OldUserName ?? "";
            NewUserName = _NewUserName ?? "";
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is PubSubAction_UserUpdated Casted &&
                UserID == Casted.UserID &&
                NewUserEmail == Casted.NewUserEmail &&
                NewUserName == Casted.NewUserName &&
                OldUserEmail == Casted.OldUserEmail &&
                OldUserName == Casted.OldUserName &&
                ChangesObject.ToString() == Casted.ChangesObject.ToString();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserID, NewUserEmail, NewUserName, OldUserEmail, OldUserName);
        }

        [JsonProperty("oldUserEmail")]
        public string OldUserEmail;

        [JsonProperty("newUserEmail")]
        public string NewUserEmail;

        [JsonProperty("oldUserName")]
        public string OldUserName;

        [JsonProperty("newUserName")]
        public string NewUserName;

        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        //Default Instance
        public static readonly PubSubAction_UserUpdated DefaultInstance = new PubSubAction_UserUpdated();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_USER_UPDATED;
        }
    }
}