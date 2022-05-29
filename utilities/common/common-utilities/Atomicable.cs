/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities
{

    /// <summary>
    /// Multiple producers: thread safety will be ensured when setting the value
    /// </summary>
    public enum EProducerStatus
    {
        SingleProducer,
        MultipleProducer
    };

    /// <summary>
    /// For passing a primitive value as reference in Action parameters
    /// </summary>
    public sealed class Atomicable<T>
    {
        private T ValueInternal;
        private readonly EProducerStatus ThreadSafety;

        public readonly Object Monitor = new Object();

        public Atomicable(T _InitialValue, EProducerStatus _ThreadSafety = EProducerStatus.SingleProducer)
        {
            ValueInternal = _InitialValue;
            ThreadSafety = _ThreadSafety;
        }

        public T Get()
        {
            return ValueInternal;
        }

        public void Set(T NewValue)
        {
            if (ThreadSafety == EProducerStatus.MultipleProducer)
            {
                lock (Monitor)
                {
                    ValueInternal = NewValue;
                }
            }
            else
            {
                ValueInternal = NewValue;
            }
        }
    }

    /// <summary>
    /// <para>Only allows getting a primitive value, not setting</para>
    /// </summary>
    public sealed class AtomicableOnlyGetAllowed<T>
    {
        private readonly Atomicable<T> RelativeValue = null;
        public T Get()
        {
            return RelativeValue.Get();
        }

        public AtomicableOnlyGetAllowed(Atomicable<T> _RelativeBoolean)
        {
            RelativeValue = _RelativeBoolean;
        }
    }
}
