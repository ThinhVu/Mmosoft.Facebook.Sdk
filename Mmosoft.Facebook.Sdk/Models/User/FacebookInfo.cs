using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Models.User
{
    /// <summary>
    /// Contain facebook info like Url, AvatarUrl, Id, AliasName, Display name, ...
    /// </summary>
    public class FacebookInfo
    {
        // [at least id or alias]
        private string _id;
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }        
        private string _alias;
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; }
        }
        // [optional]
        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        // [optional]
        private string _avatarUrl;
        public string AvatarUrl
        {
            get { return _avatarUrl; }
            set { _avatarUrl = value; }
        }
        // [optional]
        private string _fbUrl;
        public string FbUrl
        {
            get { return _fbUrl; }
            set { _fbUrl = value; }
        }
        // [optional]
        private List<string> _Fbfiends;
        public List<string> FbFriends
        {
            get { return _Fbfiends; }
            set { _Fbfiends = value; }
        }        
    }
}
