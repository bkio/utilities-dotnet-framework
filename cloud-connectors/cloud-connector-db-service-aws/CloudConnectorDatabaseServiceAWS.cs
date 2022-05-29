/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.DatabaseServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_DatabaseService_AWS
    {
        public static bool WithDatabaseService(this CloudConnector _Connector)
        {
            /*
            * Database service initialization
            */
            _Connector.DatabaseService = new DatabaseServiceAWS(
                _Connector.RequiredEnvironmentVariables["AWS_ACCESS_KEY"], 
                _Connector.RequiredEnvironmentVariables["AWS_SECRET_KEY"],
                _Connector.RequiredEnvironmentVariables["AWS_REGION"],

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