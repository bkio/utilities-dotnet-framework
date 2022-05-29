/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.LogServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>APPINSIGHTS_INSTRUMENTATIONKEY must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_LogService_AZ
    {
        public static bool WithLogService(this CloudConnector _Connector)
        {
            /*
            * Logging service initialization
            */
            _Connector.LogService = new LogServiceAzure(
                _Connector.RequiredEnvironmentVariables["APPINSIGHTS_INSTRUMENTATIONKEY"],

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
