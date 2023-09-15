/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CloudServiceUtilities;
using CloudServiceUtilities.LogServices;
using CommonUtilities;

[assembly: InternalsVisibleTo("CloudConnector_DatabaseService_AWS")]
[assembly: InternalsVisibleTo("CloudConnector_DatabaseService_GC")]
[assembly: InternalsVisibleTo("CloudConnector_DatabaseService_MongoDB")]
[assembly: InternalsVisibleTo("CloudConnector_FileService_AWS")]
[assembly: InternalsVisibleTo("CloudConnector_FileService_AZ")]
[assembly: InternalsVisibleTo("CloudConnector_FileService_GC")]
[assembly: InternalsVisibleTo("CloudConnector_FileService_MinIO")]
[assembly: InternalsVisibleTo("CloudConnector_LogService_AWS")]
[assembly: InternalsVisibleTo("CloudConnector_LogService_AZ")]
[assembly: InternalsVisibleTo("CloudConnector_LogService_GC")]
[assembly: InternalsVisibleTo("CloudConnector_MailService_SendGrid")]
[assembly: InternalsVisibleTo("CloudConnector_MemoryService_Redis")]
[assembly: InternalsVisibleTo("CloudConnector_PubSubService_AWS")]
[assembly: InternalsVisibleTo("CloudConnector_PubSubService_AZ")]
[assembly: InternalsVisibleTo("CloudConnector_PubSubService_GC")]
[assembly: InternalsVisibleTo("CloudConnector_PubSubService_Redis")]
[assembly: InternalsVisibleTo("CloudConnector_SMSService_Twilio")]
[assembly: InternalsVisibleTo("CloudConnector_TracingService_GC")]
[assembly: InternalsVisibleTo("CloudConnector_TracingService_Zipkin")]
[assembly: InternalsVisibleTo("CloudConnector_VMService_AZ")]
[assembly: InternalsVisibleTo("CloudConnector_VMService_GC")]

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>1) PROGRAM_ID:                                Program Unique ID</para>
    /// <para>2) PORT:                                      Port of the http server</para>
    /// 
    /// </summary>
    public class CloudConnector
    {
        private CloudConnector() { }

        /// <summary>
        /// <para>HTTP Server Port</para>
        /// </summary>
        public int ServerPort { get; private set; }

        /// <summary>
        /// <para>Program Unique ID</para>
        /// </summary>
        public string ProgramID { get; private set; }

        /// <summary>
        /// <para>Parsed environment variables which are required for the application</para>
        /// </summary>
        public Dictionary<string, string> RequiredEnvironmentVariables { get { return _RequiredEnvironmentVariables; } }
        private Dictionary<string, string> _RequiredEnvironmentVariables = null;

        public static bool Initialize(
            out CloudConnector _Result,
            string[][] _RequiredExtraEnvVars = null)
        {
            var Instance = new CloudConnector();
            _Result = null;

            Instance.LogService = new LogServiceBasic();

            var RequiredEnvVarKeys = new List<string[]>()
            {
                new string[] { "PORT" },
                new string[] { "PROGRAM_ID" }
            };
            if (_RequiredExtraEnvVars != null)
            {
                RequiredEnvVarKeys.AddRange(_RequiredExtraEnvVars);
            }

            /*
            * Getting environment variables
            */
            if (!Utility.GetEnvironmentVariables(out Instance._RequiredEnvironmentVariables,
                RequiredEnvVarKeys.ToArray(),
                (string Message) =>
                {
                    Instance.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), Instance.ProgramID, "Initialization");
                })) return false;

            Instance.ProgramID = Instance.RequiredEnvironmentVariables["PROGRAM_ID"];

            /*
            * Parsing http server port
            */
            if (!int.TryParse(Instance.RequiredEnvironmentVariables["PORT"], out int _ServPort))
            {
                Instance.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Given http server port is invalid."), Instance.ProgramID, "Initialization");
                return false;
            }
            Instance.ServerPort = _ServPort;

            _Result = Instance;
            return true;
        }

        /// <summary>
        /// <para>Database Service</para>
        /// </summary>
        public IDatabaseServiceInterface DatabaseService
        {
            get
            {
                if (DatabaseServiceValue != null)   return DatabaseServiceValue;
                else                                throw new Exception("DatabaseService not initialized");
            }
            internal set { DatabaseServiceValue = value; }
        }

        /// <summary>
        /// <para>File Service</para>
        /// </summary>
        public IFileServiceInterface FileService
        {
            get
            {
                if (FileServiceValue != null)       return FileServiceValue;
                else                                throw new Exception("FileService not initialized");
            }
            internal set { FileServiceValue = value; }
        }

        /// <summary>
        /// <para>Log Service</para>
        /// </summary>
        public ILogServiceInterface LogService
        {
            get
            {
                if (LogServiceValue != null)        return LogServiceValue;
                else                                throw new Exception("LogService not initialized");
            }
            internal set { LogServiceValue = value; }
        }

        /// <summary>
        /// <para>Mail Service</para>
        /// </summary>
        public IMailServiceInterface MailService
        {
            get
            {
                if (MailServiceValue != null)       return MailServiceValue;
                else                                throw new Exception("MailService not initialized");
            }
            internal set { MailServiceValue = value; }
        }

        /// <summary>
        /// <para>Memory Service</para>
        /// </summary>
        public IMemoryServiceInterface MemoryService
        {
            get
            {
                if (MemoryServiceValue != null)     return MemoryServiceValue;
                else throw                          new Exception("MemoryService not initialized");
            }
            internal set { MemoryServiceValue = value; }
        }

        /// <summary>
        /// <para>PubSub Service</para>
        /// </summary>
        public IPubSubServiceInterface PubSubService
        {
            get
            {
                if (PubSubServiceValue != null)     return PubSubServiceValue;
                else                                throw new Exception("PubSubService not initialized");
            }
            internal set { PubSubServiceValue = value; }
        }

        /// <summary>
        /// <para>SMS Service</para>
        /// </summary>
        public ISMSServiceInterface SMSService
        {
            get
            {
                if (SMSServiceValue != null)        return SMSServiceValue;
                else                                throw new Exception("SMSService not initialized");
            }
            internal set { SMSServiceValue = value; }
        }

        /// <summary>
        /// <para>Tracing Service</para>
        /// </summary>
        public ITracingServiceInterface TracingService
        {
            get
            {
                if (TracingServiceValue != null)    return TracingServiceValue;
                else                                throw new Exception("TracingService not initialized");
            }
            internal set { TracingServiceValue = value; }
        }

        /// <summary>
        /// <para>VM Service</para>
        /// </summary>
        public IVMServiceInterface VMService
        {
            get
            {
                if (VMServiceValue != null)         return VMServiceValue;
                else                                throw new Exception("VMService not initialized");
            }
            internal set { VMServiceValue = value; }
        }

        private IDatabaseServiceInterface DatabaseServiceValue = null;
        private IFileServiceInterface FileServiceValue = null;
        private ILogServiceInterface LogServiceValue = null;
        private IMailServiceInterface MailServiceValue = null;
        private IMemoryServiceInterface MemoryServiceValue = null;
        private IPubSubServiceInterface PubSubServiceValue = null;
        private ISMSServiceInterface SMSServiceValue = null;
        private ITracingServiceInterface TracingServiceValue = null;
        private IVMServiceInterface VMServiceValue = null;
    }
}