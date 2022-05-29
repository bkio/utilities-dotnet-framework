﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebServiceUtilities.PubSubUsers
{
    ///<summary>
    ///
    /// When there is a new Action;
    /// 
    /// 1) Change microservice-dependency-map.cs also, if action is designed to invoke a service by pubsub and by a storage object change; change InvokedByStoragePubSubActions; otherwise InvokedByRegularPubSubActions
    /// 2) Search relevant PubSub_Part{No} in all terraform scripts; add the action in the correct places
    ///
    /// PubSub_Part1: Always
    /// PubSub_Part2: If action is designed to invoke a service by pubsub and by a storage object change 
    ///               (if service does not exist there yet; create the relevant part in the service terraform script)
    /// PubSub_Part3: If action is designed to invoke a service by pubsub (If Part2 is relevant; this must be relevant too)
    /// PubSub_Part4: If action is designed to invoke a service by pubsub (If Part2 is relevant; this must be relevant too)
    ///               (if service does not exist there yet; create the relevant part in the service terraform script);
    ///               (sometimes awaken services need to listen an action topic, so an action does not always have to invoke a service)
    ///               
    ///</summary>
    public static class PubSubActions
    {
        public enum EAction
        {
            NONE,
            ACTION_OPERATION_TIMEOUT,
            ACTION_USER_CREATED,
            ACTION_USER_DELETED,
            ACTION_USER_UPDATED,
            ACTION_MODEL_CREATED,
            ACTION_MODEL_DELETED,
            ACTION_MODEL_UPDATED,
            ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED,
            ACTION_MODEL_REVISION_CREATED,
            ACTION_MODEL_REVISION_DELETED,
            ACTION_MODEL_REVISION_UPDATED,
            ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED,
            ACTION_MODEL_REVISION_FILE_ENTRY_DELETED,
            ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL,
            ACTION_MODEL_REVISION_RAW_FILE_UPLOADED,
            ACTION_STORAGE_FILE_UPLOADED,
            ACTION_STORAGE_FILE_UPLOADED_CLOUDEVENT,
            ACTION_STORAGE_FILE_DELETED,
            ACTION_STORAGE_FILE_DELETED_CLOUDEVENT,
            ACTION_BATCH_PROCESS_FAILED,
            ACTION_BATCH_PROCESS_RERUN,
            ACTION_BATCH_START_CAD_PROCESS,
            ACTION_BATCH_STOP_CAD_PROCESS
        }

        //Deployment name and build number are appended to prefix by Manager_PubSubService;
        //Therefore this should only be called by Manager_PubSubService.SetDeploymentBranchNameAndBuildNumber() (Except microservice-dependency-map.cs for local debug run)
        public static readonly Dictionary<EAction, string> ActionStringPrefixMap = new Dictionary<EAction, string>()
        {
            [EAction.ACTION_OPERATION_TIMEOUT] = "operation_timeout_",
            [EAction.ACTION_USER_CREATED] = "user_created_",
            [EAction.ACTION_USER_DELETED] = "user_deleted_",
            [EAction.ACTION_USER_UPDATED] = "user_updated_",
            [EAction.ACTION_MODEL_CREATED] = "model_created_",
            [EAction.ACTION_MODEL_DELETED] = "model_deleted_",
            [EAction.ACTION_MODEL_UPDATED] = "model_updated_",
            [EAction.ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED] = "model_shared_user_changed_",
            [EAction.ACTION_MODEL_REVISION_CREATED] = "revision_created_",
            [EAction.ACTION_MODEL_REVISION_DELETED] = "revision_deleted_",
            [EAction.ACTION_MODEL_REVISION_UPDATED] = "revision_updated_",
            [EAction.ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED] = "rev_file_entry_updated_",
            [EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETED] = "rev_file_entry_deleted_",
            [EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL] = "rev_file_entry_delete_all_",
            [EAction.ACTION_MODEL_REVISION_RAW_FILE_UPLOADED] = "rev_raw_file_uploaded_",
            [EAction.ACTION_STORAGE_FILE_UPLOADED] = "storage_file_uploaded_",
            [EAction.ACTION_STORAGE_FILE_UPLOADED_CLOUDEVENT] = "storage_file_uploaded_ce_",
            [EAction.ACTION_STORAGE_FILE_DELETED] = "storage_file_deleted_",
            [EAction.ACTION_STORAGE_FILE_DELETED_CLOUDEVENT] = "storage_file_deleted_ce_",
            [EAction.ACTION_BATCH_PROCESS_FAILED] = "cad_process_batch_failed_",
            [EAction.ACTION_BATCH_PROCESS_RERUN] = "cad_process_batch_rerun_",
            [EAction.ACTION_BATCH_START_CAD_PROCESS] = "cad_process_batch_start_",
            [EAction.ACTION_BATCH_STOP_CAD_PROCESS] = "cad_process_batch_stop_"
        };

        public static PubSubAction DeserializeAction(EAction _IdentifiedAction, string _SerializedAction)
        {
            switch (_IdentifiedAction)
            {
                case EAction.ACTION_OPERATION_TIMEOUT:
                    return JsonConvert.DeserializeObject<PubSubAction_OperationTimeout>(_SerializedAction);
                case EAction.ACTION_USER_CREATED:
                    return JsonConvert.DeserializeObject<PubSubAction_UserCreated>(_SerializedAction);
                case EAction.ACTION_USER_DELETED:
                    return JsonConvert.DeserializeObject<PubSubAction_UserDeleted>(_SerializedAction);
                case EAction.ACTION_USER_UPDATED:
                    return JsonConvert.DeserializeObject<PubSubAction_UserUpdated>(_SerializedAction);
                case EAction.ACTION_MODEL_CREATED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelCreated>(_SerializedAction);
                case EAction.ACTION_MODEL_DELETED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelDeleted>(_SerializedAction);
                case EAction.ACTION_MODEL_UPDATED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelUpdated>(_SerializedAction);
                case EAction.ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelSharedWithUserIdsChanged>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_CREATED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelRevisionCreated>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_DELETED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelRevisionDeleted>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_UPDATED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelRevisionUpdated>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelRevisionFileEntryDeleted>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelRevisionFileEntryUpdated>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelRevisionFileEntryDeleteAll>(_SerializedAction);
                case EAction.ACTION_MODEL_REVISION_RAW_FILE_UPLOADED:
                    return JsonConvert.DeserializeObject<PubSubAction_ModelRevisionRawFileUploaded>(_SerializedAction);
                case EAction.ACTION_STORAGE_FILE_UPLOADED:
                    return JsonConvert.DeserializeObject<PubSubAction_StorageFileUploaded>(_SerializedAction);
                case EAction.ACTION_STORAGE_FILE_UPLOADED_CLOUDEVENT:
                    return JsonConvert.DeserializeObject<PubSubAction_StorageFileUploaded_CloudEventSchemaV1_0>(_SerializedAction);
                case EAction.ACTION_STORAGE_FILE_DELETED:
                    return JsonConvert.DeserializeObject<PubSubAction_StorageFileDeleted>(_SerializedAction);
                case EAction.ACTION_STORAGE_FILE_DELETED_CLOUDEVENT:
                    return JsonConvert.DeserializeObject<PubSubAction_StorageFileDeleted_CloudEventSchemaV1_0>(_SerializedAction);
                case EAction.ACTION_BATCH_PROCESS_FAILED:
                    return JsonConvert.DeserializeObject<PubSubAction_BatchProcessFailed>(_SerializedAction);
                case EAction.ACTION_BATCH_PROCESS_RERUN:
                    return JsonConvert.DeserializeObject<PubSubAction_BatchProcessRerun>(_SerializedAction);
                case EAction.ACTION_BATCH_START_CAD_PROCESS:
                    return JsonConvert.DeserializeObject<PubSubAction_BatchStartCADProcess>(_SerializedAction);
                case EAction.ACTION_BATCH_STOP_CAD_PROCESS:
                    return JsonConvert.DeserializeObject<PubSubAction_BatchStopCADProcess>(_SerializedAction);
                default:
                    break;
            }
            return null;
        }
    }
}