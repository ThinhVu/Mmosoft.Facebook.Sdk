using System;
using System.Runtime.Serialization;

namespace Mmosoft.Facebook.Sdk.Exceptions
{
    [Serializable]
    public class PageNotFoundException : Exception
    {
        public PageNotFoundException() { }
        public PageNotFoundException(string message) : base(message) { }
        public PageNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected PageNotFoundException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
