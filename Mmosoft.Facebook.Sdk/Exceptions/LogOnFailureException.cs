using System;
using System.Runtime.Serialization;

namespace Mmosoft.Facebook.Sdk.Exceptions
{
    [Serializable]
    public class UnAuthorizedException : Exception
    {
        public UnAuthorizedException() { }
        public UnAuthorizedException(string message) : base(message) { }
        public UnAuthorizedException(string message, Exception inner) : base(message, inner) { }
        protected UnAuthorizedException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
