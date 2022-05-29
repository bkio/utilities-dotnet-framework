/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using CommonUtilities;

namespace CloudServiceUtilities
{
    /// <summary>
    /// Interface for abstracting Memory Services to make it usable with multiple solutions, like Redis, Memcached, or multiple systems combined etc. 
    /// </summary>
    public interface IMemoryServiceInterface
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
        /// Blocks the caller thread if failover mechanism is running
        /// </summary>
        void FailoverCheck();

        /// <summary>
        /// 
        /// <para>SetKeyExpireTime</para>
        /// 
        /// <para>Sets given namespace's expire time</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>                Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_TTL"/>                           Time to live</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool SetKeyExpireTime(
            string _MemoryScopeKey,
            TimeSpan _TTL,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>GetKeyExpireTime</para>
        /// 
        /// <para>Gets given namespace's expire time</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>                Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_TTL"/>                           Time to live</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool GetKeyExpireTime(
            string _MemoryScopeKey,
            out TimeSpan _TTL,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>SetKeyValue</para>
        /// 
        /// <para>Sets given keys' values within given namespace and publishes message to [_Domain]:[_SubDomain] topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_KeyValues"/>                 Key-values to be set</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool SetKeyValue(
            string _MemoryScopeKey,
            Tuple<string, PrimitiveType>[] _KeyValues,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>SetKeyValueConditionally</para>
        /// 
        /// <para>Sets given keys' values within given namespace and publishes message to [_Domain]:[_SubDomain] topic;</para>
        /// <para>With a condition; if key does not exist.</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_KeyValue"/>                  Key-value to be set</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Operation success; if condition fails; returns false.</returns>
        /// 
        /// </summary>
        bool SetKeyValueConditionally(
            string _MemoryScopeKey,
            Tuple<string, PrimitiveType> _KeyValue,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>GetKeyValue</para>
        /// 
        /// <para>Gets given key's value within given namespace [_Domain]:[_SubDomain]</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_Key"/>                       Key to be gotten</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Given key's value, null if not found</returns>
        /// 
        /// </summary>
        PrimitiveType GetKeyValue(
            string _MemoryScopeKey,
            string _Key,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>GetKeysValues</para>
        /// 
        /// <para>Gets given keys' values' within given namespace [_Domain]:[_SubDomain]</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_Keys"/>                      Keys to be gotten</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Found values</returns>
        /// 
        /// </summary>
        Dictionary<string, PrimitiveType> GetKeysValues(
            string _MemoryScopeKey, 
            List<string> _Keys,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>GetAllKeyValues</para>
        /// 
        /// <para>Gets all keys and keys' values of given namespace [_Domain]:[_SubDomain]</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  All keys and keys' values for given namespace</returns>
        /// 
        /// </summary>
        Tuple<string, PrimitiveType>[] GetAllKeyValues(
            string _MemoryScopeKey,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>DeleteKey</para>
        /// 
        /// <para>Deletes given key within given namespace [_Domain]:[_SubDomain] and publishes message to [_Domain]:[_SubDomain] topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_Key"/>                       Key to be deleted</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  True if deletion was successful.</returns>
        /// 
        /// </summary>
        bool DeleteKey(
            string _MemoryScopeKey,
            string _Key,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>DeleteAllKeys</para>
        /// 
        /// <para>Deletes all keys for given namespace and publishes message to [_Domain]:[_SubDomain] topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_bWaitUntilCompletion"/>      Whether wait until query is done or fire and forget</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  True if deletion was successful.</returns>
        /// 
        /// </summary>
        bool DeleteAllKeys(
            string _MemoryScopeKey,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>GetKeys</para>
        /// 
        /// <para>Gets all keys of given workspace [_Domain]:[_SubDomain]</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Array of returned keys</returns>
        /// 
        /// </summary>
        string[] GetKeys(
            string _MemoryScopeKey,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>GetKeysCount</para>
        /// 
        /// <para>Returns number of keys of given workspace [_Domain]:[_SubDomain]</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Number of keys of given workspace</returns>
        /// 
        /// </summary>
        long GetKeysCount(
            string _MemoryScopeKey,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>IncrementKeyValues</para>
        /// 
        /// <para>Increments given keys' by given values within given namespace and publishes message to [_Domain]:[_SubDomain] topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_KeysAndIncrementByValues"/>  Key-values to be used for incrementing</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// </summary>
        void IncrementKeyValues(
            string _MemoryScopeKey,
            Tuple<string, long>[] _KeysAndIncrementByValues,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>IncrementKeyByValueAndGet</para>
        /// 
        /// <para>Increments given key by given value within given namespace, publishes message to [_Domain]:[_SubDomain] topic and returns new value</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_string"/>                    Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_KeyValue"/>                  Key-value to be used for incrementing</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Incremented value, zero if fails.</returns>
        /// 
        /// </summary>
        long IncrementKeyByValueAndGet(
            string _string,
            Tuple<string, long> _KeyValue,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>PushToListTail</para>
        /// 
        /// <para>Pushes the value(s) to the tail of given list, returns if push succeeds (If _bAsync is true, always returns true).</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_Values"/>                    Value(s) to push</para>
        /// <para><paramref name="_bPushIfListExists"/>         If true, only pushes if list exists</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bAsync"/>                    Asynchronous execution, will be ignored if _bPushIfListExists = true </para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool PushToListTail(
            string _MemoryScopeKey,
            string _ListName,
            PrimitiveType[] _Values,
            bool _bPushIfListExists = false,
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>PushToListHead</para>
        /// 
        /// <para>Pushes the value(s) to the head of given list, returns if push succeeds (If _bAsync is true, always returns true).</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_Values"/>                    Value(s) to push</para>
        /// <para><paramref name="_bPushIfListExists"/>         If true, only pushes if list exists</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bAsync"/>                    Asynchronous execution, will be ignored if _bPushIfListExists = true </para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool PushToListHead(
            string _MemoryScopeKey,
            string _ListName,
            PrimitiveType[] _Values,
            bool _bPushIfListExists = false,
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>PopLastElementOfList</para>
        /// 
        /// <para>Pops the value from the tail of given list</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Returned element</returns>
        /// 
        /// </summary>
        PrimitiveType PopLastElementOfList(
            string _MemoryScopeKey,
            string _ListName,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>PopFirstElementOfList</para>
        /// 
        /// <para>Pops the value from the head of given list</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Returned element</returns>
        /// 
        /// </summary>
        PrimitiveType PopFirstElementOfList(
            string _MemoryScopeKey,
            string _ListName,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>GetAllElementsOfList</para>
        /// 
        /// <para>Gets all values from the given list</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Returned elements</returns>
        /// 
        /// </summary>
        PrimitiveType[] GetAllElementsOfList(
            string _MemoryScopeKey,
            string _ListName,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>EmptyList</para>
        /// 
        /// <para>Empties the list</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_bWaitUntilCompletion"/>      Whether wait until query is done or fire and forget</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// <returns> Returns:                                  Returns if list is found and emptied or not</returns>
        /// 
        /// </summary>
        bool EmptyList(
            string _MemoryScopeKey,
            string _ListName,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>EmptyListAndSublists</para>
        /// 
        /// <para>Fetches all elements in _ListName, iterates and empties all sublists (_SublistPrefix + Returned SublistName)</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_SublistPrefix"/>             Prepends this to found sublist name</para>
        /// <para><paramref name="_bWaitUntilCompletion"/>      Whether wait until query is done or fire and forget</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// <para><paramref name="_bPublishChange"/>            Publish the change to Pub/Sub channel</para>
        /// 
        /// </summary>
        void EmptyListAndSublists(
            string _MemoryScopeKey,
            string _ListName, 
            string _SublistPrefix,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true);

        /// <summary>
        /// 
        /// <para>ListSize</para>
        /// 
        /// <para>Returns number of elements of the given list</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Size of the list</returns>
        /// 
        /// </summary>
        long ListSize(
            string _MemoryScopeKey,
            string _ListName,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>ListContains</para>
        /// 
        /// <para>Returns if given list contains given value or not</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_MemoryScopeKey"/>            Memory scope key need to be provided for performing this operation</para>
        /// <para><paramref name="_ListName"/>                  Name of list</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  True if list contains the value</returns>
        /// 
        /// </summary>
        bool ListContains(
            string _MemoryScopeKey,
            string _ListName,
            PrimitiveType _Value,
            Action<string> _ErrorMessageAction = null);

        IPubSubServiceInterface GetPubSubService();
    }
}