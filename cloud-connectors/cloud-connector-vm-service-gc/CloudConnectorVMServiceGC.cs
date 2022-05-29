/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.VMServices;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_CLOUD_COMPUTE_ZONE, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_VMService_GC
    {
        public static bool WithVMService(this CloudConnector _Connector)
        {
            /*
            * VM service initialization
            */
            _Connector.VMService = new VMServiceGC(_Connector.ProgramID, 
                _Connector.RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"], 
                _Connector.RequiredEnvironmentVariables["GOOGLE_CLOUD_COMPUTE_ZONE"],

                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.VMService == null || !_Connector.VMService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "VM service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}