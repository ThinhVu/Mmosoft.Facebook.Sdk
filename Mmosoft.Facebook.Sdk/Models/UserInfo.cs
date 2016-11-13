using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Models
{
    public class UserInfo
    {       
        public string _id { get; set; }                
        public string Name { get; set; }
        public string Avatar { get; set; }
        public List<string> Friends { get; set; }
    }    
}
