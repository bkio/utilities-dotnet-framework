/// Copyright 2022- Burak Kara, All rights reserved.

using CloudServiceUtilities;
using CloudServiceUtilities.MailServices;
using CommonUtilities;

namespace CloudConnectors
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>SENDGRID_API_KEY_BASE64, SENDGRID_SENDER_EMAIL_BASE64, SENDGRID_SENDER_NAME_BASE64 must be provided and valid.</para>
    /// 
    /// </summary>
    public static class CloudConnectorExtensions_MailService_SendGrid
    {
        public static bool WithMailService(this CloudConnector _Connector)
        {
            /*
            * Mail service initialization
            */
            if (!_Connector.RequiredEnvironmentVariables.ContainsKey("SENDGRID_API_KEY_BASE64") ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("SENDGRID_SENDER_EMAIL_BASE64") ||
                !_Connector.RequiredEnvironmentVariables.ContainsKey("SENDGRID_SENDER_NAME_BASE64"))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "SENDGRID_API_KEY, SENDGRID_SENDER_EMAIL, SENDGRID_SENDER_NAME parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            if (!Utility.Base64Decode(out var SendGridAPIKey,
                _Connector.RequiredEnvironmentVariables["SENDGRID_API_KEY_BASE64"],
                (Err) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Err), _Connector.ProgramID, "Initialization");
                }))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "SENDGRID_API_KEY, SENDGRID_SENDER_EMAIL, SENDGRID_SENDER_NAME parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }
            if (!Utility.Base64Decode(out var SenderEmail,
                _Connector.RequiredEnvironmentVariables["SENDGRID_SENDER_EMAIL_BASE64"],
                (Err) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Err), _Connector.ProgramID, "Initialization");
                }))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "SENDGRID_API_KEY, SENDGRID_SENDER_EMAIL, SENDGRID_SENDER_NAME parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }
            if (!Utility.Base64Decode(out var SenderName,
                _Connector.RequiredEnvironmentVariables["SENDGRID_SENDER_NAME_BASE64"],
                (Err) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Err), _Connector.ProgramID, "Initialization");
                }))
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "SENDGRID_API_KEY, SENDGRID_SENDER_EMAIL, SENDGRID_SENDER_NAME parameters must be provided and valid."), _Connector.ProgramID, "Initialization");
                return false;
            }

            _Connector.MailService = new MailServiceSendGrid(
                SendGridAPIKey,
                SenderEmail,
                SenderName,
                (string Message) =>
                {
                    _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, Message), _Connector.ProgramID, "Initialization");
                });

            if (_Connector.MailService == null || !_Connector.MailService.HasInitializationSucceed())
            {
                _Connector.LogService.WriteLogs(LogServiceMessageUtility.Single(ELogServiceLogType.Critical, "Mail service initialization has failed."), _Connector.ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}