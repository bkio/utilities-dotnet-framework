/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using CommonUtilities;

namespace CloudServiceUtilities.LogServices
{
    public class LogServiceAWS : ILogServiceInterface
    {
        /// <summary>
        /// <para>AWS CloudWatch Client that is responsible to serve to this object</para>
        /// </summary>
        private readonly AmazonCloudWatchLogsClient CloudWatchLogsClient;

        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        /// 
        /// <para>LogServiceAWS: Parametered Constructor for Managed Service by Amazon</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_AccessKey"/>                 AWS Access Key</para>
        /// <para><paramref name="_SecretKey"/>                 AWS Secret Key</para>
        /// <para><paramref name="_Region"/>                    AWS Region that CloudWatch Client will connect to (I.E. eu-west-1) </para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public LogServiceAWS(
            string _AccessKey,
            string _SecretKey,
            string _Region,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                CloudWatchLogsClient = new AmazonCloudWatchLogsClient(new Amazon.Runtime.BasicAWSCredentials(_AccessKey, _SecretKey), Amazon.RegionEndpoint.GetBySystemName(_Region));
                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"LogServiceAWS->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        ~LogServiceAWS()
        {
            CloudWatchLogsClient?.Dispose();
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="ILogServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        ///
        /// <para>WriteLogs:</para>
        ///
        /// <para>Writes logs to the logging service</para>
        ///
        /// <para>Check <seealso cref="ILogServiceInterface.WriteLogs"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool WriteLogs(
            List<LogParametersStruct> _Messages,
            string _LogGroupName,
            string _LogStreamName,
            bool _bAsync = true,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Messages == null || _Messages.Count == 0) return false;

            if (_bAsync)
            {
                TaskWrapper.Run(() =>
                {
                    WriteLogs(_Messages, _LogGroupName, _LogStreamName, false, _ErrorMessageAction);
                });
                return true;
            }
            else
            {
                _LogGroupName = Utility.EncodeStringForTagging(_LogGroupName);
                _LogStreamName = Utility.EncodeStringForTagging(_LogStreamName);

                string SequenceToken = null;

                bool bLogStreamAndGroupExists = false;
                try
                {
                    var DescribeStreamRequest = new DescribeLogStreamsRequest(_LogGroupName);
                    using (var CreatedDescribeTask = CloudWatchLogsClient.DescribeLogStreamsAsync(DescribeStreamRequest))
                    {
                        CreatedDescribeTask.Wait();
                        if (CreatedDescribeTask.Result != null && CreatedDescribeTask.Result.LogStreams != null && CreatedDescribeTask.Result.LogStreams.Count > 0)
                        {
                            foreach (var Current in CreatedDescribeTask.Result.LogStreams)
                            {
                                if (Current != null && Current.LogStreamName == _LogStreamName)
                                {
                                    SequenceToken = Current.UploadSequenceToken;
                                    bLogStreamAndGroupExists = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    bLogStreamAndGroupExists = false;
                }

                if (!bLogStreamAndGroupExists)
                {
                    try
                    {
                        var CreateGroupRequest = new CreateLogGroupRequest(_LogGroupName);
                        using (var CreatedGroupTask = CloudWatchLogsClient.CreateLogGroupAsync(CreateGroupRequest))
                        {
                            CreatedGroupTask.Wait();
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is ResourceAlreadyExistsException))
                        {
                            _ErrorMessageAction?.Invoke($"LogServiceAWS->WriteLogs: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }
                    }

                    try
                    {
                        var CreateStreamRequest = new CreateLogStreamRequest(_LogGroupName, _LogStreamName);
                        using (var CreatedStreamTask = CloudWatchLogsClient.CreateLogStreamAsync(CreateStreamRequest))
                        {
                            CreatedStreamTask.Wait();
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is ResourceAlreadyExistsException))
                        {
                            _ErrorMessageAction?.Invoke($"LogServiceAWS->WriteLogs: {e.Message}, Trace: {e.StackTrace}");
                            return false;
                        }
                    }
                }

                var LogEvents = new List<InputLogEvent>();
                foreach (var Message in _Messages)
                {
                    var LogEvent = new InputLogEvent()
                    {
                        Message = Message.Message,
                        Timestamp = DateTime.UtcNow
                    };

                    switch (Message.LogType)
                    {
                        case ELogServiceLogType.Debug:
                            LogEvent.Message = $"Debug-> { LogEvent.Message}";
                            break;
                        case ELogServiceLogType.Info:
                            LogEvent.Message = $"Info-> { LogEvent.Message}";
                            break;
                        case ELogServiceLogType.Warning:
                            LogEvent.Message = $"Warning-> { LogEvent.Message}";
                            break;
                        case ELogServiceLogType.Error:
                            LogEvent.Message = $"Error-> { LogEvent.Message}";
                            break;
                        case ELogServiceLogType.Critical:
                            LogEvent.Message = $"Critical-> { LogEvent.Message}";
                            break;
                    }

                    LogEvents.Add(LogEvent);
                }

                try
                {
                    var PutRequest = new PutLogEventsRequest(_LogGroupName, _LogStreamName, LogEvents)
                    {
                        SequenceToken = SequenceToken
                    };
                    using (var CreatedPutTask = CloudWatchLogsClient.PutLogEventsAsync(PutRequest))
                    {
                        CreatedPutTask.Wait();
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"LogServiceAWS->WriteLogs: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }
                return true;
            }
        }
    }
}