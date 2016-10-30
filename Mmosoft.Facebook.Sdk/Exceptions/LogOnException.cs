using System;
using System.Runtime.Serialization;

namespace Mmosoft.Facebook.Sdk.Exceptions
{

    [Serializable]
    public class LogOnException : Exception
    {
        public LogOnException() { }
        public LogOnException(string message) : base(message) { }
        public LogOnException(string message, Exception inner) : base(message, inner) { }
        protected LogOnException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
