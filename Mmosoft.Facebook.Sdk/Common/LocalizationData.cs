using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Common
{
    /// <summary>
    /// Class contain localization data
    /// </summary>
    public static class LocalizationData
    {
        public static ICollection<string> IsGroupAdministrator { get; private set; }

        public static ICollection<string> PageNotFound { get; private set; }

        static LocalizationData()
        {
            IsGroupAdministrator = new List<string>
            {
                "Admin",        // English
                "Quản trị viên" // Vietnamese
            };
            PageNotFound = new List<string>
            {
                "The page you requested cannot be displayed right now." // English
            };
        }
    }
}
