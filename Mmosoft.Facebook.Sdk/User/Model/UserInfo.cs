using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Models.User
{
    public class UserInfo
    {
        private FacebookInfo _fbInfo;
        public FacebookInfo FBInfo
        {
            get { return _fbInfo; }
            set { _fbInfo = value; }
        }
        private List<WorkInfo> _works;
        public List<WorkInfo> Works
        {
            get { return _works; }
            set { _works = value; }
        }
        private AddressInfo _addr;
        public AddressInfo Address
        {
            get { return _addr; }
            set { _addr = value; }
        }
        private List<EducationInfo> _educations;
        public List<EducationInfo> Educations
        {
            get { return _educations; }
            set { _educations = value; }
        }
        private List<RelationshipInfo> _relationships;
        public List<RelationshipInfo> Relationships
        {
            get { return _relationships; }
            set { _relationships = value; }
        }
        private ContactInfo _contact;
        public ContactInfo Contact
        {
            get { return _contact; }
            set { _contact = value; }
        }
        private BasicInfo _basicInfo;
        public BasicInfo BasicInfo
        {
            get { return _basicInfo; }
            set { _basicInfo = value; }
        }
    }    
}
