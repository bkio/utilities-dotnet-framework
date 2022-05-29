/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace SDKFileFormat.Process.RandomAccessFile
{
    public static class FileFormatHeader
    {
        public const int HeaderSize = sizeof(uint);

        public static int WriteHeader(uint _FileSDKVersion, byte[] _File)
        {
            int Size = HeaderSize;
            Buffer.BlockCopy(BitConverter.GetBytes(_FileSDKVersion), 0, _File, 0, Size);
            return Size;
        }
        public static int ReadHeader(out uint _FileSDKVersion, byte[] _File)
        {
            _FileSDKVersion = BitConverter.ToUInt32(_File, 0);
            return HeaderSize;
        }
    }
}