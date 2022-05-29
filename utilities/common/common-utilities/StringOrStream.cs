/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.IO;

namespace CommonUtilities
{
    public enum EStringOrStreamEnum
    {
        String,
        Stream
    };
    public sealed class StringOrStream
    {
        private readonly Action DestructorAction;

        public Stream Stream
        {
            get
            {
                if (Type == EStringOrStreamEnum.String)
                {
                    var Streamified = new MemoryStream();
                    using (var Writer = new StreamWriter(Streamified))
                    {
                        Writer.Write(StringValue);
                    }
                    return Streamified;
                }
                return StreamValue;
            }
        }
        public long StreamLength
        {
            get
            {
                if (Type == EStringOrStreamEnum.String)
                {
                    return StringValue.Length;
                }
                return StreamLengthValue;
            }
        }

        private readonly string StringValue = "";
        private readonly Stream StreamValue = null;
        private readonly long StreamLengthValue = 0;
        public string String
        {
            get
            {
                if (Type == EStringOrStreamEnum.Stream)
                {
                    string Stringified;
                    using (var Reader = new StreamReader(Stream))
                    {
                        Stringified = Reader.ReadToEnd();
                    }
                    return Stringified;
                }
                return StringValue;
            }
        }

        public EStringOrStreamEnum Type { get; }

        public StringOrStream(Stream _Stream, long _StreamLength)
        {
            Type = EStringOrStreamEnum.Stream;
            StreamValue = _Stream;
            StreamLengthValue = _StreamLength;
            StringValue = "";
        }
        public StringOrStream(string _Str)
        {
            Type = EStringOrStreamEnum.String;
            StringValue = _Str;
        }

        public StringOrStream(Stream _Stream, long _StreamLength, Action _DestructorAction)
        {
            Type = EStringOrStreamEnum.Stream;
            StreamValue = _Stream;
            StreamLengthValue = _StreamLength;
            DestructorAction = _DestructorAction;
            StringValue = "";
        }

        private StringOrStream() { }

        ~StringOrStream()
        {
            if (Type == EStringOrStreamEnum.Stream)
            {
                DestructorAction?.Invoke();
            }
        }

        public override string ToString()
        {
            if (Type == EStringOrStreamEnum.Stream)
            {
                using (var Reader = new StreamReader(StreamValue))
                {
                    return Reader.ReadToEnd();
                }
            }
            return String;
        }
    }
}