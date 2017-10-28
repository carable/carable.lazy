using System;
using System.Threading;
using System.Threading.Tasks;

namespace Carable.Lazy
{
    /// <summary>
    /// Async version of Lazy 
    /// </summary>
    public class LazyAsync<T>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueGenerator">A delegate which generates a new value</param>
        /// <param name="mode"></param>
        public LazyAsync(Func<Task<T>> valueGenerator, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly)
        {
            this.mode = mode;
            if (mode == LazyThreadSafetyMode.ExecutionAndPublication) throw new ArgumentException("Cant use execution and publication locking");
            this.valueGenerator = valueGenerator ?? throw new ArgumentNullException(nameof(valueGenerator));
        }

        /// <summary>
        /// 
        /// </summary>
        public LazyAsync(Func<Task<T>> valueGenerator)
            : this(valueGenerator, LazyThreadSafetyMode.PublicationOnly)
        {
        }

        private readonly LazyThreadSafetyMode mode;
        private readonly Func<Task<T>> valueGenerator;

        private readonly object lockObj = new object();
        object valueReference;

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
                        if (current != null)
                            return (T)current;
                        var newValue = await valueGenerator();

                        lock (lockObj)
                        {
                            valueReference = newValue;
                            return (T)valueReference;
                        }
                    }
                case LazyThreadSafetyMode.None:
                    {
                        var current = valueReference;
                        if (current != null)
                            return (T)current;

                        current = await valueGenerator();
                        valueReference = current;
                        return (T)valueReference;
                    }
                default:
                    throw new Exception(mode.ToString());
            }

        }
    }
}
