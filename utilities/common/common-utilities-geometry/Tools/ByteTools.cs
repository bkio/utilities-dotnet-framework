/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities.Geometry.Utilities
{
    public static class ByteTools
    {
        public static void BytesToValue(out byte _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = _Bytes[_Head];
            _Head += sizeof(byte);
        }
        public static void BytesToValue(out float _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToSingle(_Bytes, _Head);
            _Head += sizeof(float);
        }
        public static void BytesToValue(out double _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToDouble(_Bytes, _Head);
            _Head += sizeof(double);
        }
        public static void BytesToValue(out short _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToInt16(_Bytes, _Head);
            _Head += sizeof(short);
        }
        public static void BytesToValue(out ushort _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToUInt16(_Bytes, _Head);
            _Head += sizeof(ushort);
        }
        public static void BytesToValue(out int _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToInt32(_Bytes, _Head);
            _Head += sizeof(int);
        }
        public static void BytesToValue(out uint _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToUInt32(_Bytes, _Head);
            _Head += sizeof(uint);
        }
        public static void BytesToValue(out long _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToInt64(_Bytes, _Head);
            _Head += sizeof(long);
        }
        public static void BytesToValue(out ulong _Result, byte[] _Bytes, ref int _Head)
        {
            _Result = BitConverter.ToUInt64(_Bytes, _Head);
            _Head += sizeof(ulong);
        }

        public static void ValueToBytes(byte _Value, byte[] _WriteToBytes, ref int _Head)
        {
            _WriteToBytes[_Head] = _Value;
            _Head += sizeof(byte);
        }
        public static void ValueToBytes(byte[] _Value, byte[] _WriteToBytes, ref int Head)
        {
            Buffer.BlockCopy(_Value, 0, _WriteToBytes, Head, _Value.Length);
            Head += _Value.Length;
        }
    }
}