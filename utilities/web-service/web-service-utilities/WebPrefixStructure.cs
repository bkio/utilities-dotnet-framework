﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using CommonUtilities;

namespace WebServiceUtilities
{
    public class WebPrefixStructure
    {
        private readonly string[] Prefixes_SortedByLength;
        public string[] GetPrefixes()
        {
            //Return by value for encapsulation
            return Prefixes_SortedByLength != null ? (string[])Prefixes_SortedByLength.Clone() : new string[] { };
        }

        public int GetPrefixesLength()
        {
            return Prefixes_SortedByLength != null ? Prefixes_SortedByLength.Length : 0;
        }

        public string GetLongestPrefix()
        {
            if (Prefixes_SortedByLength == null || Prefixes_SortedByLength.Length == 0) return null;
            return Prefixes_SortedByLength[Prefixes_SortedByLength.Length - 1];
        }

        private readonly Func<WebServiceBase> ListenerInitializer;

        private readonly WebSocketListenParameters WSListenParameters;

        public WebPrefixStructure(string[] _Prefixes, Func<WebServiceBase> _ListenerInitializer, WebSocketListenParameters _WebSocketListenParameters = null)
        {
            if (_Prefixes != null && _Prefixes.Length > 0)
            {
                Prefixes_SortedByLength = _Prefixes.OrderBy(x => x.Length).ToArray();
            }
            ListenerInitializer = _ListenerInitializer;
            WSListenParameters = _WebSocketListenParameters;
        }

        public bool GetCallbackFromRequest(out Func<WebServiceBase> _Initializer, out string _MatchedPrefix, out WebSocketListenParameters _WebSocketListenParameters, HttpListenerContext _Context)
        {
            _Initializer = null;
            _MatchedPrefix = null;
            _WebSocketListenParameters = null;

            if (_Context == null || Prefixes_SortedByLength == null || Prefixes_SortedByLength.Length == 0 || ListenerInitializer == null)
            {
                return false;
            }
            
            for (var i = (Prefixes_SortedByLength.Length - 1); i >= 0; i--)
            {
                var Prefix = Prefixes_SortedByLength[i];
                if (Regex.IsMatch(_Context.Request.RawUrl, Utility.WildCardToRegular(Prefix)))
                {
                    _MatchedPrefix = Prefix;
                    _Initializer = ListenerInitializer;
                    _WebSocketListenParameters = WSListenParameters;
                    return _Initializer != null;
                }
            }
            return false;
        }

        public class WebSocketListenParameters
        {
            public bool IsListenForWebSocketOnly()
            {
                return bListenForWebSocketOnly;
            }
            private readonly bool bListenForWebSocketOnly = false;

            public WebSocketListenParameters(bool _bListenForWebSocketOnly)
            {
                bListenForWebSocketOnly = _bListenForWebSocketOnly;
            }
        }

    }
}