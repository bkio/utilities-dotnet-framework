/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.LogServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_LogService_AWS
    {
        public static bool WithLogService(this CloudConnector _Connector)
        {
            /*
            * Logging service initialization
            */
            _Connector.LogService = new LogServiceAWS(
                _Connector.RequiredEnvironmentVariables["AWS_ACCESS_KEY"],
                _Connector.RequiredEnvironmentVariables["AWS_SECRET_KEY"],
                _Connector.RequiredEnvironmentVariables["AWS_REGION"], 

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
