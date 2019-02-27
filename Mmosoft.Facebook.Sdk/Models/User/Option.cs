
namespace Mmosoft.Facebook.Sdk.Models.User
{
    public class UserInfoOption
    {
        private FacebookInfoOption _fbInfoOption;
        public FacebookInfoOption FbInfoOption
        {
            get { return _fbInfoOption; }
            set { _fbInfoOption = value; }
        }

        private bool _includeWorkInfo;
        public bool IncludeWorkInfo
        {
            get { return _includeWorkInfo; }
            set { _includeWorkInfo = value; }
        }

        private bool _includeAddressInfo;
        public bool IncludeAddressInfo
        {
            get { return _includeAddressInfo; }
            set { _includeAddressInfo = value; }
        }

        private bool _includeEduInfo;
        public bool IncludeEduInfo
        {
            get { return _includeEduInfo; }
            set { _includeEduInfo = value; }
        }

        private bool _includeRelationshipInfo;
        public bool IncludeRelationshipInfo
        {
            get { return _includeRelationshipInfo; }
            set { _includeRelationshipInfo = value; }
        }

        private bool _includeContactInfo;
        public bool IncludeContactInfo
        {
            get { return _includeContactInfo; }
            set { _includeContactInfo = value; }
        }

        private bool _includeBasicInfo;
        public bool IncludeBasicInfo
        {
            get { return _includeBasicInfo; }
            set { _includeBasicInfo = value; }
        }

        public UserInfoOption()
        {
            _fbInfoOption = new FacebookInfoOption();
        }
    }

    public class FacebookInfoOption
    {
        private bool _includeUserId;
        public bool IncludeUserId
        {
            get { return _includeUserId; }
            set { _includeUserId = value; }
        }

        // we don't need user alias

        private bool _includeUserDisplayName;
        public bool IncludeUserDisplayName
        {
            get { return _includeUserDisplayName; }
            set { _includeUserDisplayName = value; }
        }

        private bool _includeAvatarUrl;
        public bool IncludeAvatarUrl
        {
            get { return _includeAvatarUrl; }
            set { _includeAvatarUrl = value; }
        }

        private bool _includeFbUrl;
        public bool IncludeFbUrl
        {
            get { return _includeFbUrl; }
            set { _includeFbUrl = value; }
        }

        private bool _includeFbFriends;
        public bool IncludeFbFriends
        {
            get { return _includeFbFriends; }
            set { _includeFbFriends = value; }
        }
    }
}
