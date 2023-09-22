/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;

namespace CommonUtilities
{
    public sealed class ThreadWrapper
    {
        /// <summary>
        /// Implemented for an easier thread creation. Fire-forget with a simple cancel token.
        /// </summary>
        private ThreadWrapper()
        {
            DisposerThread = new Thread(DisposerThreadRunnable);
            DisposerThread.Start();
        }
        ~ThreadWrapper()
        {
            bRunning = false;
        }
        private static ThreadWrapper Instance = null;
        private static ThreadWrapper Get()
        {
            if (Instance == null)
            {
                Instance = new ThreadWrapper();
            }
            return Instance;
        }

        private readonly List<Tuple<Thread, Atomicable<bool>>> CreatedThreads = new List<Tuple<Thread, Atomicable<bool>>>();
        private readonly object CreatedThreads_Lock = new object();
        private bool bRunning = true;

        private readonly Thread DisposerThread = null;
        private void DisposerThreadRunnable()
        {
            Thread.CurrentThread.IsBackground = true;

            while (bRunning)
            {
                Thread.Sleep(1000);

                lock (Get().CreatedThreads_Lock)
                {
                    for (var i = Get().CreatedThreads.Count - 1; i >= 0; i--)
                    {
                        var CurrentTask = Get().CreatedThreads[i];

                        if (CurrentTask != null)
                        {
                            bool bCancelRequested = CurrentTask.Item2 != null && CurrentTask.Item2.Get();

                            if (bCancelRequested)
                            {
                                try
                                {
                                    CurrentTask.Item1.Abort();
                                }
                                catch (Exception) { }
                            }

                            if (bCancelRequested
                                || !CurrentTask.Item1.IsAlive
                                || CurrentTask.Item1.ThreadState == ThreadState.Aborted
                                || CurrentTask.Item1.ThreadState == ThreadState.AbortRequested
                                || CurrentTask.Item1.ThreadState == ThreadState.Stopped
                                || CurrentTask.Item1.ThreadState == ThreadState.StopRequested
                                || CurrentTask.Item1.ThreadState == ThreadState.Suspended
                                || CurrentTask.Item1.ThreadState == ThreadState.SuspendRequested)
                            {
                                CreatedThreads.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        public static void Run(Action _Action, Atomicable<bool> _bCancel = null)
        {
            if (_Action != null)
            {
                lock (Get().CreatedThreads_Lock)
                {
                    var NewThread = new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        _Action?.Invoke();
                    });
                    NewThread.Start();

                    Get().CreatedThreads.Add(new Tuple<Thread, Atomicable<bool>>(NewThread, _bCancel));
                }
            }
        }
    }
}
