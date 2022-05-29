/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace WebServiceUtilities.PubSubUsers
{
    public static class PubSubServiceBaseCommon
    {
        public static WebServiceResponse OnRequest(HttpListenerContext _Context, string _CallerMethod, Func<PubSubAction, bool> _HandleAction,  Action<string> _ErrorMessageAction = null)
        {
            string SerializedData = null;
            using (var InputStream = _Context.Request.InputStream)
            {
                using (var Reader = new StreamReader(InputStream))
                {
                    var JsonMessage = Reader.ReadToEnd();
                    try
                    {
                        var Parsed = JObject.Parse(JsonMessage);
                        if (Parsed.ContainsKey("message"))
                        {
                            var MessageObject = (JObject)Parsed["message"];
                            if (MessageObject.ContainsKey("data"))
                            {
                                var EncodedData = (string)MessageObject["data"];
                                SerializedData = Encoding.UTF8.GetString(Convert.FromBase64String(EncodedData));
                            }
                        }
                        if (Parsed.ContainsKey("data"))
                        {
                            var CloudEventDataToken = Parsed["data"];
                            if (CloudEventDataToken.Type == JTokenType.Object)
                            {
                                var CloudEventData = (JObject)Parsed["data"];
                                SerializedData = JsonConvert.SerializeObject(CloudEventData);
                            }
                            else if (CloudEventDataToken.Type == JTokenType.String)
                            {
                                var CloudEventData = (string)Parsed["data"];
                                SerializedData = Encoding.UTF8.GetString(Convert.FromBase64String(CloudEventData));
                            }
                        }
                        if (SerializedData == null)
                        {
                            SerializedData = JsonMessage;
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke($"{_CallerMethod}->OnRequest: Conversion from Base64 to string has failed with {e.Message}, trace: {e.StackTrace}, payload is: {JsonMessage}");
                        return WebResponse.BadRequest("Conversion from Base64 to string has failed.");
                    }
                }
            }

            if (!Manager_PubSubService.Get().DeserializeReceivedMessage(SerializedData,
                out PubSubActions.EAction Action,
                out string SerializedAction,
                _ErrorMessageAction))
            {
                return WebResponse.BadRequest("Deserialization of Pub/Sub Message has failed.");
            }

            bool bResult;
            try
            {
                bResult = _HandleAction.Invoke(PubSubActions.DeserializeAction(Action, SerializedAction));
            }
            catch (Exception e)
            {
                return WebResponse.BadRequest($"Deserialization to Action has failed with {e.Message}, trace: { e.StackTrace}");
            }

            if (bResult)
            {
                return WebResponse.StatusOK("Processed.");
            }

            _ErrorMessageAction?.Invoke($"{_CallerMethod}->OnRequest: An error occured. Retrying.");

            //Cooldown
            Thread.Sleep(1000);

            return WebResponse.BadRequest("An error occurred. Retrying.");
        }
    }

    public abstract class PubSubServiceBase : InternalWebServiceBase
    {
        public PubSubServiceBase(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
        }

        protected override WebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return PubSubServiceBaseCommon.OnRequest(_Context, "PubSubServiceBase",
                (PubSubAction _Action) =>
                {
                    var bResult = Handle(_Context, _Action, _ErrorMessageAction);
                    return bResult;

                }, _ErrorMessageAction);
        }

        protected abstract bool Handle(HttpListenerContext _Context, PubSubAction _DeserializedAction, Action<string> _ErrorMessageAction = null);
    }
}