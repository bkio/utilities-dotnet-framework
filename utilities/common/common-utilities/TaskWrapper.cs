/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities
{
    public sealed class TaskWrapper
    {
        private TaskWrapper()
        {
        }
        public static void Run(Action _Action)
        {
            ThreadWrapper.Run(_Action); //For now...
        }
    }
}