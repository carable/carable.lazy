using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Carable.Lazy.Internals;

namespace Carable.Lazy
{
    /// <summary>
    /// Async version of Lazy expiry 
    /// </summary>
    public class LazyExpiryAsync<T>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueGenerator">A delegate which generates a new value when old value has expired</param>
        /// <param name="mode"></param>
        /// <param name="now"></param>
        public LazyExpiryAsync(Func<Task<Tuple<T, DateTime>>> valueGenerator, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly, Func<DateTime> now = null)
        {
            this.mode = mode;
            if (mode == LazyThreadSafetyMode.ExecutionAndPublication) throw new ArgumentException("Cant use execution and publication locking");
            this.valueGenerator = valueGenerator ?? throw new ArgumentNullException(nameof(valueGenerator));
            this.now = now != null ? now : DefaultNow;
        }

        /// <summary>
        /// 
        /// </summary>
        public LazyExpiryAsync(Func<Task<Tuple<T, DateTime>>> valueGenerator)
            : this(valueGenerator, LazyThreadSafetyMode.PublicationOnly)
        {
        }

        private readonly LazyThreadSafetyMode mode;
        private readonly Func<Task<Tuple<T, DateTime>>> valueGenerator;

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
        public async Task<T> GetValue()
        {
            switch (mode)
            {
                case LazyThreadSafetyMode.PublicationOnly:
                    {
                        var current = valueReference;
                        if (IsNotNullOrExpired(current))
                            return current.Value;
                        var newValue = await valueGenerator();

                        lock (lockObj)
                        {
                            valueReference = new ValueAndExpires<T>(newValue);
                            return valueReference.Value;
                        }
                    }
                case LazyThreadSafetyMode.None:
                    {
                        var current = valueReference;
                        if (IsNotNullOrExpired(current))
                            return current.Value;

                        current = new ValueAndExpires<T>(await valueGenerator());
                        valueReference = current;
                        return valueReference.Value;
                    }
                default:
                    throw new Exception(mode.ToString());
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsNotNullOrExpired(ValueAndExpires<T> current)
        {
            return current != null && current.Expires > now();
        }
    }
}
