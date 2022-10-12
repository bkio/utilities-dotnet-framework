/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;

namespace WebServiceUtilities
{
    public abstract class WebServiceBase
    {
        protected WebServiceBase() { }

        /// <summary>
        /// If the url is like: /abc/def/*/qwe/ghj/*/bk
        /// This map would contain def->[Value of first *], ghj->[Value of second *]
        /// If there is not a value before the first occurence of *, it will be ignored.
        /// </summary>
        protected Dictionary<string, string> RestfulUrlParameters = new Dictionary<string, string>();

        /// <summary>
        /// The map that contains parameters provided after question mark.
        /// </summary>
        protected Dictionary<string, string> UrlParameters = new Dictionary<string, string>();

        private bool bInitialized = false;
        protected bool IsInitialized()
        {
            return bInitialized;
        }
        internal void InitializeWebService(HttpListenerContext _Context, string _MatchedPrefix)
        {
            if (bInitialized) return;
            bInitialized = true;
            
            var SplittedRawUrl = _Context.Request.RawUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var SplittedMatchedPrefix = _MatchedPrefix.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (SplittedRawUrl != null && SplittedMatchedPrefix != null && SplittedRawUrl.Length >= SplittedMatchedPrefix.Length)
            {
                for (int i = 1; i < SplittedMatchedPrefix.Length; i++)
                {
                    if (SplittedMatchedPrefix[i] == "*")
                    {
                        string Value;
                        if (SplittedRawUrl[i].Contains("?"))
                        {
                            Value = SplittedRawUrl[i].Split("?")[0];
                        }
                        else
                        {
                            Value = SplittedRawUrl[i];
                        }
                        RestfulUrlParameters[SplittedRawUrl[i - 1]] = Value;
                    }
                }
            }

            var Params = WebUtilities.AnalyzeURLParametersFromRawURL(_Context.Request.RawUrl);
            if (Params != null)
            {
                foreach (var Param in Params)
                {
                    UrlParameters[Param.Item1] = Param.Item2;
                }
            }
        }

        internal WebServiceResponse ProcessRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return OnRequest(_Context, _ErrorMessageAction);
        }
        protected abstract WebServiceResponse OnRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);

        public virtual CORSHeaders GetCORSHeaders()
        {
            return new CORSHeaders();
        }
        public class CORSHeaders
        {
            public string AccessControlAllowOrigin = "*";
            public string AccessControlAllowHeaders = "*";
            public string AccessControllAllowCredentials = "true";
            public string AccessControlAllowMethods = "GET, POST, PUT, DELETE, PATCH";
            public string AccessControlMaxAge = "-1";
        }
    }
}