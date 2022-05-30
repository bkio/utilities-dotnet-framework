/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Net.WebSockets;

namespace WebServiceUtilities
{
    /**
     * 
     * IMPORTANT!: CloseAndDisposeWebSocketConnection must be called somewhere in the derived class!
     * 
     * IMPORTANT!: This must be used as a websocket listener surely, but also can perform regular web service listener functionality along with websocket.
     * As an example, same listen-prefix can be used for both websocket connection as well as a http request.
     * So it is ok to leave OnRequest blank.
     * 
     * Like;
     * protected override OnRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null) { return WebResponse.StatusOK("OK"); }
     * 
     * But OnWebSocketRequest must be properly implemented and CloseAndDisposeWebSocketConnection must be called somewhere, surely!
     * 
     */
    public abstract class WebAndWebSocketServiceBase : WebServiceBase
    {
        private WeakReference<WebService> OwnerWebService;
        private WebSocketContext OwnedWSContext;

        public void OnWebSocketRequest_Internal(WebSocketContext _Context, WeakReference<WebService> _OwnerWebService, Action<string> _ErrorMessageAction = null)
        {
            OwnerWebService = _OwnerWebService;
            OwnedWSContext = _Context;
            OnWebSocketRequest(_Context, _ErrorMessageAction);
        }

        protected abstract void OnWebSocketRequest(WebSocketContext _Context, Action<string> _ErrorMessageAction = null);

        //Must be called when the operation is finished. Otherwise it will cause memory leak!
        protected void CloseAndDisposeWebSocketConnection()
        {
            try
            {
                if (OwnerWebService.TryGetTarget(out WebService _WSB))
                {
                    _WSB.OnWebAndWebSocketServiceBaseDestroy(this);
                }
            }
            catch (Exception) { }

            try
            {
                OwnedWSContext?.WebSocket?.Dispose();
                OwnedWSContext = null;
            }
            catch (Exception) { }
        }
    }
}