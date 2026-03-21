using System;

namespace Electrons.Core.Net8
{
    public class BaseballGameException : Exception
    {
        public BaseballGameException(string message) : base(message) { }
        public BaseballGameException(string message, Exception inner) : base(message, inner) { }
    }
}
