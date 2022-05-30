/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Net.WebSockets;

namespace WebServiceUtilities
{
    /**
     * 
     * IMPORTANT!: CloseAndDisposeWebSocketConnection must be called somewhere in the derived class!
     * 
     */
    public abstract class WebSocketServiceBase : WebServiceBase
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
                    _WSB.OnWebSocketServiceBaseDestroy(this);
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