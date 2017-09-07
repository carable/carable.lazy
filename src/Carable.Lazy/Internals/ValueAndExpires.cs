using System;
namespace Carable.Lazy.Internals
{
    class ValueAndExpires<T>
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
}
