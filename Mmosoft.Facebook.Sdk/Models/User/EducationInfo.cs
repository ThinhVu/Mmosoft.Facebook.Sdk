using System;

namespace Mmosoft.Facebook.Sdk.Models.User
{
    public class EducationInfo
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private DateTime _start;

        public DateTime Start
        {
            get { return _start; }
            set { _start = value; }
        }

        private DateTime _end;

        public DateTime End
        {
            get { return _end; }
            set { _end = value; }
        }   
    }
}
