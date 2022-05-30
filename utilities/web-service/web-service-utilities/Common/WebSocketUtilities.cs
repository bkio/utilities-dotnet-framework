/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace WebServiceUtilities.Common
{
    public class WebSocketUtilities
    {
        // Snipped the rest of the connection class.
        byte[] GetBuffer()
        {
            if (this.internalBufferIfArrayPoolNotUsed == null)
                this.internalBufferIfArrayPoolNotUsed = new byte[ReceiveChunkSize];

            return this.internalBufferIfArrayPoolNotUsed;
        }

        async Task Loop(IEnumerable<string> tickers, Func<ReadOnlySequence<byte>, Task> dispatch, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                var webSocket = new ClientWebSocket();
                webSocket.Options.KeepAliveInterval = this.keepAliveInterval;
                var resultProcessor = new WebSocketReceiveResultProcessor(this.isUsingArrayPool);

                try
                {
                    this.logger.LogInformation($"Connecting.");
                    await webSocket.ConnectAsync(uri, cancellationToken);

                    while (webSocket.State == WebSocketState.Open && cancellationToken.IsCancellationRequested == false)
                    {
                        var buffer = GetBuffer();
                        var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                        var isEndOfMessage = resultProcessor.Receive(result, buffer, out var frame);

                        if (isEndOfMessage)
                        {
                            if (frame.IsEmpty == true)
                                break; // End of message with no data means socket closed - break so we can reconnect.
                            else
                                await dispatch(frame);
                        }
                    }
                }
                catch (WebSocketException ex)
                {
                    this.logger.LogError(ex, ex.Message);
                }
                catch (Exception ex)
                {
                    this.logger.LogCritical(ex, ex.Message);
                    return;
                }
                finally
                {
                    webSocket.Dispose();
                    resultProcessor.Dispose();
                }
            }
        }

        class Chunk<T> : ReadOnlySequenceSegment<T>
        {
            public Chunk(ReadOnlyMemory<T> memory)
            {
                Memory = memory;
            }
            public Chunk<T> Add(ReadOnlyMemory<T> mem)
            {
                var segment = new Chunk<T>(mem)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };

                Next = segment;
                return segment;
            }
        }

        sealed class WebSocketReceiveResultProcessor : IDisposable
        {
            Chunk<byte> startChunk = null;
            Chunk<byte> currentChunk = null;

            public WebSocketReceiveResultProcessor()
            {
            }

            public bool Receive(WebSocketReceiveResult result, ArraySegment<byte> buffer, out ReadOnlySequence<byte> frame)
            {
                if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Close)
                {
                    frame = default;
                    return false;
                }

                // If not using array pool, take a local copy to avoid corruption as buffer is reused by caller.
                var slice = buffer.Slice(0, result.Count).ToArray();

                if (startChunk == null)
                    startChunk = currentChunk = new Chunk<byte>(slice);
                else
                    currentChunk = currentChunk.Add(slice);

                if (result.EndOfMessage && startChunk != null)
                {

                    if (startChunk.Next == null)
                        frame = new ReadOnlySequence<byte>(startChunk.Memory);
                    else
                        frame = new ReadOnlySequence<byte>(startChunk, 0, currentChunk, currentChunk.Memory.Length);

                    startChunk = currentChunk = null; // Reset so we can accept new chunks from scratch.
                    return true;
                }
                else
                {
                    frame = default;
                    return false;
                }
            }

            public void Dispose()
            {
                // Suppress finalization.
                GC.SuppressFinalize(this);
            }
        }
    }
}