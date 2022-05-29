/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Specialized;
using System.Net;
using zipkin4net;
using zipkin4net.Propagation;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;

namespace CloudServiceUtilities.TracingServices
{
    public class TracingServiceZipkin : ITracingServiceInterface
    {
        private readonly bool bInitializationSucceed = false;

        private readonly string ProgramUniqueID;

        public TracingServiceZipkin(LogServiceLoggerZipkin _Logger, string _ProgramUniqueID, string _ZipkinServerIP, int _ZipkinServerPort, Action<string> _ErrorMessageAction)
        {
            ProgramUniqueID = _ProgramUniqueID;
            try
            {
                TraceManager.SamplingRate = 1.0f;

                var HttpSender = new HttpZipkinSender($"http://{ _ZipkinServerIP}:{_ZipkinServerPort}", "application/json");
                var Tracer = new ZipkinTracer(HttpSender, new JSONSpanSerializer());

                TraceManager.RegisterTracer(Tracer);
                bInitializationSucceed = TraceManager.Start(_Logger);
            }
            catch (Exception e)
            {
                bInitializationSucceed = false;
                _ErrorMessageAction?.Invoke($"TracingServiceZipkin->Constructor: {e.Message}, Trace: {e.StackTrace}");
            }
        }

        ~TracingServiceZipkin()
        {
            TraceManager.Stop();
        }

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

        private readonly IInjector<NameValueCollection> TraceInjector = Propagations.B3String.Injector<NameValueCollection>((Carrier, Key, Value) => Carrier.Add(Key, Value));
        private readonly IExtractor<NameValueCollection> TraceExtractor = Propagations.B3String.Extractor((NameValueCollection Carrier, string Key) => Carrier[Key]);

        private void CommonHttpTraceAnnotations(Trace _Trace, HttpListenerContext _Context)
        {
            _Trace.Record(Annotations.ServiceName(ProgramUniqueID));
            _Trace.Record(Annotations.Rpc(_Context.Request.HttpMethod));
            _Trace.Record(Annotations.Tag("http.host", _Context.Request.Url.Host));
            _Trace.Record(Annotations.Tag("http.url", _Context.Request.Url.AbsoluteUri));
            _Trace.Record(Annotations.Tag("http.path", _Context.Request.Url.AbsolutePath));
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
            var CreatedTrace = Trace.Create();
            TraceInjector.Inject(CreatedTrace.CurrentSpan, _Context.Request.Headers);

            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.ServerRecv());

            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStart($"{ProgramUniqueID}->{_Context.Request.Url.AbsolutePath}"));
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
            var TraceContext = TraceExtractor.Extract(_Context.Request.Headers);
            var CreatedTrace = TraceContext == null ? Trace.Create() : Trace.CreateFromId(TraceContext).Child();
            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStop());
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
            var TraceContext = TraceExtractor.Extract(_Context.Request.Headers);
            var CreatedTrace = TraceContext == null ? Trace.Create() : Trace.CreateFromId(TraceContext).Child();
            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStart($"{ProgramUniqueID}->{_Context.Request.Url.AbsolutePath}"));
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
            var TraceContext = TraceExtractor.Extract(_Context.Request.Headers);
            var CreatedTrace = TraceContext == null ? Trace.Create() : Trace.CreateFromId(TraceContext).Child();
            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStop());
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
            var TraceContext = TraceExtractor.Extract(_Context.Request.Headers);
            var CreatedTrace = TraceContext == null ? Trace.Create() : Trace.CreateFromId(TraceContext).Child();
            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStart($"{ProgramUniqueID}->{_Context.Request.Url.AbsolutePath}"));
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
            var TraceContext = TraceExtractor.Extract(_Context.Request.Headers);
            var CreatedTrace = TraceContext == null ? Trace.Create() : Trace.CreateFromId(TraceContext).Child();
            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStop());
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
            var TraceContext = TraceExtractor.Extract(_Context.Request.Headers);
            var CreatedTrace = TraceContext == null ? Trace.Create() : Trace.CreateFromId(TraceContext).Child();
            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStart($"{ProgramUniqueID}->{_Context.Request.Url.AbsolutePath}"));
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
            var TraceContext = TraceExtractor.Extract(_Context.Request.Headers);
            var CreatedTrace = TraceContext == null ? Trace.Create() : Trace.CreateFromId(TraceContext).Child();

            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.LocalOperationStop());

            CommonHttpTraceAnnotations(CreatedTrace, _Context);
            CreatedTrace.Record(Annotations.ServerSend());
        }
    }
}