/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.LogServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_LogService_GC
    {
        public static bool WithLogService(this CloudConnector _Connector)
        {
            /*
            * Logging service initialization
            */
            _Connector.LogService = new LogServiceGC(_Connector.RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"],
                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.LogService == null || !_Connector.LogService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Logging service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}
