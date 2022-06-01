/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Net.WebSockets;

namespace WebServiceUtilities
{
    /**
     * 
     * IMPORTANT!: This must be used as a websocket listener surely, but also can perform regular web service listener functionality along with websocket.
     * As an example, same listen-prefix can be used for both websocket connection as well as a http request.
     * So it is ok to leave OnRequest blank.
     * 
     * Like;
     * protected override OnRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null) { return WebResponse.StatusOK("OK"); }
     * 
     * But OnWebSocketRequest must be properly implemented, surely!
     * 
     */
    public abstract class WebAndWebSocketServiceBase : WebServiceBase
    {
        internal void OnWebSocketRequest_Internal(WebSocketContext _Context, Action<string> _ErrorMessageAction = null)
        {
            WebSocketObject = new WeakReference<WebSocket>(_Context.WebSocket);
            OnWebSocketRequest(_Context, _ErrorMessageAction);
        }

        protected abstract void OnWebSocketRequest(WebSocketContext _Context, Action<string> _ErrorMessageAction = null);

        public bool IsWebSocketOpen()
        {
            bool bResult = false;
            try
            {
                if (WebSocketObject != null && WebSocketObject.TryGetTarget(out WebSocket _Socket))
                {
                    bResult = _Socket.State == WebSocketState.Open;
                }
            }
            catch (Exception) { }
            return bResult;
        }
        private WeakReference<WebSocket> WebSocketObject;
    }
}