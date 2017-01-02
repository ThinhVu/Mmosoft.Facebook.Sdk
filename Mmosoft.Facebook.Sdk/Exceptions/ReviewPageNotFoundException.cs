using System;
using System.Runtime.Serialization;

namespace Mmosoft.Facebook.Sdk.Exceptions
{
    [Serializable]
    public class ReviewPageNotFoundException : Exception
    {
        public ReviewPageNotFoundException() { }
        public ReviewPageNotFoundException(string message) : base(message) { }
        public ReviewPageNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ReviewPageNotFoundException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
