/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using SDKFileFormat.Process.Procedure;

namespace SDKFileFormat.Process.RandomAccessFile
{
    public class FileFormatStreamReader : IDisposable
    {
        private readonly ENodeType FileType;
        private readonly Action<uint> OnFileSDKVersionRead;
        private readonly Action<Node> OnNodeRead_TS;
        
        private readonly EDeflateCompression Compression;
        private readonly GZipStream DecompressionStream;
        private readonly Stream InnerStream;

        public const int MaximumChunkSize = 1000000;

        public FileFormatStreamReader(ENodeType _FileType, Stream _InnerStream, Action<uint> _OnFileSDKVersionRead, Action<Node> _OnNodeRead_TS, EDeflateCompression _Compression)
        {
            FileType = _FileType;
            OnFileSDKVersionRead = _OnFileSDKVersionRead;
            OnNodeRead_TS = _OnNodeRead_TS;
            InnerStream = _InnerStream;

            Compression = _Compression;
            if(Compression == EDeflateCompression.Compress)
            {
                DecompressionStream = new GZipStream(InnerStream, CompressionMode.Decompress);
            }

            ProcessThread = new Thread(Process_Runnable);
            ProcessThread.Start();
        }

        public bool Process(Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var ReadChunk = new byte[MaximumChunkSize];

                while (true)
                {
                    int ReadCount;

                    if (Compression == EDeflateCompression.Compress)
                    {
                        ReadCount = DecompressionStream.Read(ReadChunk, 0, MaximumChunkSize);
                    }
                    else
                    {
                        ReadCount = InnerStream.Read(ReadChunk, 0, MaximumChunkSize);
                    }

                    if (ReadCount <= 0)
                        break;

                    Write(ReadChunk, 0, ReadCount);
                }
                bInnerStreamReadCompleted = true;
                ThreadOperationCompletedEvent.WaitOne();
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"XStreamReader: {e.Message}, trace:{e.StackTrace}");
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            bInnerStreamReadCompleted = true;
            try
            {
                ThreadOperationCompletedEvent.WaitOne();
                ThreadOperationCompletedEvent.Close();
            }
            catch (Exception) { }

            try
            {
                if (DecompressionStream != null)
                {
                    DecompressionStream.Dispose();
                }
            }
            catch (Exception) { }

            Flush();
            InnerStream.Close();
        }

        private void Flush()
        {
            WaitingDataBlockQueue_Header_TotalSize = 0;
            WaitingDataBlockQueue_Header.Clear();

            UnprocessedDataSize = 0;
            UnprocessedDataQueue.Clear();
        }

        private void Write(byte[] _Buffer, int _Offset, int _Count)
        {
            int Index = _Offset;
            int RemainedBytes = _Count;
            while (RemainedBytes > 0)
            {
                var BytesToProcess = Math.Min(RemainedBytes, MaximumChunkSize);

                Process(_Buffer, Index, BytesToProcess);
                
                RemainedBytes -= BytesToProcess;
                Index += BytesToProcess;
            }
        }
        
        private bool bHeaderRead = false;

        private long WaitingDataBlockQueue_Header_TotalSize = 0;
        private readonly Queue<byte[]> WaitingDataBlockQueue_Header = new Queue<byte[]>();

        private Thread ProcessThread;

        private readonly ConcurrentQueue<byte[]> UnprocessedDataQueue = new ConcurrentQueue<byte[]>();
        public long UnprocessedDataSize  = 0;
        private long UnprocessedNodes = 0;

        private bool bInnerStreamReadCompleted = false;
        private readonly ManualResetEvent ThreadOperationCompletedEvent = new ManualResetEvent(false);

        private void Process(byte[] _Buffer, int _Offset, int _Count)
        {
            if (!bHeaderRead)
            {
                if ((WaitingDataBlockQueue_Header_TotalSize + _Count) >= FileFormatHeader.HeaderSize)
                {
                    long ArraySize = WaitingDataBlockQueue_Header_TotalSize > 0 ? (WaitingDataBlockQueue_Header_TotalSize + _Count) : _Count;
                    if(ArraySize > 2147483591)
                    {
                        ArraySize = 2147483591;
                    }

                    var CurrentBlock = new byte[ArraySize];


                    long CurrentIx = 0;
                    while (WaitingDataBlockQueue_Header.TryPeek(out byte[] WaitingBlock) && CurrentIx + WaitingBlock.Length < ArraySize)
                    {
                        WaitingDataBlockQueue_Header.TryDequeue(out byte[] _);

                        for (int i = 0; i < WaitingBlock.Length; i++)
                        {
                            CurrentBlock[CurrentIx++] = WaitingBlock[i];
                        }
                        WaitingDataBlockQueue_Header_TotalSize -= WaitingBlock.Length;
                    }
                    for (int i = _Offset; i < _Count; i++)
                    {
                        CurrentBlock[CurrentIx++] = _Buffer[i];
                    }

                    FileFormatHeader.ReadHeader(out uint FileSDKVersion, CurrentBlock);
                    OnFileSDKVersionRead(FileSDKVersion);

                    if (CurrentBlock.Length > FileFormatHeader.HeaderSize)
                    {
                        int Count = CurrentBlock.Length - FileFormatHeader.HeaderSize;
                        var Rest = new byte[Count];
                        Buffer.BlockCopy(CurrentBlock, FileFormatHeader.HeaderSize, Rest, 0, Count);

                        UnprocessedDataQueue.Enqueue(Rest);
                        Interlocked.Add(ref UnprocessedDataSize, Rest.Length);
                    }

                    bHeaderRead = true;
                }
                else
                {
                    var CurrentBuffer = new byte[_Count];
                    Buffer.BlockCopy(_Buffer, _Offset, CurrentBuffer, 0, _Count);
                    WaitingDataBlockQueue_Header.Enqueue(CurrentBuffer);
                    WaitingDataBlockQueue_Header_TotalSize += _Count;
                }
            }
            else
            {
                var CurrentBuffer = new byte[_Count];
                Buffer.BlockCopy(_Buffer, _Offset, CurrentBuffer, 0, _Count);

                UnprocessedDataQueue.Enqueue(CurrentBuffer);
                Interlocked.Add(ref UnprocessedDataSize, _Count);
            }
        }

        private void Process_Runnable()
        {
            Thread.CurrentThread.IsBackground = true;
            
            try
            {
                Process_Internal();
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    ProcessThread = new Thread(Process_Runnable)
                    {
                        Priority = ThreadPriority.Highest
                    };
                    ProcessThread.Start();
                }
            }
        }

        private void Process_Internal()
        {
            byte[] FailedLeftOverBlock = null;

            while (true)
            {
                long ProcessedBufferCount = 0;

                var UnprocessedDataSize_Current = UnprocessedDataSize;
                if (UnprocessedDataSize_Current > 0)
                {
                    long ArraySize = (FailedLeftOverBlock != null ? FailedLeftOverBlock.Length : 0) + UnprocessedDataSize_Current;

                    if (ArraySize > 2147483591)
                    {
                        if(FailedLeftOverBlock == null)
                        {
                            ArraySize = 2147483591 - (2147483591 % MaximumChunkSize);
                        }
                        else
                        {

                            ArraySize = 2147483591 - ((2147483591 - FailedLeftOverBlock.Length) % MaximumChunkSize);
                        }
                    }

                    var CurrentBuffer = new byte[ArraySize];

                    if (FailedLeftOverBlock != null)
                    {
                        Buffer.BlockCopy(FailedLeftOverBlock, 0, CurrentBuffer, 0, FailedLeftOverBlock.Length);
                    }
                    int CurrentIndex = FailedLeftOverBlock != null ? FailedLeftOverBlock.Length : 0;
                    FailedLeftOverBlock = null;

                    while (UnprocessedDataQueue.TryPeek(out byte[] CurrentBlock) && CurrentIndex < CurrentBuffer.Length && CurrentIndex + CurrentBlock.Length <= CurrentBuffer.Length)
                    {
                        Buffer.BlockCopy(CurrentBlock, 0, CurrentBuffer, CurrentIndex, CurrentBlock.Length);
                        CurrentIndex += CurrentBlock.Length;
                        Interlocked.Add(ref UnprocessedDataSize, -1 * CurrentBlock.Length);
                        ProcessedBufferCount++;

                        UnprocessedDataQueue.TryDequeue(out byte[] _);
                    }

                    var SuccessOffset = ReadUntilFailure(CurrentBuffer);
                    if (SuccessOffset == -1) continue;

                    FailedLeftOverBlock = new byte[CurrentBuffer.Length - SuccessOffset];
                    Buffer.BlockCopy(CurrentBuffer, SuccessOffset, FailedLeftOverBlock, 0, FailedLeftOverBlock.Length);
                }
                if (bInnerStreamReadCompleted && UnprocessedDataQueue.Count == 0 && UnprocessedDataSize == 0 && UnprocessedNodes == 0)
                {
                    try
                    {
                        ThreadOperationCompletedEvent.Set();
                    }
                    catch (Exception) { }
                    return;
                }
                if (ProcessedBufferCount < 32)
                {
                    Thread.Sleep(50);
                }
            }
        }

        //Returns -1 on full success on reading
        private int ReadUntilFailure(byte[] _Input)
        {
            int SuccessOffset = 0;

            while (SuccessOffset < _Input.Length)
            {
                Node NewNode = null;
                try
                {
                    var Offset = NodeTools.BufferToNode(out NewNode, FileType, _Input, SuccessOffset);
                    SuccessOffset += Offset;
                }
                catch (Exception ex)
                {
                    if (ex is IndexOutOfRangeException || ex is ArgumentException)
                    {
                        return SuccessOffset;
                    }
                    throw;
                }

                Interlocked.Increment(ref UnprocessedNodes);
                Task.Run(() =>
                {
                    OnNodeRead_TS(NewNode);
                    Interlocked.Decrement(ref UnprocessedNodes);
                });
            }
            return -1;
        }
    }
}