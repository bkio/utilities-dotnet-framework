/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using CommonUtilities;
using WebResponse = WebServiceUtilities.WebResponse;

namespace WebServiceUtilities
{
    public static class WebServiceExtraUtilities
    {
        private static readonly HashSet<string> IllegalHttpRequestHeaders = new HashSet<string>()
        {
            "upgrade-insecure-requests", "sec-fetch-user", "sec-fetch-site", "sec-fetch-mode",
            "cache-control", "connection",
            "accept", "accept-encoding", "accept-language", "host", "user-agent",
            "x-forwarded-for", "x-forwarded-proto", "x-cloud-trace-context", "forwarded",
            "content-length", "content-type"
        };

        public static void InsertHeadersFromContextInto(HttpListenerContext _Context, Action<string, string> _CollectionAddFunction, string[] _ExcludeHeaderKeys = null)
        {
            if (_ExcludeHeaderKeys != null)
            {
                for (int i = 0; i < _ExcludeHeaderKeys.Length; i++)
                {
                    _ExcludeHeaderKeys[i] = _ExcludeHeaderKeys[i].ToLower();
                }
            }

            foreach (var RequestKey in _Context.Request.Headers.AllKeys)
            {
                var LoweredKey = RequestKey.ToLower();
                if (!IllegalHttpRequestHeaders.Contains(LoweredKey))
                {
                    if (_ExcludeHeaderKeys != null && _ExcludeHeaderKeys.Contains(LoweredKey)) continue;

                    var Values = _Context.Request.Headers.GetValues(RequestKey);
                    foreach (var Value in Values)
                    {
                        _CollectionAddFunction?.Invoke(RequestKey, Value);
                    }
                }
            }
        }

        public static void InsertHeadersFromDictionaryInto(Dictionary<string, IEnumerable<string>> _Dictionary, Action<string, string> _CollectionAddFunction, string[] _ExcludeHeaderKeys = null)
        {
            if (_ExcludeHeaderKeys != null)
            {
                for (int i = 0; i < _ExcludeHeaderKeys.Length; i++)
                {
                    _ExcludeHeaderKeys[i] = _ExcludeHeaderKeys[i].ToLower();
                }
            }

            foreach (var RequestKeyVals in _Dictionary)
            {
                var LoweredKey = RequestKeyVals.Key.ToLower();
                if (!IllegalHttpRequestHeaders.Contains(LoweredKey))
                {
                    if (_ExcludeHeaderKeys != null && _ExcludeHeaderKeys.Contains(LoweredKey)) continue;

                    foreach (var Value in RequestKeyVals.Value)
                    {
                        _CollectionAddFunction?.Invoke(RequestKeyVals.Key, Value);
                    }
                }
            }
        }

        public static void InsertHeadersFromDictionaryIntoContext(Dictionary<string, IEnumerable<string>> _HttpRequestResponseHeaders, HttpListenerContext Context)
        {
            foreach (var Header in _HttpRequestResponseHeaders)
            {
                if (!IllegalHttpRequestHeaders.Contains(Header.Key.ToLower()))
                {
                    foreach (var Value in Header.Value)
                    {
                        Context.Request.Headers.Add(Header.Key, Value);
                    }
                }
            }
        }

        public class InterServicesRequestRequest
        {
            public string DestinationServiceUrl;
            public string RequestMethod;
            public string ContentType;
            public Dictionary<string, IEnumerable<string>> Headers = null;
            public StringOrStream Content = null;
            public bool bWithAuthToken = true;
            public HttpListenerContext UseContextHeaders = null;
            public IEnumerable<string> ExcludeHeaderKeysForRequest = null;
        }

        public class InterServicesRequestResponse
        {
            public bool bSuccess = false;
            public int ResponseCode = WebResponse.Error_InternalError_Code;
            public string ContentType = WebResponse.Error_InternalError_ContentType;
            public StringOrStream Content = null;
            public Dictionary<string, IEnumerable<string>> ResponseHeaders = new Dictionary<string, IEnumerable<string>>();

            public static InterServicesRequestResponse InternalErrorOccured(string _Message)
            {
                return new InterServicesRequestResponse()
                {
                    Content = new StringOrStream(_Message)
                };
            }
        }

        public static InterServicesRequestResponse InterServicesRequest(
            InterServicesRequestRequest _Request,
            bool _bKillProcessOnAddAccessTokenForServiceExecutionFailure = true,
            Action<string> _ErrorMessageAction = null)
        {
            var bHttpRequestSuccess = false;
            var HttpRequestResponseCode = WebResponse.Error_InternalError_Code;
            var HttpRequestResponseContentType = "";
            StringOrStream HttpRequestResponseContent = null;
            Dictionary<string, IEnumerable<string>> HttpRequestResponseHeaders = null;

            var Request = (HttpWebRequest)WebRequest.Create(_Request.DestinationServiceUrl);
            Request.Method = _Request.RequestMethod;
            Request.ServerCertificateValidationCallback = (a, b, c, d) => true;
            Request.AllowAutoRedirect = false;
            Request.Timeout = 900000; //15 mins

            if (_Request.bWithAuthToken)
            {
                //If context-headers already contain authorization; we must rename it to client-authorization to prevent override.
                if (_Request.UseContextHeaders != null
                    && WebUtilities.DoesContextContainHeader(out List<string> AuthorizationHeaderValues, out string CaseSensitive_FoundHeaderKey, _Request.UseContextHeaders, "authorization")
                    && Utility.CheckAndGetFirstStringFromList(AuthorizationHeaderValues, out string ClientAuthorization))
                {
                    _Request.UseContextHeaders.Request.Headers.Remove(CaseSensitive_FoundHeaderKey);
                    _Request.UseContextHeaders.Request.Headers.Add("client-authorization", ClientAuthorization);
                }
            }

            var ExcludeHeaderKeysForRequest = LowerContentOfStrings(_Request.ExcludeHeaderKeysForRequest);

            if (_Request.UseContextHeaders != null)
            {
                InsertHeadersFromContextInto(_Request.UseContextHeaders, (string _Key, string _Value) =>
                {
                    if (ExcludeHeaderKeysForRequest != null && ExcludeHeaderKeysForRequest.Contains(_Key.ToLower())) return;

                    Request.Headers.Add(_Key, _Value);
                });
            }
            if (_Request.Headers != null)
            {
                InsertHeadersFromDictionaryInto(_Request.Headers, (string _Key, string _Value) =>
                {
                    if (ExcludeHeaderKeysForRequest != null && ExcludeHeaderKeysForRequest.Contains(_Key.ToLower())) return;

                    Request.Headers.Add(_Key, _Value);
                });
            }

            try
            {
                if (_Request.RequestMethod != "GET" /*&& _Request.RequestMethod != "DELETE"*/
                    && _Request.Content != null && ((_Request.Content.Type == EStringOrStreamEnum.Stream && _Request.Content.Stream != null) || (_Request.Content.Type == EStringOrStreamEnum.String && _Request.Content.String != null && _Request.Content.String.Length > 0)))
                {
                    Request.ContentType = _Request.ContentType;

                    using (var OStream = Request.GetRequestStream())
                    {
                        if (_Request.Content.Type == EStringOrStreamEnum.Stream)
                        {
                            _Request.Content.Stream.CopyTo(OStream);
                        }
                        else
                        {
                            using (var RStream = new StreamWriter(OStream))
                            {
                                RStream.Write(_Request.Content.String);
                            }
                        }
                    }
                }

                try
                {
                    //Don't dispose response here because the content might be needed for streaming beyond this point unless you want to keep all the data in memory.
                    var Response = (HttpWebResponse)Request.GetResponse();
                    AnalyzeResponse(Response, out bHttpRequestSuccess, out HttpRequestResponseCode, out HttpRequestResponseContentType, out HttpRequestResponseContent, out HttpRequestResponseHeaders, _ErrorMessageAction, true);
                    
                }
                catch (Exception e)
                {
                    if (e is WebException)
                    {
                        using (var ErrorResponse = (HttpWebResponse)(e as WebException).Response)
                        {
                            AnalyzeResponse(ErrorResponse, out bHttpRequestSuccess, out HttpRequestResponseCode, out HttpRequestResponseContentType, out HttpRequestResponseContent, out HttpRequestResponseHeaders, _ErrorMessageAction);
                        }
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke($"Error: InterServicesRequest: {e.Message}, Trace: { e.StackTrace}");
                        bHttpRequestSuccess = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"Error: InterServicesRequest: {e.Message}, Trace: { e.StackTrace}");
                bHttpRequestSuccess = false;
            }

            if (!bHttpRequestSuccess)
            {
                _ErrorMessageAction?.Invoke($"Error: Request has failed due to an internal api gateway error. Service endpoint: { _Request.DestinationServiceUrl}");
                return InterServicesRequestResponse.InternalErrorOccured("Request has failed due to an internal api gateway error.");
            }

            if (_Request.UseContextHeaders != null)
            {
                InsertHeadersFromDictionaryIntoContext(HttpRequestResponseHeaders, _Request.UseContextHeaders);
            }

            return new InterServicesRequestResponse()
            {
                bSuccess = true,
                ResponseCode = HttpRequestResponseCode,
                ContentType = HttpRequestResponseContentType,
                ResponseHeaders = HttpRequestResponseHeaders,
                Content = HttpRequestResponseContent
            };
        }

        private static List<string> LowerContentOfStrings(IEnumerable<string> _Strings)
        {
            List<string> Lowered = null;
            if (_Strings != null)
            {
                Lowered = new List<string>(_Strings.Count());
                foreach (var ExcludeKey in _Strings)
                {
                    Lowered.Add(ExcludeKey.ToLower());
                }
            }
            return Lowered;
        }

        public static WebServiceResponse RequestRedirection(
            HttpListenerContext _Context,
            string _FullEndpoint,
            Action<string> _ErrorMessageAction = null,
            bool _bWithAuthToken = true,
            bool _bKillProcessOnAddAccessTokenForServiceExecutionFailure = true,
            IEnumerable<string> _ExcludeHeaderKeysForRequest = null)
        {
            using (var InputStream = _Context.Request.InputStream)
            {
                using (var RequestStream = InputStream)
                {
                    var Result = InterServicesRequest(new InterServicesRequestRequest()
                    {
                        DestinationServiceUrl = _FullEndpoint,
                        RequestMethod = _Context.Request.HttpMethod,
                        ContentType = _Context.Request.ContentType,
                        Content = new StringOrStream(RequestStream, _Context.Request.ContentLength64),
                        bWithAuthToken = _bWithAuthToken,
                        UseContextHeaders = _Context,
                        ExcludeHeaderKeysForRequest = _ExcludeHeaderKeysForRequest
                    },
                    _bKillProcessOnAddAccessTokenForServiceExecutionFailure,
                    _ErrorMessageAction);

                    return new WebServiceResponse(
                        Result.ResponseCode,
                        Result.ResponseHeaders,
                        Result.Content,
                        Result.ContentType);
                }
            }
        }

        private static void AnalyzeResponse(
            HttpWebResponse _Response,
            out bool _bHttpRequestSuccess,
            out int _HttpRequestResponseCode,
            out string _HttpRequestResponseContentType,
            out StringOrStream _HttpRequestResponseContent,
            out Dictionary<string, IEnumerable<string>> _HttpRequestResponseHeaders,
            Action<string> _ErrorMessageAction,
            bool _StreamableContent = false)
        {
            _bHttpRequestSuccess = false;
            _HttpRequestResponseCode = WebResponse.Error_InternalError_Code;
            _HttpRequestResponseContentType = "";
            _HttpRequestResponseContent = null;
            _HttpRequestResponseHeaders = new Dictionary<string, IEnumerable<string>>();

            try
            {
                _HttpRequestResponseCode = (int)_Response.StatusCode;

                WebUtilities.InjectHeadersIntoDictionary(_Response.Headers, _HttpRequestResponseHeaders);

                _HttpRequestResponseContentType = _Response.ContentType;


                if(_StreamableContent && _Response.ContentLength > 0)
                {
                    _HttpRequestResponseContent = new StringOrStream(_Response.GetResponseStream(), _Response.ContentLength);
                }
                else
                {
                    if (_Response.ContentLength > 0)
                    {
                        using (var ResStream = _Response.GetResponseStream())
                        {
                            var CopyStream = new MemoryTributary(Utility.ReadToEnd(ResStream));

                            _HttpRequestResponseContent = new StringOrStream(CopyStream, CopyStream.Length, () => { try { CopyStream?.Dispose(); } catch { } });
                        }
                    }
                    else
                    {
                        _HttpRequestResponseContent = new StringOrStream("");
                    }
                }


                _bHttpRequestSuccess = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"Error: RequestRedirection-AnalyzeResponse: {e.Message}, Trace: { e.StackTrace}");
                _bHttpRequestSuccess = false;
            }
        }
    }
}