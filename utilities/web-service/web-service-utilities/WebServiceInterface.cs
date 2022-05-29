/// Copyright 2022- Burak Kara, All rights reserved.

using System;
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

        public WebServiceResponse(int _StatusCode, Dictionary<string, IEnumerable<string>> _Headers, StringOrStream _ResponseContent, string _ResponseContentType = null)
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
        }

        public WebServiceResponse(int _StatusCode, StringOrStream _ResponseContent, string _ResponseContentType = null)
        {
            StatusCode = _StatusCode;

            Headers = new Dictionary<string, IEnumerable<string>>();

            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;
        }
    }
}