/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CommonUtilitiesWindows
{
    public static class PerformanceInfo
    {
        public class Info
        {
            public string AvailablePhysicalMemory;
            public string TotalMemory;
            public string FreeMemoryPercent;
            public string OccupiedMemoryPercent;

            public Info(Info _Other)
            {
                AvailablePhysicalMemory = _Other.AvailablePhysicalMemory;
                TotalMemory = _Other.TotalMemory;
                FreeMemoryPercent = _Other.FreeMemoryPercent;
                OccupiedMemoryPercent = _Other.OccupiedMemoryPercent;
            }
            public Info() {}

            public override string ToString()
            {
                return $"Available Memory: {AvailablePhysicalMemory}/{TotalMemory} - Free: %{FreeMemoryPercent} - Occupied: %{OccupiedMemoryPercent}";
            }
        }

        public static readonly string ERROR_LOG_STARTS_WITH = "[[PERFORMANCE_COUNTER]]";

        public static Info GetInfo(Action<string> _ErrorMessageAction = null)
        {
            if (!bSetup || LastErrorMessageAction_Internal != _ErrorMessageAction)
            {
                lock (Work_Lock)
                {
                    LastErrorMessageAction_Internal = _ErrorMessageAction;
                    if (!bSetup) //If still not set up
                    {
                        bSetup = true;

                        var InfoOriginal = Work_Prelocked();
                        lock (CopyInfoResult_Lock)
                        {
                            LastInfo_Copy = new Info(InfoOriginal);
                        }

                        try
                        {
                            WorkerThread = new Thread(WorkerThreadRunnable);
                            WorkerThread.Start();
                        }
                        catch (Exception e)
                        {
                            Log($"{ERROR_LOG_STARTS_WITH} PerformanceInfo: {e.Message} {e.StackTrace}");
                        }
                    }
                }
            }
            lock (CopyInfoResult_Lock)
            {
                return LastInfo_Copy;
            }
        }

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        private static Info Work_Prelocked()
        {
            long AvailablePhysicalMemory = 0;
            long TotalMemory = 0;
            int PercentFree = 0;
            int PercentOccupied = 0;

            Lock_Work_Mutex();

            try
            {
                AvailablePhysicalMemory = GetPhysicalAvailableMemoryInMiB();
                TotalMemory = GetTotalMemoryInMiB();
                PercentFree = Convert.ToInt32(((decimal)AvailablePhysicalMemory / (decimal)TotalMemory) * 100);
                PercentOccupied = 100 - PercentFree;
            }
            catch (Exception e)
            {
                Log($"{ERROR_LOG_STARTS_WITH} [Error] PerformanceInfo: {e.Message} {e.StackTrace}");
            }

            Unlock_Work_Mutex();

            return new Info()
            {
                AvailablePhysicalMemory = ConvertDataSizeString(AvailablePhysicalMemory).ToString(),
                TotalMemory = ConvertDataSizeString(TotalMemory).ToString(),
                FreeMemoryPercent = PercentFree.ToString(),
                OccupiedMemoryPercent = PercentOccupied.ToString()
            };
        }

        private static long GetPhysicalAvailableMemoryInMiB()
        {
            try
            {
                var PI = new PerformanceInformation();
                if (GetPerformanceInfo(out PI, Marshal.SizeOf(PI)))
                {
                    return Convert.ToInt64((PI.PhysicalAvailable.ToInt64() * PI.PageSize.ToInt64()));
                }
            }
            catch (Exception e)
            {
                Log($"{ERROR_LOG_STARTS_WITH} [Error] GetPhysicalAvailableMemoryInMiB: {e.Message} {e.StackTrace}");
            }
            return -1;
        }

        private static long GetTotalMemoryInMiB()
        {
            try
            {
                var PI = new PerformanceInformation();
                if (GetPerformanceInfo(out PI, Marshal.SizeOf(PI)))
                {
                    return Convert.ToInt64((PI.PhysicalTotal.ToInt64() * PI.PageSize.ToInt64()));
                }
            }
            catch (Exception e)
            {
                Log($"{ERROR_LOG_STARTS_WITH} [Error] GetTotalMemoryInMiB: {e.Message} {e.StackTrace}");
            }
            return -1;
        }

        public static string ConvertDataSizeString(long _ByteCount)
        {
            string[] Suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (_ByteCount == 0) return $"0 {Suf[0]}";
            long Bytes = Math.Abs(_ByteCount);
            int Place = Convert.ToInt32(Math.Floor(Math.Log(Bytes, 1024)));
            double Num = Math.Round(Bytes / Math.Pow(1024, Place), 3);
            return $"{(Math.Sign(_ByteCount) * Num)} {Suf[Place]}";
        }

        private static void Log(string _Log)
        {
            try
            {
                //Do not prepend anything to _Log here.
                LastErrorMessageAction_Internal?.Invoke(_Log);
            }
            catch (Exception) { }
        }
        private static Action<string> LastErrorMessageAction_Internal;

        private static void WorkerThreadRunnable()
        {
            try
            {
                Thread.CurrentThread.IsBackground = true;

                while (true)
                {
                    Thread.Sleep(1000);

                    Info InfoOriginal;
                    lock (Work_Lock)
                    {
                        InfoOriginal = Work_Prelocked();
                    }
                    lock (CopyInfoResult_Lock)
                    {
                        LastInfo_Copy = new Info(InfoOriginal);
                    }
                }
            }
            catch (Exception e) 
            {
                if (e is ThreadAbortException) return;
                Log($"{ERROR_LOG_STARTS_WITH} [Error] WorkerThreadRunnable: {e.Message} {e.StackTrace}");
            }
        }

        private static bool bSetup = false;
        private static readonly object Work_Lock = new object();
        private static readonly object CopyInfoResult_Lock = new object();
        private static Thread WorkerThread;
        private static Info LastInfo_Copy;

        private static Mutex Work_Mutex = null;
        private static void Lock_Work_Mutex()
        {
            try
            {
                if (Work_Mutex == null)
                {
                    Work_Mutex = new Mutex(false, "PerformanceInfo");
                }
                Work_Mutex.WaitOne();
            }
            catch (Exception) { }
        }
        private static void Unlock_Work_Mutex()
        {
            try
            {
                if (Work_Mutex != null)
                {
                    Work_Mutex.ReleaseMutex();
                }
            }
            catch (Exception) { }
        }
    }
}