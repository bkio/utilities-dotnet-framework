/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace WebServiceUtilities.Common
{
    public struct AuthorizedRequester
    {
        public bool bParseSuccessful;
        public string ParseErrorMessage;

        public string UserID;
        public string UserName;
        public string UserEmail;
        public string AuthMethodKey;
    }

    public static class Methods
    {
        public static AuthorizedRequester GetAuthorizedRequester(HttpListenerContext _Context, Action<string> _ErrorMessageAction)
        {
            if (!WebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserID, out string _, _Context, "authorized-u-id") ||
                !WebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserName, out string _, _Context, "authorized-u-name") ||
                !WebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserEmail, out string _, _Context, "authorized-u-email") ||
                !WebUtilities.DoesContextContainHeader(out List<string> AuthorizedUserAuthMethodKey, out string _, _Context, "authorized-u-auth-key"))
            {
                _ErrorMessageAction?.Invoke("Error: Request headers do not contain authorized-u-* headers.");
                return new AuthorizedRequester()
                {
                    bParseSuccessful = false,
                    ParseErrorMessage = "Expected headers could not be found."
                };
            }
            return new AuthorizedRequester()
            {
                bParseSuccessful = true,
                UserID = AuthorizedUserID[0],
                UserEmail = AuthorizedUserEmail[0],
                UserName = AuthorizedUserName[0],
                AuthMethodKey = AuthorizedUserAuthMethodKey[0]
            };
        }

        public static string GetSelfPublicIP()
        {
            using var Handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };

            using var Client = new HttpClient(Handler);
            try
            {
                using var RequestTask = Client.GetAsync("https://api.ipify.org");
                RequestTask.Wait();

                using var Response = RequestTask.Result;
                using var Content = Response.Content;
                using var ReadResponseTask = Content.ReadAsStringAsync();

                ReadResponseTask.Wait();
                return ReadResponseTask.Result;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static string ToISOString()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffZ");
        }

        public static bool TryParseDateTimeFromUtcNowString(string _UtcNowShortDateAndLongTime, out DateTime ParsedDateTime)
        {
            return DateTime.TryParseExact(_UtcNowShortDateAndLongTime, "yyyy-MM-dd HH:mm:ss.fffZ", null, System.Globalization.DateTimeStyles.None, out ParsedDateTime);
        }
    }
}