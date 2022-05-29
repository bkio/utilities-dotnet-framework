/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.DatabaseServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>((MONGODB_CONNECTION_STRING) or (MONGODB_CLIENT_CONFIG, MONGODB_PASSWORD) or (MONGODB_HOST, MONGODB_PORT)) and MONGODB_DATABASE must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_DatabaseService_MongoDB
    {
        public static bool WithDatabaseService(this CloudConnector _Connector)
        {
            /*
            * File service initialization
            */
            if (!_Connector.RequiredEnvironmentVariables.ContainsKey("MONGODB_DATABASE") 
                || _Connector.RequiredEnvironmentVariables["MONGODB_DATABASE"] == null)
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "((MONGODB_CONNECTION_STRING) or (MONGODB_CLIENT_CONFIG, MONGODB_PASSWORD) or (MONGODB_HOST, MONGODB_PORT)) and MONGODB_DATABASE must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            if (_Connector.RequiredEnvironmentVariables.ContainsKey("MONGODB_CLIENT_CONFIG") 
                && _Connector.RequiredEnvironmentVariables["MONGODB_CLIENT_CONFIG"] != null
                && _Connector.RequiredEnvironmentVariables.ContainsKey("MONGODB_PASSWORD") 
                && _Connector.RequiredEnvironmentVariables["MONGODB_PASSWORD"] != null)
            {
                _Connector.DatabaseService = new DatabaseServiceMongoDB(
                    _Connector.RequiredEnvironmentVariables["MONGODB_CLIENT_CONFIG"], 
                    _Connector.RequiredEnvironmentVariables["MONGODB_PASSWORD"],
                    _Connector.RequiredEnvironmentVariables["MONGODB_DATABASE"],

                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });
            }
            else if (_Connector.RequiredEnvironmentVariables.ContainsKey("MONGODB_CONNECTION_STRING") 
                && _Connector.RequiredEnvironmentVariables["MONGODB_CONNECTION_STRING"] != null)
            {
                _Connector.DatabaseService = new DatabaseServiceMongoDB(
                    _Connector.RequiredEnvironmentVariables["MONGODB_CONNECTION_STRING"],
                    _Connector.RequiredEnvironmentVariables["MONGODB_DATABASE"],

                    (string Message) =>
                    {
                        _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                    });
            }
            else if (_Connector.RequiredEnvironmentVariables.ContainsKey("MONGODB_HOST") 
                && _Connector.RequiredEnvironmentVariables["MONGODB_HOST"] != null
                && _Connector.RequiredEnvironmentVariables.ContainsKey("MONGODB_PORT") 
                && _Connector.RequiredEnvironmentVariables["MONGODB_PORT"] != null
                && int.TryParse(_Connector.RequiredEnvironmentVariables["MONGODB_PORT"], out int MongoDbPort))
            {
                _Connector.DatabaseService = new DatabaseServiceMongoDB(
                    _Connector.RequiredEnvironmentVariables["MONGODB_HOST"], 
                    MongoDbPort, 
                    _Connector.RequiredEnvironmentVariables["MONGODB_DATABASE"],

                    (string Message) =>
                    {
                        _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                    });
            }
            else
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "((MONGODB_CONNECTION_STRING) or (MONGODB_CLIENT_CONFIG, MONGODB_PASSWORD) or (MONGODB_HOST, MONGODB_PORT)) and MONGODB_DATABASE must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            if (_Connector.DatabaseService == null || !_Connector.DatabaseService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Database service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}