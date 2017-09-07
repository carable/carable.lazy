using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Carable.Lazy.Internals;

namespace Carable.Lazy
{
    /// <summary>
    /// Lazy expiry 
    /// </summary>
    public class LazyExpiry<T>
    {

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
        public LazyExpiry(Func<Tuple<T, DateTime>> valueGenerator)
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
        ValueAndExpires<T> valueReference;
        private readonly Func<DateTime> now;

        /// <summary>
        /// Get current value
        /// </summary>
        public T Value
        {
            get
            {
                switch (mode)
                {
                    case LazyThreadSafetyMode.ExecutionAndPublication:
                        lock (lockObj)
                        {
                            var current = valueReference;
                            if (IsNotNullOrExpired(current))
                                return current.Value;

                            this.valueReference = new ValueAndExpires<T>(valueGenerator());

                            return valueReference.Value;
                        }
                    case LazyThreadSafetyMode.PublicationOnly:
                        {
                            var current = valueReference;
                            if (IsNotNullOrExpired(current))
                                return current.Value;
                            var newValue = valueGenerator();

                            lock (lockObj)
                            {
                                valueReference = new ValueAndExpires<T>(newValue);
                                return valueReference.Value;
                            }
                        }
                    default:
                        {
                            var current = valueReference;
                            if (IsNotNullOrExpired(current))
                                return current.Value;
                            
                            current = new ValueAndExpires<T>(valueGenerator());
                            valueReference = current;
                            return valueReference.Value;
                        }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsNotNullOrExpired(ValueAndExpires<T> current)
        {
            return current != null && current.Expires > now();
        }
    }
}
