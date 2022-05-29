/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Net;
using WebServiceUtilities.PubSubUsers;

namespace WebServiceUtilities.PubSubDatabaseUsers
{
    public abstract class PubSubServiceBaseTimeoutable : InternalWebServiceBaseTimeoutable
    {
        public PubSubServiceBaseTimeoutable(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
        }

        protected override WebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return PubSubServiceBaseCommon.OnRequest(_Context, "PubSubServiceBaseTimeoutableDeliveryEnsurerUser",
                (PubSubAction _Action) =>
                {
                    var bResult = Handle(_Context, _Action, _ErrorMessageAction);
                    return bResult;

                }, _ErrorMessageAction);
        }

        protected abstract bool Handle(HttpListenerContext _Context, PubSubAction _DeserializedAction, Action<string> _ErrorMessageAction = null);
    }
}