/// Copyright 2022- Burak Kara, All rights reserved.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebServiceUtilities.PubSubUsers
{
    public abstract class PubSubAction_StorageNotification : PubSubAction
    {
    }

    /// https://cloud.google.com/storage/docs/json_api/v1/objects#resource-representations
    public class PubSubAction_StorageFileUploaded : PubSubAction_StorageNotification
    {
        [JsonProperty("bucket")]
        public string BucketName;

        [JsonProperty("name")]
        public string RelativeUrl;

        [JsonProperty("size")]
        public ulong Size;

        [JsonProperty("md5Hash")]
        public string MD5Hash;

        [JsonProperty("crc32c")]
        public string CRC32C;

        [JsonProperty("etag")]
        public string ETag;

        [JsonProperty("contentType")]
        public string ContentType;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("bucket")
                && _Input.ContainsKey("name")
                && _Input.ContainsKey("size");
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_STORAGE_FILE_UPLOADED;
        }

        //Default Instance
        public static readonly PubSubAction_StorageFileUploaded DefaultInstance = new PubSubAction_StorageFileUploaded();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_StorageFileDeleted : PubSubAction_StorageNotification
    {
        [JsonProperty("bucket")]
        public string BucketName;

        [JsonProperty("name")]
        public string RelativeUrl;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("bucket")
                && _Input.ContainsKey("name");
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_STORAGE_FILE_DELETED;
        }

        //Default Instance
        public static readonly PubSubAction_StorageFileDeleted DefaultInstance = new PubSubAction_StorageFileDeleted();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_StorageFileUploaded_CloudEventSchemaV1_0 : PubSubAction_StorageNotification
    {
        [JsonProperty("api")]
        public string DataApi;

        [JsonProperty("url")]
        public string RelativeUrl;

        public string CompleteUrl;

        [JsonProperty("contentLength")]
        public ulong Size;

        [JsonProperty("eTag")]
        public string ETag;

        [JsonProperty("contentType")]
        public string ContentType;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("url")
                && _Input.ContainsKey("api") && _Input.GetValue("api").ToString().StartsWith("Put")
                && _Input.ContainsKey("contentLength");
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_STORAGE_FILE_UPLOADED_CLOUDEVENT;
        }

        public void ConvertUrlToRelativeUrl(string _ServiceEndpointPart)
        {
            CompleteUrl = new string(RelativeUrl);
            if (RelativeUrl.StartsWith(_ServiceEndpointPart))
            {
                RelativeUrl = RelativeUrl.Replace(_ServiceEndpointPart, "");
            }
        }

        //Default Instance
        public static readonly PubSubAction_StorageFileUploaded_CloudEventSchemaV1_0 DefaultInstance = new PubSubAction_StorageFileUploaded_CloudEventSchemaV1_0();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class PubSubAction_StorageFileDeleted_CloudEventSchemaV1_0 : PubSubAction_StorageNotification
    {
        [JsonProperty("api")]
        public string DataApi;

        [JsonProperty("url")]
        public string RelativeUrl;

        public string CompleteUrl;

        public static bool IsMatch(JObject _Input)
        {
            return _Input.ContainsKey("api") && _Input.GetValue("api").ToString().StartsWith("Delete")
                && _Input.ContainsKey("url");
        }

        public override PubSubActions.EAction GetActionType()
        {
            return PubSubActions.EAction.ACTION_STORAGE_FILE_DELETED_CLOUDEVENT;
        }

        public void ConvertUrlToRelativeUrl(string _ServiceEndpointPart)
        {
            CompleteUrl = new string(RelativeUrl);
            if (RelativeUrl.StartsWith(_ServiceEndpointPart))
            {
                RelativeUrl = RelativeUrl.Replace(_ServiceEndpointPart, "");
            }
        }

        //Default Instance
        public static readonly PubSubAction_StorageFileDeleted_CloudEventSchemaV1_0 DefaultInstance = new PubSubAction_StorageFileDeleted_CloudEventSchemaV1_0();
        protected override PubSubAction GetStaticDefaultInstance() { return DefaultInstance; }
    }
}
