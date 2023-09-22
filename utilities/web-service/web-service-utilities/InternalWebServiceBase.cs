/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using CommonUtilities;

namespace WebServiceUtilities
{
    public abstract class InternalWebServiceBase : WebServiceBase
    {
        protected readonly string InternalCallPrivateKey;
        private string WebhookRequestCallback = null;
        private string WebhookRequestOrigin = null;

        public InternalWebServiceBase(string _InternalCallPrivateKey)
        {
            InternalCallPrivateKey = _InternalCallPrivateKey;
        }
        private InternalWebServiceBase() { }

        protected override WebServiceResponse OnRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            // Cloud Event Schema v1.0
            // https://github.com/cloudevents/spec/blob/v1.0/http-webhook.md#4-abuse-protection
            if (_Context.Request.HttpMethod == "OPTIONS")
            {
                WebhookRequestCallback = _Context.Request.Headers.Get("WebHook-Request-Callback");
                WebhookRequestOrigin = _Context.Request.Headers.Get("WebHook-Request-Origin");

                if (WebhookRequestCallback != null && WebhookRequestOrigin != null)
                {
                    ThreadWrapper.Run(() =>
                    {
                        Thread.Sleep(1000);

                        SendWebhookValidationRequest(WebhookRequestOrigin, WebhookRequestCallback, _ErrorMessageAction);
                    });

                    return WebResponse.StatusOK("OK.");
                }

                return WebResponse.BadRequest("WebHook-Request-Callback and WebHook-Request-Origin must be included in the request.");
            }

            if (UrlParameters.ContainsKey("secret") && UrlParameters["secret"] == InternalCallPrivateKey)
            {
                return Process(_Context, _ErrorMessageAction);
            }
            return WebResponse.Forbidden("You are trying to access to a private service.");
        }

        private void SendWebhookValidationRequest(string _WebhookRequestOrigin, string _WebhookRequestCallback, Action<string> _ErrorMessageAction)
        {
            using var Handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };
            using var Client = new HttpClient(Handler);
            Client.DefaultRequestHeaders.TryAddWithoutValidation("WebHook-Allowed-Origin", _WebhookRequestOrigin);
            Client.DefaultRequestHeaders.TryAddWithoutValidation("WebHook-Allowed-Rate", "*");
            using (var RequestTask = Client.GetAsync(_WebhookRequestCallback))
            {
                RequestTask.Wait();
                using var Response = RequestTask.Result;
                using var ResponseContent = Response.Content;

                using var ReadResponseTask = ResponseContent.ReadAsStringAsync();
                ReadResponseTask.Wait();

                var ResponseString = ReadResponseTask.Result;
                var ResponseStatusCode = (int)Response.StatusCode;
                var ResponseSuccessString = Response.IsSuccessStatusCode ? "OK" : "ERROR";
                _ErrorMessageAction?.Invoke($"InternalWebServiceBaseWebhook->ValidationResponse: From '{_WebhookRequestOrigin}', Result {ResponseSuccessString} ({ResponseStatusCode}): '{ResponseString}'");
            }
        }

        protected abstract WebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
}