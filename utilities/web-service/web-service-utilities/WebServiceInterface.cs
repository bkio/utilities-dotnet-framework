/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;
using System.Net;
using CommonUtilities;

namespace WebServiceUtilities
{
    public struct WebServiceResponse
    {
        public readonly int StatusCode;
        public readonly Dictionary<string, IEnumerable<string>> Headers;
        public readonly StringOrStream ResponseContent;
        public readonly string ResponseContentType;

        public readonly bool bRedirect;
        public readonly string URLIfRedirect;

        public WebServiceResponse(int _StatusCode, Dictionary<string, IEnumerable<string>> _Headers, StringOrStream _ResponseContent = null, string _ResponseContentType = null)
        {
            StatusCode = _StatusCode;

            if (_Headers == null)
            {
                Headers = new Dictionary<string, IEnumerable<string>>();
            }
            else
            {
                Headers = new Dictionary<string, IEnumerable<string>>(_Headers);
            }

            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;

            bRedirect = false;
            URLIfRedirect = null;
        }

        private WebServiceResponse(string _RedirectUrl, Dictionary<string, IEnumerable<string>> _Headers)
        {
            StatusCode = (int)HttpStatusCode.Redirect;

            Headers = _Headers ?? new Dictionary<string, IEnumerable<string>>();

            ResponseContent = null;
            ResponseContentType = null;

            bRedirect = true;
            URLIfRedirect = _RedirectUrl;
        }
        public static WebServiceResponse Redirect(string _Url, Dictionary<string, IEnumerable<string>> _Headers = null)
        {
            return new WebServiceResponse(_Url, _Headers);
        }

        public WebServiceResponse(int _StatusCode, StringOrStream _ResponseContent = null, string _ResponseContentType = null)
        {
            StatusCode = _StatusCode;

            Headers = new Dictionary<string, IEnumerable<string>>();

            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;

            bRedirect = false;
            URLIfRedirect = null;
        }
    }
}