namespace Mmosoft.Facebook.Sdk.Models.User
{
    public enum RelationShipType
    {
        Single,
        Married,
        // ...
    }
    public class RelationshipInfo
    {
        private RelationShipType _type;
        public RelationShipType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private string _target;
        public string Target
        {
            get { return _target; }
            set { _target = value; }
        }
    }
}
