namespace Mmosoft.Facebook.Sdk.Models.User
{
    public class ContactInfo
    {
        private string _mobile;
        public string Mobile
        {
            get { return _mobile; }
            set { _mobile = value; }
        }

        private string _email;
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }
        
        private string _website;
        public string Website
        {
            get { return _website; }
            set { _website = value; }
        }
        
        // ... not support
    }
}
