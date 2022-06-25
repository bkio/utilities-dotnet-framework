/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.DatabaseServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_DatabaseService_GC
    {
        public static bool WithDatabaseService(this CloudConnector _Connector)
        {
            /*
            * File service initialization
            */
            _Connector.DatabaseService = new DatabaseServiceGC(
                _Connector.RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"],

                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.DatabaseService == null || !_Connector.DatabaseService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Database service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}