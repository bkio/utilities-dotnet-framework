/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.FileServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AZ_STORAGE_SERVICE_URL, AZ_STORAGE_ACCOUNT_NAME, AZ_STORAGE_ACCOUNT_ACCESS_KEY, AZ_RESOURCE_GROUP_NAME, AZ_RESOURCE_GROUP_LOCATION, AZ_CLIENT_ID, AZ_CLIENT_SECRET, AZ_SUBSCRIPTION_ID, AZ_TENANT_ID  must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_FileService_AZ
    {
        public static bool WithFileService(this CloudConnector _Connector)
        {
            /*
            * File service initialization
            */
            _Connector.FileService = new FileServiceAZ(
                _Connector.RequiredEnvironmentVariables["AZ_STORAGE_SERVICE_URL"],
                _Connector.RequiredEnvironmentVariables["AZ_STORAGE_ACCOUNT_NAME"],
                _Connector.RequiredEnvironmentVariables["AZ_STORAGE_ACCOUNT_ACCESS_KEY"],
                _Connector.RequiredEnvironmentVariables["AZ_RESOURCE_GROUP_NAME"],
                _Connector.RequiredEnvironmentVariables["AZ_RESOURCE_GROUP_LOCATION"],
                _Connector.RequiredEnvironmentVariables["AZ_CLIENT_ID"],
                _Connector.RequiredEnvironmentVariables["AZ_CLIENT_SECRET"],
                _Connector.RequiredEnvironmentVariables["AZ_SUBSCRIPTION_ID"],
                _Connector.RequiredEnvironmentVariables["AZ_TENANT_ID"],

                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.FileService == null || !_Connector.FileService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "File service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}
