using System;
using System.Runtime.Serialization;

namespace Mmosoft.Facebook.Sdk.Exceptions
{
    [Serializable]
    public class MissingReviewPageException : Exception
    {
        public MissingReviewPageException() { }
        public MissingReviewPageException(string message) : base(message) { }
        public MissingReviewPageException(string message, Exception inner) : base(message, inner) { }
        protected MissingReviewPageException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
