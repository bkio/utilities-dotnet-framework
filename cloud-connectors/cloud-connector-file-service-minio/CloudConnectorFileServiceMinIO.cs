/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.FileServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>MINIO_SERVER_ENDPOINT, MINIO_REGION, MINIO_ACCESS_KEY, MINIO_SECRET_KEY must be provided and valid.</para>
    /// MINIO_SERVER_ENDPOINT: It should be in a format like: http://IP:9000 (or another port). http or https both ok.
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_FileService_MinIO
    {
        public static bool WithFileService(this CloudConnector _Connector)
        {
            /*
            * File service initialization
            */
            _Connector.FileService = new FileServiceAWS(
                _Connector.RequiredEnvironmentVariables["MINIO_SERVER_ENDPOINT"],
                _Connector.RequiredEnvironmentVariables["MINIO_ACCESS_KEY"],
                _Connector.RequiredEnvironmentVariables["MINIO_SECRET_KEY"],
                _Connector.RequiredEnvironmentVariables["MINIO_REGION"],

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