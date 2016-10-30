using System;
using System.Runtime.Serialization;

namespace Mmosoft.Facebook.Sdk.Exceptions
{
    [Serializable]
    public class NodeNotFoundException : Exception
    {
        public NodeNotFoundException() { }
        public NodeNotFoundException(string message) : base(message) { }
        public NodeNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected NodeNotFoundException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }  
}
