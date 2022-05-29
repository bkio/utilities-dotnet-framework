/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.FileServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_FileService_AWS
    {
        public static bool WithFileService(this CloudConnector _Connector)
        {
            /*
            * File service initialization
            */
            _Connector.FileService = new FileServiceAWS(
                _Connector.RequiredEnvironmentVariables["AWS_ACCESS_KEY"],
                _Connector.RequiredEnvironmentVariables["AWS_SECRET_KEY"],
                _Connector.RequiredEnvironmentVariables["AWS_REGION"],

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