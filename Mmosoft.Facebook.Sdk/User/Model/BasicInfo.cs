namespace Mmosoft.Facebook.Sdk.Models.User
{
    public class BasicInfo
    {
        private string _birthDay;
        public string BirthDay
        {
            get { return _birthDay; }
            set { _birthDay = value; }
        }

        private string _gender;

        public string Gender
        {
            get { return _gender; }
            set { _gender = value; }
        }

        private string _interestedIn;
        public string InterestedIn
        {
            get { return _interestedIn; }
            set { _interestedIn = value; }
        }

        private string _languages;
        public string Languages
        {
            get { return _languages; }
            set { _languages = value; }
        }

        private string _religiousViews;
        public string ReligiousViews
        {
            get { return _religiousViews; }
            set { _religiousViews = value; }
        }

        private string _polictialViews;
        public string PolictialViews
        {
            get { return _polictialViews; }
            set { _polictialViews = value; }
        }        
    }
}
