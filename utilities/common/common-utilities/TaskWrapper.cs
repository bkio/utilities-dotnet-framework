/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;

namespace CommonUtilities
{
    /// <summary>
    /// <para>TaskWrapper is implemented due to Dispose issue for created task.</para>
    /// <para>MSDN Documentation: "Always call Dispose before you release your last reference to the Task."</para>
    /// </summary>
    public sealed class TaskWrapper
    {
        private TaskWrapper()
        {
            DisposerThread = new Thread(DisposerThreadRunnable);
            DisposerThread.Start();
        }
        ~TaskWrapper()
        {
            bRunning = false;
        }
        private static TaskWrapper Instance = null;
        private static TaskWrapper Get()
        {
            if (Instance == null)
            {
                Instance = new TaskWrapper();
            }
            return Instance;
        }

        private readonly List<Tuple<Thread, Atomicable<bool>>> CreatedThreads = new List<Tuple<Thread, Atomicable<bool>>>();
        private readonly object CreatedTasks_Lock = new object();
        private bool bRunning = true;

        private readonly Thread DisposerThread = null;
        private void DisposerThreadRunnable()
        {
            Thread.CurrentThread.IsBackground = true;

            while (bRunning)
            {
                Thread.Sleep(1000);

                lock (Get().CreatedTasks_Lock)
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
                lock (Get().CreatedTasks_Lock)
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