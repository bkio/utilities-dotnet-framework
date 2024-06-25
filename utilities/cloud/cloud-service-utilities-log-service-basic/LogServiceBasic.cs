/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;

namespace CloudServiceUtilities.LogServices
{
    public class LogServiceBasic : ILogServiceInterface
    {
        public bool HasInitializationSucceed()
        {
            return true;
        }

        public bool WriteLogs(List<LogParametersStruct> _Messages, string _LogGroupName, string _LogStreamName, bool _bAsync = true, Action<string> _ErrorMessageAction = null)
        {
            foreach (var Message in _Messages)
            {
                Console.WriteLine($"{Message.LogType}: {_LogStreamName} -> {_LogGroupName} -> {Message.Message}");
            }
            return true;
        }
        public bool GetLogs(string _LogGroupName, string _LogStreamName, out List<LogParametersStruct> _Logs, out string _NewPageToken, string _PreviousPageToken = null, int _PageSize = 20, Action<string> _ErrorMessageAction = null)
        {
            throw new NotImplementedException();
        }
    }
}