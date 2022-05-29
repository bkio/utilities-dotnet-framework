/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace CommonUtilities.Geometry.Tools.Compiler
{
    public static class ItemCompilerTools
    {
        public static string Compress(string _S)
        {
            var Bytes = Encoding.Unicode.GetBytes(_S);

            using (var Msi = new MemoryStream(Bytes))
            {
                using (var Mso = new MemoryStream())
                {
                    using (var Gs = new GZipStream(Mso, CompressionMode.Compress))
                    {
                        Msi.CopyTo(Gs);
                    }
                    return Convert.ToBase64String(Mso.ToArray());
                }
            }
        }

        public static string Decompress(string _S)
        {
            var Bytes = Convert.FromBase64String(_S);

            using (var Msi = new MemoryStream(Bytes))
            {
                using (var Mso = new MemoryStream())
                {
                    using (var Gs = new GZipStream(Mso, CompressionMode.Decompress))
                    {
                        Gs.CopyTo(Mso);
                    }
                    return Encoding.UTF8.GetString(Mso.ToArray());
                }
            }
        }

        public static byte[] Compile(
            JObject _ItemData,
            List<byte[]> _CompiledGeometryParts,
            Action<string> _ErrorMessageAction)
        {
            byte[] Result;
            try
            {
                Result = Compile_Internal(_ItemData, _CompiledGeometryParts, _ErrorMessageAction);
            }
            catch (Exception e)
            {
                Result = null;
                _ErrorMessageAction?.Invoke($"Exception in ItemCompiler.Compile: {e.Message}, number of compiled geometry parts: {_CompiledGeometryParts.Count}");
            }
            return Result;
        }
        private static byte[] Compile_Internal(
            JObject _ItemData,
            List<byte[]> _CompiledGeometryParts,
            Action<string> _ErrorMessageAction)
        {
            /*
             * int ItemDataLength                                   1 int                           * 4                       = 4
             * string ItemData                                      ItemDataLength                                            = ItemDataLength
             * 
             * int NumberOfCompiledGeometryParts                    1 int                           * 4                       = 4
             * [i] CompiledGeometryPartLength                       1 int                           * 4                       = 4
             * [i] CompiledGeometryPart                             [i] CompiledGeometryPartLength                            = CompiledGeometryPartLength
             * ...
             * 
             *                                                                                                          Total = 8 + ItemDataLength + (4 + EachCompiledGeometryPartLength)...
             */

            //8 = ItemDataLength + NumberOfCompiledGeometryParts
            int Size = 8;

            byte[] ItemDataAsBytes = null;
            int ItemDataAsBytesLength = 0;

            try
            {
                ItemDataAsBytes = Encoding.UTF8.GetBytes(_ItemData.ToString());
                ItemDataAsBytesLength = ItemDataAsBytes.Length;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"Exception in ItemCompiler.Compile_Internal-1: {e.Message}");
                ItemDataAsBytes = null;
                ItemDataAsBytesLength = 0;
            }
            Size += ItemDataAsBytesLength;

            for (int j = 0; j < _CompiledGeometryParts.Count; j++)
            {
                Size += 4 + _CompiledGeometryParts[j].Length;
            }

            byte[] Result = null;
            try
            {
                Result = new byte[Size];
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"Exception in ItemCompiler.Compile_Internal-2: {e.Message}, size: {Size}, item data size: {ItemDataAsBytesLength}, number of compiled geometry parts: {_CompiledGeometryParts.Count}");
                return null;
            }

            //int ItemDataLength
            Array.Copy(BitConverter.GetBytes(ItemDataAsBytesLength), 0, Result, 0, 4);

            //string ItemData
            Array.Copy(ItemDataAsBytes, 0, Result, 4, ItemDataAsBytesLength);

            //int NumberOfCompiledGeometryParts
            Array.Copy(BitConverter.GetBytes(_CompiledGeometryParts.Count), 0, Result, 4 + ItemDataAsBytesLength, 4);

            int CurrentIndex = 8 + ItemDataAsBytesLength;

            //CompiledGeometryPartition
            int i = 0;
            while (i < _CompiledGeometryParts.Count)
            {
                try
                {
                    //int CompiledFragmentLength
                    Array.Copy(BitConverter.GetBytes(_CompiledGeometryParts[i].Length), 0, Result, CurrentIndex, 4);

                    //byte[] CompiledGeometryPart
                    Array.Copy(_CompiledGeometryParts[i], 0, Result, CurrentIndex + 4, _CompiledGeometryParts[i].Length);

                    CurrentIndex += 4 + _CompiledGeometryParts[i].Length;
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"Exception in ItemCompiler.Compile_Internal-3: {e.Message}, desired size: {Size}, result array size: {Result.Length}, current byte index: {CurrentIndex}, geometry parts count: {_CompiledGeometryParts.Count}, current geometry part index: {i}, current geometry part byte length: {_CompiledGeometryParts[i].Length}");
                    return null;
                }
                i++;
            }
            return Result;
        }

        //ItemData will only be filled if FragmentNo is 1 (first fragment) and metadata available for the item
        public static Tuple<List<byte[]>, JObject> Decompile(byte[] _CompiledData)
        {
            if (_CompiledData != null && _CompiledData.Length >= 8) /*NumberOfCompiledGeometryParts: 1 int = 4 bytes and ItemDataLength: 1 int = 4 bytes*/
            {
                var GeometryParts = new List<byte[]>();
                JObject ItemData = null;

                //ItemDataLength
                int ItemDataLength = BitConverter.ToInt32(_CompiledData, 0);

                //string ItemData
                if (ItemDataLength > 0 && (ItemDataLength + 8) <= _CompiledData.Length)
                {
                    try
                    {
                        string ItemDataJsonString = Encoding.UTF8.GetString(_CompiledData, 4, ItemDataLength);
                        //var CompressedItemData = Decompress(ItemDataJsonString);
                        ItemData = JObject.Parse(ItemDataJsonString);
                    }
                    catch (Exception) { }
                }

                //int NumberOfCompiledGeometryParts
                int NumberOfCompiledGeometryParts = BitConverter.ToInt32(_CompiledData, 4 + ItemDataLength);
                if (NumberOfCompiledGeometryParts > 0)
                {
                    int CurrentIndex = 8 + ItemDataLength;
                    for (int i = 0; i < NumberOfCompiledGeometryParts; i++)
                    {
                        if ((CurrentIndex + 4) <= _CompiledData.Length)
                        {
                            //int CompiledFragmentLength
                            int CompiledGeometryPartLength = BitConverter.ToInt32(_CompiledData, CurrentIndex);
                            CurrentIndex += 4;

                            if (CompiledGeometryPartLength > 0)
                            {
                                if ((CurrentIndex + CompiledGeometryPartLength) <= _CompiledData.Length)
                                {
                                    //byte[] CompiledFragment
                                    byte[] CurrentFragment = new byte[CompiledGeometryPartLength];
                                    Array.Copy(_CompiledData, CurrentIndex, CurrentFragment, 0, CompiledGeometryPartLength);
                                    GeometryParts.Add(CurrentFragment);
                                    CurrentIndex += CompiledGeometryPartLength;
                                }
                                else break;
                            }
                        }
                        else break;
                    }
                }
                return new Tuple<List<byte[]>, JObject>(GeometryParts, ItemData);
            }
            return null;
        }
    }
}