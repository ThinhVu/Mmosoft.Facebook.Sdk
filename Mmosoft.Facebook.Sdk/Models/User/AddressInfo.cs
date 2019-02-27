namespace Mmosoft.Facebook.Sdk.Models.User
{
    public class AddressInfo
    {
        private string _homeTown;
        public string HomeTown
        {
            get { return _homeTown; }
            set { _homeTown = value; }
        }

        private string _city;
        public string City
        {
            get { return _city; }
            set { _city = value; }
        }

        private string _country;
        public string Country
        {
            get { return _country; }
            set { _country = value; }
        }       
    }
}
