/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;

namespace CloudServiceUtilities
{
    public enum ELogServiceLogType
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public readonly struct LogParametersStruct
    {
        public readonly ELogServiceLogType LogType;
        public readonly string Message;

        public LogParametersStruct(ELogServiceLogType _LogType, string _Message)
        {
            LogType = _LogType;
            Message = _Message;
        }
    }

    public class LogServiceMessageUtility
    {
        public static List<LogParametersStruct> Single(ELogServiceLogType _LogType, string _Message)
        {
            return new List<LogParametersStruct>()
            {
                new LogParametersStruct(_LogType, _Message)
            };
        }
    }

    public interface ILogServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        /// 
        /// <para>WriteLogs</para>
        /// 
        /// <para>Writes logs to the logging service</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Messages"/>                      List of messages to be written</para>
        /// <para><paramref name="_LogGroupName"/>                  Name of the log group (Group)</para>
        /// <para><paramref name="_LogStreamName"/>                 Stream name of the logs (Sub-group)</para>
        /// <para><paramref name="_bAsync"/>                        Sends messages asynchronously if this parameter is set to true</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool WriteLogs(
            List<LogParametersStruct> _Messages,
            string _LogGroupName,
            string _LogStreamName,
            bool _bAsync = true,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        ///
        /// <para>GetLogs:</para>
        ///
        /// <para>Get logs from the logging service</para>
        ///
        /// <para>Parameters:</para>
        /// <para><paramref name="_LogGroupName"/>                  Name of the log group (Group)</para>
        /// <para><paramref name="_LogStreamName"/>                 Stream name of the logs (Sub-group)</para>
        /// <para><paramref name="_Logs"/>                          Returned logs</para>
        /// <para><paramref name="_NewPageToken"/>                  Returned page token which can be used in next page retrieve request</para>
        /// <para><paramref name="_PreviousPageToken"/>             Previously received page token. If null, means first page.</para>
        /// <para><paramref name="_PageSize"/>                      Up to how many logs in this page should be retrieved.</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        ///
        /// </summary>
        bool GetLogs(
            string _LogGroupName,
            string _LogStreamName,
            out List<LogParametersStruct> _Logs,
            out string _NewPageToken,
            string _PreviousPageToken = null,
            int _PageSize = 20,
            Action<string> _ErrorMessageAction = null);
    }
}