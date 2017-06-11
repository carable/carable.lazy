using System;
namespace Carable.Lazy.Tests
{
    public class FailException : Exception
    {
        public FailException()
        {
        }

        public FailException(string message) : base(message)
        {
        }

        public FailException(string message, Exception inner) : base(message, inner)
        {
        }

    }
}
