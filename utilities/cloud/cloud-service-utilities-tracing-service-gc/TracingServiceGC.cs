﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Timers;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Trace.V2;
using Google.Protobuf.WellKnownTypes;
using Grpc.Auth;
using CommonUtilities;

namespace CloudServiceUtilities.TracingServices
{
    public class TracingServiceGC : ITracingServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly string ProgramUniqueID;

        private readonly Google.Api.Gax.ResourceNames.ProjectName ProjectName;

        private readonly TraceServiceClient TraceClient;

        private readonly ServiceAccountCredential Credential;

        private readonly List<Span> Spans = new List<Span>();

        private readonly Timer UploadTimer;

        private readonly Action<string> ErrorMessageAction;

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        /// 
        /// <para>TracingServiceGC: Parametered Constructor for Managed Service by Google</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProjectID"/>              GC Project ID</para>
        /// <para><paramref name="_ProgramUniqueID"/>        Program Unique ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>     Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public TracingServiceGC(
            string _ProjectID,
            string _ProgramUniqueID,
            Action<string> _ErrorMessageAction = null)
        {
            ProgramUniqueID = _ProgramUniqueID;
            ErrorMessageAction = _ErrorMessageAction;

            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                string ApplicationCredentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_BASE64_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null && ApplicationCredentialsBase64 == null)
                {
                    _ErrorMessageAction?.Invoke("TracingServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    var Scopes = new List<string>();
                    foreach (var Scope in TraceServiceClient.DefaultScopes)
                    {
                        if (!Scopes.Contains(Scope))
                        {
                            Scopes.Add(Scope);
                        }
                    }

                    if (ApplicationCredentials == null)
                    {
                        if (ApplicationCredentialsPlain != null && !Utility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        else if (!Utility.Base64Decode(out ApplicationCredentialsPlain, ApplicationCredentialsBase64, _ErrorMessageAction))
                        {
                            throw new Exception("Base64 decode operation for application credentials plain has failed.");
                        }
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                .CreateScoped(
                                Scopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream)
                                .CreateScoped(
                                Scopes.ToArray())
                                .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        TraceClient = new TraceServiceClientBuilder()
                        {
                            ChannelCredentials = Credential.ToChannelCredentials()
                        }.Build();

                        ProjectName = new Google.Api.Gax.ResourceNames.ProjectName(_ProjectID);
                        bInitializationSucceed = TraceClient != null;

                        UploadTimer = new Timer(1_000);
                        UploadTimer.Elapsed += OnTimedEvent;
                        UploadTimer.AutoReset = true;
                        UploadTimer.Enabled = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"TracingServiceGC->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        ///Taken from a random stackoverflow. Should see if there is way to make it cleaner. Used to get
        ///The spanid. Spanids have to be unique within a traceid
        private static readonly Random Random = new Random();
        private static string GetRandomHexNumber(int _Digits)
        {
            var Buffer = new byte[_Digits / 2];
            Random.NextBytes(Buffer);
            var Result = string.Concat(Buffer.Select(x => x.ToString("X2")).ToArray());
            if (_Digits % 2 == 0)
            {
                return Result;
            }
            return $"{Result}{Random.Next(16).ToString("X")}";
        }

        private void OnTimedEvent(object _Source, ElapsedEventArgs _E)
        {
            var TimeSpans = new List<Span>();

            // Keep the lock has tight has possible, grab the values we need then clear out the existing ones.
            lock (Spans)
            {
                TimeSpans.AddRange(Spans);
                Spans.Clear();
            }

            // Just in case things back up, ensure we write each one without clobbering via threads
            lock (TraceClient)
            {
                if (!TimeSpans.Any())
                {
                    return;
                }

                try
                {
                    TraceClient.BatchWriteSpans(ProjectName, TimeSpans);
                }
                catch (Exception e)
                {
                    ErrorMessageAction?.Invoke($"TracingServiceGC->OnTimedEvent: {e.Message}, Trace: {e.StackTrace}");
                }
            }
        }

        private void AddTrace(HttpListenerContext _Context, bool _bNewSpan, Action<string> _ErrorMessageAction = null)
        {
            //Start
            if (_bNewSpan)
            {
                _Context.Request.Headers.Set("span-start-time", DateTime.Now.ToString());
            }
            //End
            else
            {
                var TraceID = _Context.Request.Headers.Get("trace-id");
                if (TraceID == null || TraceID.Length == 0)
                {
                    //It is a new trace
                    TraceID = Guid.NewGuid().ToString("N");
                }

                string ParentSpanID = null;
                string SpanID = _Context.Request.Headers.Get("span-id");
                if (SpanID != null && SpanID.Length > 0)
                {
                    ParentSpanID = SpanID;
                }
                SpanID = GetRandomHexNumber(16);

                _Context.Request.Headers.Set("trace-id", TraceID);
                _Context.Request.Headers.Set("span-id", SpanID);

                var LegitSpanName = new SpanName(ProjectName.ProjectId, TraceID, SpanID);
                var TruncString = new TruncatableString
                {
                    Value = $"{_Context.Request.HttpMethod}->{_Context.Request.Url.AbsolutePath}",
                    TruncatedByteCount = 0
                };

                Timestamp StartTime = null;
                try
                {
                    StartTime = Timestamp.FromDateTimeOffset(DateTime.Parse(_Context.Request.Headers.Get("span-start-time")));
                }
                catch (Exception ex)
                {
                    _ErrorMessageAction?.Invoke($"TracingServiceGC->AddTrace: { ex.Message}, Trace: {ex.StackTrace}");
                }

                var Span = new Span
                {
                    SpanName = LegitSpanName,
                    DisplayName = TruncString,
                    SpanId = SpanID,
                    StartTime = StartTime ?? Timestamp.FromDateTimeOffset(DateTime.Now),
                    EndTime = Timestamp.FromDateTimeOffset(DateTime.Now),
                    Attributes = new Span.Types.Attributes()
                };
                if (ParentSpanID != null)
                {
                    Span.ParentSpanId = ParentSpanID;
                }

                AddEntryToSpan(Span, "Service Name", ProgramUniqueID);
                AddEntryToSpan(Span, "HTTP Method", _Context.Request.HttpMethod);
                AddEntryToSpan(Span, "HTTP URL", _Context.Request.Url.AbsoluteUri);
                AddEntryToSpan(Span, "HTTP Path", _Context.Request.Url.AbsolutePath);

                lock (Spans)
                {
                    Spans.Add(Span);
                }
            }
        }
        private static void AddEntryToSpan(Span _Span, string _Key, string _Value)
        {
            _Span.Attributes.AttributeMap.Add(_Key, new AttributeValue()
            {
                StringValue = new TruncatableString
                {
                    Value = _Value,
                    TruncatedByteCount = 0
                }
            });
        }

        /// <summary>
        ///
        /// <para>On_FromClientToGateway_Received:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromClientToGateway_Received"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromClientToGateway_Received(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, true, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>On_FromGatewayToService_Sent:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromGatewayToService_Sent"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromGatewayToService_Sent(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, false, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>On_FromGatewayToService_Received:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromGatewayToService_Received"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromGatewayToService_Received(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, true, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>On_FromServiceToService_Sent:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromServiceToService_Sent"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromServiceToService_Sent(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, false, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>On_FromServiceToService_Received:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromServiceToService_Received"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromServiceToService_Received(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, true, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>On_FromServiceToGateway_Sent:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromGatewayToService_Received"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromServiceToGateway_Sent(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, false, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>On_FromServiceToGateway_Received:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromGatewayToService_Received"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromServiceToGateway_Received(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, true, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>On_FromGatewayToClient_Sent:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.On_FromGatewayToClient_Sent"/> for detailed documentation</para>
        ///
        /// </summary>
        public void On_FromGatewayToClient_Sent(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            AddTrace(_Context, false, _ErrorMessageAction);
        }
    }
}