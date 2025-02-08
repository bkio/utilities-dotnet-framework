/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using CommonUtilities;
using Elastic.Clients.Elasticsearch;

namespace CloudServiceUtilities.LogServices
{
    public class LogServiceElastic : ILogServiceInterface
    {
        public LogServiceElastic(string _ElasticSearchConnectionString)
        {
            EsClient = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(_ElasticSearchConnectionString))
                .DisableDirectStreaming()
                .PrettyJson());
        }
        private readonly ElasticsearchClient EsClient;

        public bool HasInitializationSucceed()
        {
            try
            {
                var PingTask = EsClient.PingAsync();
                PingTask.Wait();
                return PingTask.Result.IsSuccess();
            }
            catch
            {
                return false;
            }
        }

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

            string IndexName = Utility.SanitizeElasticIndexName($"{_LogGroupName}-{_LogStreamName}");

            var Entries = _Messages.Select(Message => new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Severity = Message.LogType.ToString(),
                Message = Message.Message
            }).ToList();

            try
            {
                var IndexTask = EsClient.IndexManyAsync(Entries, IndexName);
                IndexTask.Wait();
                if (!IndexTask.Result.IsSuccess())
                {
                    throw new Exception(IndexTask.Result.DebugInformation);
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"Elasticsearch Write Failed: {e.Message}");
                return false;
            }

            return true;
        }
        public bool GetLogs(
            string _LogGroupName, 
            string _LogStreamName, 
            out List<LogParametersStruct> _Logs, 
            out string _NewPageToken, 
            string _PreviousPageToken = null, 
            int _PageSize = 20, 
            Action<string> _ErrorMessageAction = null)
        {
            _Logs = new List<LogParametersStruct>();
            _NewPageToken = null;

            string IndexName = Utility.SanitizeElasticIndexName($"{_LogGroupName}-{_LogStreamName}");

            var FromValue = 0;
            if (!string.IsNullOrEmpty(_PreviousPageToken) && !int.TryParse(_PreviousPageToken, out FromValue))
            {
                _ErrorMessageAction?.Invoke("Invalid previous page token. Resetting to 0.");
                FromValue = 0;
            }

            try
            {
                var SearchTask = EsClient.SearchAsync<LogEntry>(search => search
                    .Index(IndexName)
                    .Sort(sort => sort.Field(Infer.Field<LogEntry>(field => field.Timestamp), field_sort => field_sort.Order(SortOrder.Desc)))
                    .From(FromValue)
                    .Size(_PageSize));

                SearchTask.Wait();

                if (!SearchTask.Result.IsSuccess())
                {
                    throw new Exception($"Elasticsearch query failed: {SearchTask.Result.DebugInformation}");
                }

                foreach (var Hit in SearchTask.Result.Hits)
                {
                    var Entry = Hit.Source;
                    if (Entry == null) continue;

                    _Logs.Add(new LogParametersStruct(Entry.Severity?.ToLower() switch
                    {
                        "debug" => ELogServiceLogType.Debug,
                        "info" => ELogServiceLogType.Info,
                        "warning" => ELogServiceLogType.Warning,
                        "error" => ELogServiceLogType.Error,
                        "critical" => ELogServiceLogType.Critical,
                        _ => ELogServiceLogType.Info
                    }, Entry.Message));
                }

                if (SearchTask.Result.Hits.Count > 0)
                {
                    _NewPageToken = (FromValue + _PageSize).ToString();
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"Error retrieving logs: {ex.Message}");
                return false;
            }
            return true;
        }

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Severity { get; set; }
            public string Message { get; set; }
        }
    }
}