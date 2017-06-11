using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Carable.Lazy
{
    /// <summary>
    /// Lazy expiry 
    /// </summary>
    public class LazyExpiry<T>
    {
        class ValueAndExpires
        {
            public ValueAndExpires(Tuple<T, DateTime> valueAndExpires) : this(valueAndExpires.Item1, valueAndExpires.Item2)
            {
            }

            public ValueAndExpires(T value, DateTime expires)
            {
                this.Value = value;
                this.Expires = expires;
            }

            public readonly T Value;
            public readonly DateTime Expires;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueGenerator">A delegate which generates a new value when old value has expired</param>
        /// <param name="mode"></param>
        /// <param name="now"></param>
        public LazyExpiry(Func<Tuple<T, DateTime>> valueGenerator, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly, Func<DateTime> now = null)
        {
            this.mode = mode;
            this.valueGenerator = valueGenerator ?? throw new ArgumentNullException(nameof(valueGenerator));
            this.now = now != null ? now : DefaultNow;
        }

        /// <summary>
        /// 
        /// </summary>
        public LazyExpiry(Func<Tuple<T, DateTime>> valueGenerator, bool threadSafe)
            : this(valueGenerator, LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }

        private readonly LazyThreadSafetyMode mode;
        private readonly Func<Tuple<T, DateTime>> valueGenerator;

        private readonly object lockObj = new object();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DateTime DefaultNow()
        {
            return DateTime.UtcNow;
        }
        ValueAndExpires valueReference;
        private readonly Func<DateTime> now;

        /// <summary>
        /// Get current value
        /// </summary>
        public T Value
        {
            get
            {
                {
                    var current = valueReference;
                    if (IsNotNullOrExpired(current))
                        return current.Value;
                }
                if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
                {
                    lock (lockObj)
                    {
                        var current = valueReference;
                        if (IsNotNullOrExpired(current))
                            return current.Value;

                        this.valueReference = new ValueAndExpires(valueGenerator());

                        return valueReference.Value;
                    }
                }

                else if (mode == LazyThreadSafetyMode.PublicationOnly)
                {
                    var newValue = valueGenerator();

                    lock (lockObj)
                    {
                        var current = valueReference;
                        if (IsNotNullOrExpired(current))
                            return current.Value;

                        valueReference = new ValueAndExpires(newValue);
                        return valueReference.Value;
                    }
                }
                else
                {
                    var current = new ValueAndExpires(valueGenerator());
                    valueReference = current;
                    return current.Value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsNotNullOrExpired(ValueAndExpires current)
        {
            return current != null && current.Expires > now();
        }
    }
}
