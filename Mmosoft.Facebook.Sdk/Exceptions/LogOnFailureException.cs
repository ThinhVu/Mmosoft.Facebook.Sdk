using System;
using System.Runtime.Serialization;

namespace Mmosoft.Facebook.Sdk.Exceptions
{

    [Serializable]
    public class LogOnFailureException : Exception
    {
        public LogOnFailureException() { }
        public LogOnFailureException(string message) : base(message) { }
        public LogOnFailureException(string message, Exception inner) : base(message, inner) { }
        protected LogOnFailureException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
