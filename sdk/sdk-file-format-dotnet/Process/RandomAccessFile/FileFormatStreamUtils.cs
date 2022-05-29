/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.IO;

namespace SDKFileFormat.Process.RandomAccessFile
{
    public enum EDeflateCompression
    {
        Compress,
        DoNotCompress
    }
    public struct StreamStruct
    {
        public Stream IOStream;
        public EDeflateCompression IOCompression;

        public StreamStruct(Stream _IOStream, EDeflateCompression _IOCompression) {
            IOStream = _IOStream;
            IOCompression = _IOCompression;
        }
        public static implicit operator StreamStruct(Tuple<Stream, EDeflateCompression> _Input)
        {
            return new StreamStruct(_Input.Item1, _Input.Item2);
        }

        public override bool Equals(object _Other)
        {
            return _Other is StreamStruct Casted &&
                    IOStream == Casted.IOStream &&
                    IOCompression == Casted.IOCompression;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(IOStream, IOCompression);
        }
        public override string ToString()
        {
            return $"{IOStream.ToString()},{IOCompression.ToString()}";
        }
        public static bool operator ==(StreamStruct x, StreamStruct y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(StreamStruct x, StreamStruct y)
        {
            return !x.Equals(y);
        }
    }
}