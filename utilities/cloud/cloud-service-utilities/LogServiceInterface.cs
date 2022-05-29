/// Copyright 2022- Burak Kara, All rights reserved.

using System;

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

    public struct LogParametersStruct
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
        public static System.Collections.Generic.List<LogParametersStruct> Single(ELogServiceLogType _LogType, string _Message)
        {
            return new System.Collections.Generic.List<LogParametersStruct>()
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
            System.Collections.Generic.List<LogParametersStruct> _Messages,
            string _LogGroupName,
            string _LogStreamName,
            bool _bAsync = true,
            Action<string> _ErrorMessageAction = null);
    }
}
