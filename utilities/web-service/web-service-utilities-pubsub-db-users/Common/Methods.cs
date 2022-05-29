/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json.Linq;
using CloudServiceUtilities;
using CommonUtilities;
using WebServiceUtilities.Common;

namespace WebServiceUtilities.PubSubDatabaseUsers
{
    public static class Methods
    {
        public static bool GenerateNonExistentUniqueID(
            WebServiceBaseTimeoutable _Request,
            IDatabaseServiceInterface _DatabaseService,
            string _TableName,
            string _TableKeyName,
            string[] _TableEntryMustHaveProperties,
            EGetClearance _GetClearance,
            out string _GeneratedUniqueID,
            out WebServiceResponse _FailureResponse,
            Action<string> _ErrorMessageAction = null)
        {
            _GeneratedUniqueID = null;
            _FailureResponse = WebResponse.InternalError("");

            int ExistenceTrial = 0;

            while (_GeneratedUniqueID == null && ExistenceTrial < 3)
            {
                if (!Utility.CalculateStringMD5(Utility.RandomString(32, false), out _GeneratedUniqueID, _ErrorMessageAction))
                {
                    _FailureResponse = WebResponse.InternalError("Hashing operation has failed.");
                    return false;
                }

                if (_GetClearance == EGetClearance.Yes && !Controller_AtomicDBOperation.Get().GetClearanceForDBOperation(_Request.InnerProcessor, _TableName, _GeneratedUniqueID, _ErrorMessageAction))
                {
                    _FailureResponse = WebResponse.InternalError("Atomic operation control has failed.");
                    return false;
                }

                if (!_DatabaseService.GetItem(
                    _TableName,
                    _TableKeyName,
                    new PrimitiveType(_GeneratedUniqueID),
                    _TableEntryMustHaveProperties,
                    out JObject ExistenceCheck,
                    _ErrorMessageAction))
                {
                    _FailureResponse = WebResponse.InternalError("Database existence check operation has failed.");
                    return false;
                }
                if (ExistenceCheck != null)
                {
                    if (_GetClearance == EGetClearance.Yes)
                    {
                        Controller_AtomicDBOperation.Get().SetClearanceForDBOperationForOthers(_Request.InnerProcessor, _TableName, _GeneratedUniqueID, _ErrorMessageAction);
                    }

                    _GeneratedUniqueID = null;
                    ExistenceTrial++;
                }
                else break;
            }
            if (_GeneratedUniqueID == null)
            {
                _FailureResponse = WebResponse.InternalError("Unique model ID generation operation has failed.");
                return false;
            }
            return true;
        }
    }
}
