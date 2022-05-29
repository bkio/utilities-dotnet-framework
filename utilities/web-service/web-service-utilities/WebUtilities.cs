﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using CommonUtilities;

namespace WebServiceUtilities
{
    public static class WebUtilities
    {
        public static void InjectHeadersIntoDictionary(HttpHeaders _Headers, Dictionary<string, IEnumerable<string>> _Dictionary)
        {
            if (_Headers != null && _Dictionary != null)
            {
                foreach (var Header in _Headers)
                {
                    if (!_Dictionary.ContainsKey(Header.Key))
                    {
                        _Dictionary.Add(Header.Key, new List<string>());
                    }

                    foreach (var Value in Header.Value)
                    {
                        ((List<string>)(_Dictionary[Header.Key])).Add(Value);
                    }
                }
            }
        }

        public static void InjectHeadersIntoDictionary(WebHeaderCollection _Headers, Dictionary<string, IEnumerable<string>> _Dictionary)
        {
            if (_Headers != null && _Dictionary != null)
            {
                foreach (var RHeader in _Headers.AllKeys)
                {
                    if (!_Dictionary.ContainsKey(RHeader))
                    {
                        _Dictionary.Add(RHeader, new List<string>());
                    }
                    ((List<string>)(_Dictionary[RHeader])).AddRange(_Headers.GetValues(RHeader));
                }
            }
        }

        public static bool DoesContextContainHeader(out List<string> _HeaderValues, out string _CaseSensitive_FoundHeaderKey, HttpListenerContext _Context, string _HeaderKey)
        {
            _CaseSensitive_FoundHeaderKey = null;

            _HeaderKey = _HeaderKey.ToLower();
            _HeaderValues = new List<string>();

            foreach (var RequestKey in _Context.Request.Headers.AllKeys)
            {
                string Key = RequestKey.ToLower();
                if (Key == _HeaderKey)
                {
                    _CaseSensitive_FoundHeaderKey = RequestKey;
                    foreach (var HeaderValue in _Context.Request.Headers.GetValues(RequestKey))
                    {
                        _HeaderValues.Add(HeaderValue);
                    }
                }
            }
            return _HeaderValues.Count > 0;
        }

        public static string ReplaceHostPart(string Source, string NewHostname)
        {
            if (Source.StartsWith("http://"))
            {
                Source = Source.Substring("http://".Length);
            }
            else if (Source.StartsWith("https://"))
            {
                Source = Source.Substring("https://".Length);
            }

            int FirstSlash = Source.IndexOf('/');
            if (FirstSlash < 0)
            {
                return NewHostname;
            }
            Source = Source.Substring(FirstSlash);

            return $"{NewHostname}{Source}";
        }

        public static void LogHeaders(string _Identifier, Dictionary<string, IEnumerable<string>> _Headers)
        {
            var LogText = "";
            foreach (var Header in _Headers)
            {
                var HeaderValues = "";
                foreach (var HeaderValue in Header.Value)
                {
                    HeaderValues += $"{HeaderValue}[-]";
                }
                HeaderValues = HeaderValues.TrimEnd("[-]");
                LogText += $"{Header.Key}--->{HeaderValues}\n";
            }
            LogText = LogText.TrimEnd('\n');
            Console.WriteLine($"{_Identifier} headers:\n {LogText}");
        }
        public static void LogHeaders(string _Identifier, WebHeaderCollection _Headers)
        {
            var LogText = "";
            foreach (var HeaderKey in _Headers.AllKeys)
            {
                var HeaderValues = "";
                foreach (var HeaderValue in _Headers.GetValues(HeaderKey))
                {
                    HeaderValues += $"{HeaderValue}[-]";
                }
                HeaderValues = HeaderValues.TrimEnd("[-]");
                LogText += $"{HeaderKey}--->{HeaderValues}\n";
            }
            LogText = LogText.TrimEnd('\n');
            Console.WriteLine($"{_Identifier} headers:\n {LogText}");
        }
        public static void LogHeaders(string _Identifier, HttpRequestHeaders _Headers)
        {
            var LogText = "";
            foreach (var Header in _Headers)
            {
                var HeaderValues = "";
                foreach (var HeaderValue in Header.Value)
                {
                    HeaderValues += $"{HeaderValue}[-]";
                }
                HeaderValues = HeaderValues.TrimEnd("[-]");
                LogText += $"{Header.Key}--->{HeaderValues}\n";
            }
            LogText = LogText.TrimEnd('\n');
            Console.WriteLine($"{_Identifier} headers:\n {LogText}");
        }

        public static Tuple<string, string>[] AnalyzeURLParametersFromRawURL(string _RawURL)
        {
            if (_RawURL == null) return null;

            _RawURL = _RawURL.TrimStart('/');
            if (_RawURL.Length == 0) return null;

            int FirstQMIndex = _RawURL.IndexOf('?');
            if (FirstQMIndex == -1) return null;

            _RawURL = _RawURL.Substring(FirstQMIndex + 1);

            List<Tuple<string, string>> Parameters = new List<Tuple<string, string>>();

            string[] Splitted = _RawURL.Split('&');
            if (Splitted != null && Splitted.Length > 0)
            {
                foreach (string Parameter in Splitted)
                {
                    string[] SplittedKeyValue = Parameter.Split('=');
                    if (SplittedKeyValue != null && SplittedKeyValue.Length == 2)
                    {
                        Parameters.Add(new Tuple<string, string>(SplittedKeyValue[0], SplittedKeyValue[1]));
                    }
                }
            }

            if (Parameters.Count == 0) return null;
            return Parameters.ToArray();
        }

        public static string GetFirstPathElementFromRawUrl(string _RawUrl)
        {
            string FirstPathElement = "";
            var PathElements = _RawUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (PathElements != null && PathElements.Length > 0)
            {
                FirstPathElement = PathElements[0];
            }
            return FirstPathElement;
        }

        public static bool GetEndpointListFromDirectoryTreeNode(out List<Tuple<string, string>> _EndpointList, DirectoryTreeNode _ParentNode, Action<string> _ErrorMessageAction = null)
        {
            _EndpointList = new List<Tuple<string, string>>();

            if (!ConvertDirectoryTreeNodeToPath(_EndpointList, _ParentNode, _ErrorMessageAction))
            {
                _EndpointList = null;
                return false;
            }

            //index.html fix iteration
            var InitialEndpointListSize = _EndpointList.Count;
            for (int i = 0; i < InitialEndpointListSize; i++)
            {
                var Lowered = _EndpointList[i].Item1.ToLower();
                if (Lowered.EndsWith("/index.html") || Lowered.EndsWith("/index.htm"))
                {
                    var WithoutIndexHtml = _EndpointList[i].Item1.Substring(0, _EndpointList[i].Item1.LastIndexOf('/'));

                    _EndpointList.Add(new Tuple<string, string>($"{WithoutIndexHtml}/", _EndpointList[i].Item2));
                }
            }
            return true;
        }
        private static bool ConvertDirectoryTreeNodeToPath(List<Tuple<string, string>> _EndpointList, DirectoryTreeNode _CurrentNode, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                if (_CurrentNode.GetNodeType() == EDirectoryTreeNodeType.File)
                {
                    string Path = _CurrentNode.GetName();

                    var CurrentParent = _CurrentNode.GetParent();
                    while (CurrentParent != null && CurrentParent.GetParent() != null /*Root Directory*/)
                    {
                        Path = $"{CurrentParent.GetName()}/{Path}";
                        CurrentParent = CurrentParent.GetParent();
                    }

                    Path = $"/{Path}";

                    _EndpointList.Add(new Tuple<string, string>(Path, CurrentParent != null ? ($"/{CurrentParent.GetName()}{Path}") : Path));
                }
                else
                {
                    if (_CurrentNode.GetChildren() != null)
                    {
                        foreach (var ChildNode in _CurrentNode.GetChildren())
                        {
                            if (!ConvertDirectoryTreeNodeToPath(_EndpointList, ChildNode, _ErrorMessageAction))
                            {
                                return false;
                            }
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"WebUtilities->ConvertDirectoryTreeNodeToPath has failed with {e.Message}, trace: {e.StackTrace}");
                return false;
            }
            return true;
        }
    }
}