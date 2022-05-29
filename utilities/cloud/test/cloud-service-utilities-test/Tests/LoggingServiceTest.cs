/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using CloudServiceUtilities;
using CommonUtilities;

namespace CloudServiceUtilitiesTest.Tests
{
    public class LogServiceTest
    {
        private readonly ILogServiceInterface SelectedLogService;

        private readonly Action<string> PrintAction;

        public LogServiceTest(ILogServiceInterface _LogService, Action<string> _PrintAction)
        {
            SelectedLogService = _LogService;
            PrintAction = _PrintAction;
        }

        public bool Start()
        {
            if (!SelectedLogService.WriteLogs(new List<LogParametersStruct>()
            {
                new LogParametersStruct(ELogServiceLogType.Debug, "This is a test debug message - 1"),
                new LogParametersStruct(ELogServiceLogType.Info, "This is a test info message - 1"),
                new LogParametersStruct(ELogServiceLogType.Warning, "This is a test warning message - 1"),
                new LogParametersStruct(ELogServiceLogType.Error, "This is a test error message - 1"),
                new LogParametersStruct(ELogServiceLogType.Critical, "This is a test critical message - 1")
            },
            "BTestGroup",
            "BTestStream",
            false,
            PrintAction))
            {
                return false;
            }
            
            if (!SelectedLogService.WriteLogs(new List<LogParametersStruct>()
            {
                new LogParametersStruct(ELogServiceLogType.Debug, "This is a test debug message - 2"),
                new LogParametersStruct(ELogServiceLogType.Info, "This is a test info message - 2"),
                new LogParametersStruct(ELogServiceLogType.Warning, "This is a test warning message - 2"),
                new LogParametersStruct(ELogServiceLogType.Error, "This is a test error message - 2"),
                new LogParametersStruct(ELogServiceLogType.Critical, "This is a test critical message - 2")
            },
            "BTestGroup",
            "BTestStream",
            false,
            PrintAction))
            {
                return false;
            }
            return true;
        }
    }
}