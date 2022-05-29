/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CloudServiceUtilities
{
    public class LogServiceLoggerZipkin : ILogger, zipkin4net.ILogger
    {
        private readonly ILogServiceInterface SelectedLogService;
        private readonly Action<string> BackupLoggingAction;

        private readonly string ProgramUniqueID;

        private bool bRunning = true;
        private readonly Thread TickerThread;
        private readonly ConcurrentQueue<LogParametersStruct> MessageQueue = new ConcurrentQueue<LogParametersStruct>();
        private void TickerThreadRunnable()
        {
            Thread.CurrentThread.IsBackground = true;

            while (bRunning)
            {
                var Logs = new List<LogParametersStruct>();
                while (MessageQueue.TryDequeue(out LogParametersStruct Message))
                {
                    Logs.Add(Message);
                }

                if (!SelectedLogService.WriteLogs(Logs, ProgramUniqueID, "Logger", false, BackupLoggingAction))
                {
                    foreach (var Log in Logs)
                    {
                        BackupLoggingAction?.Invoke($"{Log.LogType}: {Log.Message}");
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public LogServiceLoggerZipkin(ILogServiceInterface _SelectedLogService, Action<string> _BackupLoggingAction, string _ProgramUniqueID)
        {
            SelectedLogService = _SelectedLogService;
            BackupLoggingAction = _BackupLoggingAction;
            ProgramUniqueID = _ProgramUniqueID;

            TickerThread = new Thread(TickerThreadRunnable);
            TickerThread.Start();
        }
        ~LogServiceLoggerZipkin()
        {
            bRunning = false;
        }

        public IDisposable BeginScope<TState>(TState _State)
        {
            return null;
        }

        public bool IsEnabled(LogLevel _LogLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel _LogLevel, EventId _EventId, TState _State, Exception _Exception, Func<TState, Exception, string> _Formatter)
        {
            string Message = null;

            var LogType = ELogServiceLogType.Info;
            switch (_LogLevel)
            {
                case LogLevel.Debug:
                    LogType = ELogServiceLogType.Debug;
                    break;
                case LogLevel.Warning:
                    LogType = ELogServiceLogType.Warning;
                    break;
                case LogLevel.Error:
                    LogType = ELogServiceLogType.Error;
                    break;
                case LogLevel.Critical:
                    LogType = ELogServiceLogType.Critical;
                    break;
                default:
                    break;
            }

            if (_Formatter != null)
            {
                Message = _Formatter(_State, _Exception);
            }
            else if (_Exception != null)
            {
                Message = $"Message: {_Exception.Message}, Trace: {_Exception.StackTrace}";
            }

            if (Message != null && Message.Length > 0)
            {
                MessageQueue.Enqueue(new LogParametersStruct(LogType, Message));
            }
        }

        public void LogInformation(string _Message)
        {
            if (_Message != null && _Message.Length > 0)
            {
                MessageQueue.Enqueue(new LogParametersStruct(ELogServiceLogType.Info, _Message));
            }
        }

        public void LogWarning(string _Message)
        {
            if (_Message != null && _Message.Length > 0)
            {
                MessageQueue.Enqueue(new LogParametersStruct(ELogServiceLogType.Warning, _Message));
            }
        }

        public void LogError(string _Message)
        {
            if (_Message != null && _Message.Length > 0)
            {
                MessageQueue.Enqueue(new LogParametersStruct(ELogServiceLogType.Error, _Message));
            }
        }
    }
}