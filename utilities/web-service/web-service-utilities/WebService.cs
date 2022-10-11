/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using CommonUtilities;
using static WebServiceUtilities.WebPrefixStructure;

namespace WebServiceUtilities
{
    public class WebService
    {
        private readonly WebPrefixStructure[] PrefixesToListen;

        private readonly HttpListener Listener = new HttpListener();

        private readonly List<string> ServerNames = new List<string>()
        {
            "http://localhost",
            "http://127.0.0.1"
        };
        private readonly int ServerPort = 8080;

        public WebService(WebPrefixStructure[] _PrefixesToListen, int _ServerPort = 8080, string _OverrideServerNames = null)
        {
            if (_PrefixesToListen == null || _PrefixesToListen.Length == 0)
                throw new ArgumentException("PrefixesToListen");

            if (_OverrideServerNames == null)
            {
                _OverrideServerNames = "http://*";
            }

            ServerPort = _ServerPort;

            if (_OverrideServerNames != null && _OverrideServerNames.Length > 0)
            {
                string[] _OverrideServerNameArray = _OverrideServerNames.Split(';');
                if (_OverrideServerNameArray != null && _OverrideServerNameArray.Length > 0)
                {
                    ServerNames.Clear();
                    foreach (var _OverrideServerName in _OverrideServerNameArray)
                    {
                        ServerNames.Add(_OverrideServerName);
                    }
                }
            }

            foreach (string ServerName in ServerNames)
            {
                Listener.Prefixes.Add($"{ServerName}:{ServerPort}/");
            }

            if (Listener.Prefixes.Count == 0)
                throw new ArgumentException("Invalid prefixes (Count 0)");

            PrefixesToListen = _PrefixesToListen;
        }

        private bool LookForListenersFromRequest(out WebServiceBase _Callback, out WebSocketListenParameters _WebSocketListenParameters, HttpListenerContext _Context)
        {
            var LongestMatch = new Tuple<string, Func<WebServiceBase>, WebSocketListenParameters>(null, null, null);
            int LongestLength = 0;

            foreach (var CurrentPrefixes in PrefixesToListen)
            {
                if (CurrentPrefixes != null)
                {
                    if (CurrentPrefixes.GetCallbackFromRequest(out Func<WebServiceBase> _CallbackInitializer, out string _MatchedPrefix, out WebSocketListenParameters WSListenParameters, _Context))
                    {
                        if (_MatchedPrefix.Length > LongestLength)
                        {
                            LongestLength = _MatchedPrefix.Length;
                            LongestMatch = new Tuple<string, Func<WebServiceBase>, WebSocketListenParameters>(_MatchedPrefix, _CallbackInitializer, WSListenParameters);
                        }
                    }
                }
            }

            if (LongestLength > 0)
            {
                _Callback = LongestMatch.Item2.Invoke();
                _Callback.InitializeWebService(_Context, LongestMatch.Item1);
                _WebSocketListenParameters = LongestMatch.Item3;
                return true;
            }

            _Callback = null;
            _WebSocketListenParameters = null;
            return false;
        }

        public void Run(Action<string> _ServerLogAction = null)
        {
            var bStartSucceed = new Atomicable<bool>(false);
            var WaitForFirstSuccess = new ManualResetEvent(false);
            TaskWrapper.Run(() =>
            {
                var WaitForException = new ManualResetEvent(false);
                int FailureCount = 0;
                do
                {
                    try
                    {
                        lock (Listener)
                        {
                            Listener.Start();
                        }

                        bStartSucceed.Set(true);
                        try
                        {
                            WaitForFirstSuccess.Set();
                        }
                        catch (Exception) {}

                        FailureCount = 0;

                        try
                        {
                            WaitForException.WaitOne();
                            //Do not close WaitForException! Can be reused.
                        }
                        catch (Exception) { }
                    }
                    catch (Exception e)
                    {
                        _ServerLogAction?.Invoke($"WebService->Run->HttpListener->Start: {e.Message}, trace: {e.Message}");

                        try
                        {
                            WaitForException.Set();
                        }
                        catch (Exception) { }

                        Thread.Sleep(1000);
                    }

                } while (++FailureCount < 10);

                try
                {
                    WaitForFirstSuccess.Set(); //When exhausted
                }
                catch (Exception) { }
            });

            try
            {
                WaitForFirstSuccess.WaitOne();
                //Do not close WaitForFirstSuccess! Can be reused.
            }
            catch (Exception) {}

            if (!bStartSucceed.Get())
            {
                _ServerLogAction?.Invoke("WebService->Run: HttpListener.Start() has failed.");
                return;
            }

            TaskWrapper.Run(() =>
            {
                _ServerLogAction?.Invoke($"WebService->Run: Server is running. Listening port {ServerPort}");

                while (Listener.IsListening)
                {
                    HttpListenerContext Context = null;

                    int FailureCount = 0;
                    bool bSuccess;
                    do
                    {
                        try
                        {
                            lock (Listener)
                            {
                                Context = Listener.GetContext();
                            }
                            bSuccess = true;
                            FailureCount = 0;
                        }
                        catch (Exception e)
                        {
                            _ServerLogAction?.Invoke($"WebService->Run->HttpListener->GetContext: {e.Message}, trace: {e.Message}");
                            bSuccess = false;
                            Thread.Sleep(1000);
                        }

                    } while (!bSuccess && ++FailureCount < 10);

                    if (Context == null) continue;

                    TaskWrapper.Run(() =>
                    {
                        if (Context == null) return;
                        try
                        {
                            Context.Response.AppendHeader("Access-Control-Allow-Origin", "*");

                            bool bIsWebhookRequest =
                                WebUtilities.DoesContextContainHeader(out List<string> _, out string _, Context, "webhook-request-callback")
                                && WebUtilities.DoesContextContainHeader(out List<string> _, out string _, Context, "webhook-request-origin");

                            Context.Response.AppendHeader("Access-Control-Expose-Headers", "*");
                            
                            if (Context.Request.HttpMethod == "OPTIONS" && !bIsWebhookRequest)
                            {
                                Context.Response.AppendHeader("Access-Control-Allow-Headers", "*");
                                Context.Response.AppendHeader("Access-Control-Allow-Credentials", "true");
                                Context.Response.AppendHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH");
                                Context.Response.AppendHeader("Access-Control-Max-Age", "-1");
                                Context.Response.StatusCode = WebResponse.Status_OK_Code;
                            }
                            else
                            {
                                if (PrefixesToListen == null)
                                {
                                    _ServerLogAction?.Invoke("WebService->Run: PrefixesToListen is null.");
                                    WriteInternalError(Context.Response, "Code: WS-PTLN.");
                                    return;
                                }
                                if (!LookForListenersFromRequest(out WebServiceBase _Callback, out WebSocketListenParameters _WSListenParameters, Context))
                                {
                                    if (Context.Request.RawUrl.EndsWith("/ping"))
                                    {
                                        WriteOK(Context.Response, "pong");
                                        return;
                                    }
                                    _ServerLogAction?.Invoke($"WebService->Run: Request is not being listened. Request: {Context.Request.RawUrl}");
                                    WriteNotFound(Context.Response, "Request is not being listened.");
                                    return;
                                }

                                //WS part starts.
                                WebSocket WS = null;
                                try
                                {
                                    WebSocketContext WSContext = null;
                                    if (Context.Request.IsWebSocketRequest)
                                    {
                                        try
                                        {
                                            // Calling `AcceptWebSocketAsync` on the `HttpListenerContext` will accept the WebSocket connection,
                                            // sending the required 101 response to the client and return an instance of `WebSocketContext`.
                                            // IMPORTANT!: So do not use Http Response write/alter functionality from here on!
                                            using (var AcceptSocketTask = Context.AcceptWebSocketAsync(subProtocol: null))
                                            {
                                                AcceptSocketTask.Wait();
                                                WSContext = AcceptSocketTask.Result;
                                                WS = WSContext.WebSocket;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            try
                                            {
                                                WriteInternalError(Context.Response, $"An internal error has occured during websocket accept: {e.Message}");
                                            }
                                            catch (Exception) { }

                                            _ServerLogAction?.Invoke($"Exception in the request handle during websocket accept: {e.Message}, trace: {e.StackTrace}");
                                            return;
                                        }
                                    }
                                    if (_WSListenParameters != null)
                                    {
                                        if (_WSListenParameters.IsListenForWebSocketOnly() && WSContext == null)
                                        {
                                            WriteBadRequest(Context.Response, "Only websocket requests are accepted.");
                                            return;
                                        }
                                        else if (WSContext != null)
                                        {
                                            if (!(_Callback is WebAndWebSocketServiceBase))
                                            {
                                                using (var CloseTask = WS.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, "An internal error has occurred.", CancellationToken.None))
                                                {
                                                    CloseTask.Wait();
                                                }
                                                _ServerLogAction?.Invoke($"WebService->Error: {Context.Request.RawUrl} supposed to be listened by a WebAndWebSocketServiceBase, but it is not.");
                                                return;
                                            }

                                            (_Callback as WebAndWebSocketServiceBase).OnWebSocketRequest_Internal(WSContext, _ServerLogAction);

                                            return;
                                        }
                                    }
                                    else if (WSContext != null)
                                    {
                                        using (var CloseTask = WS.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "Only HTTP requests are accepted for this endpoint.", CancellationToken.None))
                                        {
                                            CloseTask.Wait();
                                        }
                                        return;
                                    }
                                }
                                catch (Exception e)
                                {
                                    _ServerLogAction?.Invoke($"Exception handled in the request handle during websocket process: {e.Message}, trace: {e.StackTrace}");
                                    return;
                                }
                                finally
                                {
                                    // In case its not closed.
                                    try
                                    {
                                        using (var CloseTask = WS.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None))
                                        {
                                            CloseTask.Wait();
                                        }
                                    }
                                    catch (Exception) { }

                                    try { WS?.Dispose(); } catch (Exception) { }
                                }
                                //WS part ends.
                                //From now on it cannot be a WS request anymore.

                                var Response = _Callback.ProcessRequest(Context, _ServerLogAction);

                                Context.Response.StatusCode = Response.StatusCode;

                                foreach (var CurrentHeader in Response.Headers)
                                {
                                    foreach (var Value in CurrentHeader.Value)
                                    {
                                        if (CurrentHeader.Key.ToLower() != "access-control-allow-origin")
                                        {
                                            Context.Response.AppendHeader(CurrentHeader.Key, Value);
                                        }
                                    }
                                }

                                if (Response.bRedirect)
                                {
                                    Context.Response.Redirect(Response.URLIfRedirect);
                                }
                                else
                                {
                                    if (Response.ResponseContent != null)
                                    {
                                        if (Response.ResponseContentType != null)
                                        {
                                            Context.Response.ContentType = Response.ResponseContentType;
                                        }

                                        if (Response.ResponseContent.Type == EStringOrStreamEnum.String)
                                        {
                                            byte[] Buffer = Encoding.UTF8.GetBytes(Response.ResponseContent.String);
                                            if (Buffer != null)
                                            {
                                                Context.Response.ContentLength64 = Buffer.Length;
                                                if (Buffer.Length > 0)
                                                {
                                                    Context.Response.OutputStream.Write(Buffer, 0, Buffer.Length);
                                                }
                                            }
                                            else
                                            {
                                                Context.Response.ContentLength64 = 0;
                                            }
                                        }
                                        else if (Response.ResponseContent.Type == EStringOrStreamEnum.Stream)
                                        {
                                            if (Response.ResponseContent.Stream != Context.Response.OutputStream)
                                            {
                                                if (Response.ResponseContent.Stream != null && Response.ResponseContent.StreamLength > 0)
                                                {
                                                    Context.Response.ContentLength64 = Response.ResponseContent.StreamLength;
                                                    Response.ResponseContent.Stream.CopyTo(Context.Response.OutputStream);
                                                }
                                                else
                                                {
                                                    _ServerLogAction?.Invoke($"WebService->Error: Response is stream, but stream object is {(Response.ResponseContent.Stream == null ? "null" : "valid")} and content length is {Response.ResponseContent.StreamLength}");
                                                    WriteInternalError(Context.Response, "Code: WS-STRMINV.");
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Context.Response.ContentLength64 = 0;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                WriteInternalError(Context.Response, $"An unexpected internal error has occured: {e.Message}");
                            }
                            catch (Exception) { }

                            _ServerLogAction?.Invoke($"Uncaught exception in the request handle: {e.Message}, trace: {e.StackTrace}");
                        }
                        finally
                        {
                            //Always close the streams
                            try { Context.Response.OutputStream.Close(); } catch (Exception) { }
                            try { Context.Response.OutputStream.Dispose(); } catch (Exception) { }
                            //try { Context.Response.Close(); } catch (Exception) { }
                        }
                    });
                }
            });
        }

        private static void WriteBadRequest(HttpListenerResponse _WriteTo, string _CustomMessage)
        {
            string Resp = WebResponse.Error_BadRequest_String(_CustomMessage);
            byte[] Buff = Encoding.UTF8.GetBytes(Resp);

            _WriteTo.ContentType = WebResponse.Error_BadRequest_ContentType;
            _WriteTo.StatusCode = WebResponse.Error_BadRequest_Code;
            _WriteTo.ContentLength64 = Buff.Length;
            _WriteTo.OutputStream.Write(Buff, 0, Buff.Length);
        }

        private static void WriteInternalError(HttpListenerResponse _WriteTo, string _CustomMessage)
        {
            string Resp = WebResponse.Error_InternalError_String(_CustomMessage);
            byte[] Buff = Encoding.UTF8.GetBytes(Resp);

            _WriteTo.ContentType = WebResponse.Error_InternalError_ContentType;
            _WriteTo.StatusCode = WebResponse.Error_InternalError_Code;
            _WriteTo.ContentLength64 = Buff.Length;
            _WriteTo.OutputStream.Write(Buff, 0, Buff.Length);
        }
        private static void WriteNotFound(HttpListenerResponse _WriteTo, string _CustomMessage)
        {
            string Resp = WebResponse.Error_NotFound_String(_CustomMessage);
            byte[] Buff = Encoding.UTF8.GetBytes(Resp);

            _WriteTo.ContentType = WebResponse.Error_NotFound_ContentType;
            _WriteTo.StatusCode = WebResponse.Error_NotFound_Code;
            _WriteTo.ContentLength64 = Buff.Length;
            _WriteTo.OutputStream.Write(Buff, 0, Buff.Length);
        }
        private static void WriteOK(HttpListenerResponse _WriteTo, string _CustomMessage)
        {
            string Resp = WebResponse.Status_Success_String(_CustomMessage);
            byte[] Buff = Encoding.UTF8.GetBytes(Resp);

            _WriteTo.ContentType = WebResponse.Status_Success_ContentType;
            _WriteTo.StatusCode = WebResponse.Status_OK_Code;
            _WriteTo.ContentLength64 = Buff.Length;
            _WriteTo.OutputStream.Write(Buff, 0, Buff.Length);
        }

        public void Stop()
        {
            if (Listener != null)
            {
                try { Listener.Stop(); } catch (Exception) { }
                try { Listener.Close(); } catch (Exception) { }
            }
        }
    }
}