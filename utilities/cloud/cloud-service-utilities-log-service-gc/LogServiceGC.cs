/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Google.Api;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Grpc.Auth;
using CommonUtilities;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace CloudServiceUtilities.LogServices
{
    public class LogServiceGC : ILogServiceInterface
    {
        /// <summary>
        /// <para>Google Logging Service Client that is responsible to serve to this object</para>
        /// </summary>
        private readonly LoggingServiceV2Client LogServiceClient;

        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly ServiceAccountCredential Credential;

        private readonly string ProjectID;

        private static readonly MonitoredResource ResourceName = new MonitoredResource
        {
            Type = "project",
        };

        /// <summary>
        /// 
        /// <para>LogServiceGC: Parametered Constructor for Managed Service by Google</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProjectID"/>                 GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public LogServiceGC(
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            ProjectID = _ProjectID;
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                string ApplicationCredentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_BASE64_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null && ApplicationCredentialsBase64 == null)
                {
                    _ErrorMessageAction?.Invoke("LogServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    var Scopes = new List<string>();
                    foreach (var Scope in LoggingServiceV2Client.DefaultScopes)
                    {
                        if (!Scopes.Contains(Scope))
                        {
                            Scopes.Add(Scope);
                        }
                    }

                    if (ApplicationCredentials == null)
                    {
                        if (ApplicationCredentialsPlain != null && !Utility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        else if (!Utility.Base64Decode(out ApplicationCredentialsPlain, ApplicationCredentialsBase64, _ErrorMessageAction))
                        {
                            throw new Exception("Base64 decode operation for application credentials plain has failed.");
                        }
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                .CreateScoped(
                                Scopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream)
                                .CreateScoped(
                                Scopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        LogServiceClient = new LoggingServiceV2ClientBuilder()
                        {
                            ChannelCredentials = Credential.ToChannelCredentials()

                        }.Build();
                    }

                    if (LogServiceClient != null)
                    {
                        bInitializationSucceed = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"LogServiceGC->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="ILogServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        ///
        /// <para>WriteLogs:</para>
        ///
        /// <para>Writes logs to the logging service</para>
        ///
        /// <para>Check <seealso cref="ILogServiceInterface.WriteLogs"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool WriteLogs(
            List<LogParametersStruct> _Messages,
            string _LogGroupName,
            string _LogStreamName,
            bool _bAsync = true,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Messages == null || _Messages.Count == 0) return false;
            if (_bAsync)
            {
                TaskWrapper.Run(() =>
                {
                    WriteLogs(_Messages, _LogGroupName, _LogStreamName, false, _ErrorMessageAction);
                });
                return true;
            }

            _LogGroupName = Utility.EncodeStringForTagging(_LogGroupName);
            _LogStreamName = Utility.EncodeStringForTagging(_LogStreamName);

            string StreamIDBase = $"{_LogGroupName}-{_LogStreamName}";
            try
            {
                var LogEntries = new LogEntry[_Messages.Count];

                int i = 0;
                foreach (var Message in _Messages)
                {
                    LogEntries[i] = new LogEntry
                    {
                        LogName = new LogName(ProjectID, StreamIDBase).ToString(),
                        TextPayload = Message.Message
                    };

                    switch (Message.LogType)
                    {
                        case ELogServiceLogType.Debug:
                            LogEntries[i].Severity = LogSeverity.Debug;
                            break;
                        case ELogServiceLogType.Info:
                            LogEntries[i].Severity = LogSeverity.Info;
                            break;
                        case ELogServiceLogType.Warning:
                            LogEntries[i].Severity = LogSeverity.Warning;
                            break;
                        case ELogServiceLogType.Error:
                            LogEntries[i].Severity = LogSeverity.Error;
                            break;
                        case ELogServiceLogType.Critical:
                            LogEntries[i].Severity = LogSeverity.Critical;
                            break;
                    }

                    i++;
                }

                LogServiceClient.WriteLogEntries(
                    new LogName(ProjectID, StreamIDBase),
                    ResourceName,
                    new Dictionary<string, string>()
                    {
                        ["LogGroup"] = _LogGroupName,
                        ["LogStream"] = _LogStreamName
                    },
                    LogEntries);

                return true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"LogServiceGC->WriteLogs: {e.Message}, Trace: {e.StackTrace}");
            }
            return false;
        }

        /// <summary>
        ///
        /// <para>GetLogs:</para>
        ///
        /// <para>Get logs from the logging service</para>
        ///
        /// <para>Check <seealso cref="ILogServiceInterface.GetLogs"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetLogs(
            string _LogGroupName,
            string _LogStreamName,
            out List<LogParametersStruct> _Logs,
            out string _NewPageToken,
            string _PreviousPageToken = null,
            int _PageSize = 20,
            Action<string> _ErrorMessageAction = null)
        {
            _Logs = new List<LogParametersStruct>();
            _NewPageToken = null;

            _LogGroupName = Utility.EncodeStringForTagging(_LogGroupName);
            _LogStreamName = Utility.EncodeStringForTagging(_LogStreamName);

            string StreamIDBase = $"{_LogGroupName}-{_LogStreamName}";

            var Request = new ListLogEntriesRequest()
            {
                PageSize = _PageSize,
                OrderBy = "timestamp desc",
                Filter = $"logName=\"projects/{ProjectID}/logs/{StreamIDBase}\""
            };

            var ResourceName = $"projects/{ProjectID}";
            Request.ResourceNames.Add(ResourceName);

            if (!string.IsNullOrEmpty(_PreviousPageToken))
            {
                Request.PageToken = _PreviousPageToken;
            }

            try
            {
                var Result = LogServiceClient.ListLogEntries(Request);
                var ResultPage = Result.ReadPage(_PageSize);
                foreach (var LogEntry in ResultPage)
                {
                    if (LogEntry.PayloadCase == LogEntry.PayloadOneofCase.None) continue;

                    var LogAsString = "";
                    switch (LogEntry.PayloadCase)
                    {
                        case LogEntry.PayloadOneofCase.TextPayload:
                            LogAsString = LogEntry.TextPayload;
                            break;
                        case LogEntry.PayloadOneofCase.JsonPayload:
                            LogAsString = JsonFormatter.Default.Format(LogEntry.JsonPayload);
                            break;
                        case LogEntry.PayloadOneofCase.ProtoPayload:
                            LogAsString = JsonFormatter.Default.Format(LogEntry.ProtoPayload.Unpack<Struct>());
                            break;
                    }

                    switch (LogEntry.Severity)
                    {
                        case LogSeverity.Debug:
                            _Logs.Add(new LogParametersStruct(ELogServiceLogType.Debug, LogAsString));
                            break;
                        case LogSeverity.Warning:
                            _Logs.Add(new LogParametersStruct(ELogServiceLogType.Warning, LogAsString));
                            break;
                        case LogSeverity.Error:
                            _Logs.Add(new LogParametersStruct(ELogServiceLogType.Error, LogAsString));
                            break;
                        case LogSeverity.Critical:
                            _Logs.Add(new LogParametersStruct(ELogServiceLogType.Critical, LogAsString));
                            break;
                        default:
                            _Logs.Add(new LogParametersStruct(ELogServiceLogType.Info, LogAsString));
                            break;
                    }
                }
                _NewPageToken = ResultPage.NextPageToken;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"LogServiceGC->GetLogs: {e.Message}, Trace: {e.StackTrace}, ResourceName: " + ResourceName);
                return false;
            }
            return true;
        }
    }
}