﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Net;

namespace CloudServiceUtilities
{
    public interface ITracingServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        /// 
        /// <para>On_FromClientToGateway_Received</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromClientToGateway_Received(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromGatewayToService_Sent</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromGatewayToService_Sent(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromGatewayToService_Received</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromGatewayToService_Received(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromServiceToGateway_Sent</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromServiceToGateway_Sent(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromServiceToGateway_Received</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromServiceToGateway_Received(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromServiceToService_Sent</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromServiceToService_Sent(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromGatewayToClient_Sent</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromGatewayToClient_Sent(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>On_FromServiceToService_Received</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Context"/>                       Http context</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void On_FromServiceToService_Received(
            HttpListenerContext _Context,
            Action<string> _ErrorMessageAction = null);
    }
}