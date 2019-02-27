using System.Collections.Generic;
using Mmosoft.Facebook.Sdk.Models.User;

namespace Mmosoft.Facebook.Sdk.Models.Group
{
    public class GroupInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<GroupMember> Members { get; set; }

        public GroupInfo()
        {
            Id = string.Empty;
            Name = string.Empty;
            Members = new List<GroupMember>();
        }
    }
}
