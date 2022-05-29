/// Copyright 2022- Burak Kara, All rights reserved.

using SDKFileFormat.Tests;

namespace SDKFileFormat
{
    class Program
    {
        static void Main(string[] args)
        {
            TestFileFormat.SimpleReadWriteTest(true);
            TestFileFormat.SimpleReadWriteTest(false);
        }
    }
}