namespace FacebookAPI.Models.Exception
{
    public class ContentNotFoundException : System.Exception
    {        
        public ContentNotFoundException() : base("Content not found")
        {

        }

        public ContentNotFoundException(string message) : base(message)
        {

        }


        public ContentNotFoundException(string message, System.Exception inner) : base (message, inner)
        {

        }
    }
}
