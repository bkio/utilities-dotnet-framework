/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using CommonUtilities;
using WebServiceUtilities.PubSubUsers;

namespace WebServiceUtilities.PubSubDatabaseUsers
{
    public abstract class InternalWebServiceBaseTimeoutable : InternalWebServiceBase
    {
        public readonly WebServiceBaseTimeoutableProcessor InnerProcessor;

        protected InternalWebServiceBaseTimeoutable(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
            InnerProcessor = new WebServiceBaseTimeoutableProcessor(OnRequest_Interruptable);
        }

        protected override WebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return InnerProcessor.ProcessRequest(_Context, _ErrorMessageAction);
        }
        protected abstract WebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }

    public abstract class WebServiceBaseTimeoutable : WebServiceBase
    {
        public readonly WebServiceBaseTimeoutableProcessor InnerProcessor;

        protected WebServiceBaseTimeoutable() 
        {
            InnerProcessor = new WebServiceBaseTimeoutableProcessor(OnRequest_Interruptable);
        }

        protected override WebServiceResponse OnRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return InnerProcessor.ProcessRequest(_Context, _ErrorMessageAction);
        }
        protected abstract WebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }

    public class WebServiceBaseTimeoutableProcessor
    {
        private static readonly HashSet<WeakReference<WebServiceBaseTimeoutableProcessor>> InterruptableWebServiceProcessors = new HashSet<WeakReference<WebServiceBaseTimeoutableProcessor>>();

        private readonly Func<HttpListenerContext, Action<string>, WebServiceResponse> OnRequestCallback;

        public WebServiceBaseTimeoutableProcessor(Func<HttpListenerContext, Action<string>, WebServiceResponse> _OnRequestCallback)
        {
            OnRequestCallback = _OnRequestCallback;
            lock (InterruptableWebServiceProcessors)
            {
                InterruptableWebServiceProcessors.Add(new WeakReference<WebServiceBaseTimeoutableProcessor>(this));
            }
        }
        ~WebServiceBaseTimeoutableProcessor()
        {
            lock (InterruptableWebServiceProcessors)
            {
                foreach (var Weak in InterruptableWebServiceProcessors)
                {
                    if (Weak.TryGetTarget(out WebServiceBaseTimeoutableProcessor Strong) && Strong == this)
                    {
                        InterruptableWebServiceProcessors.Remove(Weak);
                        return;
                    }
                }
            }
            try
            {
                WaitUntilSignal.Close();
            }
            catch (Exception) { }
        }

        public static void OnTimeoutNotificationReceived(PubSubAction_OperationTimeout _Notification)
        {
            lock (InterruptableWebServiceProcessors)
            {
                foreach (var ProcessorWeakPtr in InterruptableWebServiceProcessors)
                {
                    if (ProcessorWeakPtr.TryGetTarget(out WebServiceBaseTimeoutableProcessor Processor))
                    {
                        foreach (var TimeoutStructure in Processor.RelevantTimeoutStructures)
                        {
                            if (TimeoutStructure.Equals(_Notification))
                            {
                                Processor.TimeoutOccurred();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool bDoNotGetDBClearance = false;
        public bool IsDoNotGetDBClearanceSet()
        {
            return bDoNotGetDBClearance;
        }

        private readonly ConcurrentQueue<WebServiceResponse> Responses = new ConcurrentQueue<WebServiceResponse>();
        private readonly ManualResetEvent WaitUntilSignal = new ManualResetEvent(false);
        public WebServiceResponse ProcessRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            bDoNotGetDBClearance =
                WebUtilities.DoesContextContainHeader(out List<string> DNGDBCs, out string _, _Context, "do-not-get-db-clearance")
                && Utility.CheckAndGetFirstStringFromList(DNGDBCs, out string DNGDBC)
                && DNGDBC == "true";

            TaskWrapper.Run(() =>
            {
                var Response =  OnRequestCallback?.Invoke(_Context, _ErrorMessageAction);
                if (Response.HasValue)
                {
                    Responses.Enqueue(Response.Value);
                }

                try
                {
                    WaitUntilSignal.Set();
                }
                catch (Exception) { }
            });

            try
            {
                WaitUntilSignal.WaitOne();
            }
            catch (Exception) { }

            if (!Responses.TryDequeue(out WebServiceResponse FirstResponse))
            {
                FirstResponse = WebResponse.InternalError("Unexpected error in concurrence.");
            }
            return FirstResponse;
        }

        private void TimeoutOccurred()
        {
            Responses.Enqueue(WebResponse.InternalError("Database operation timed out."));
            try
            {
                WaitUntilSignal.Set();
            }
            catch (Exception) { }
        }

        public readonly List<PubSubAction_OperationTimeout> RelevantTimeoutStructures = new List<PubSubAction_OperationTimeout>();
    }
}