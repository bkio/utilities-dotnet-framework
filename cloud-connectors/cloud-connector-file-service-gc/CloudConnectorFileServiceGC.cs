/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.FileServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_FileService_GC
    {
        public static bool WithFileService(this CloudConnector _Connector)
        {
            /*
            * File service initialization
            */
            _Connector.FileService = new FileServiceGC(
                _Connector.RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"],

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