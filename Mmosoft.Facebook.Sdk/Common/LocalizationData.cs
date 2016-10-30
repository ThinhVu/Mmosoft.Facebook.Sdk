using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mmosoft.Facebook.Sdk.Common
{
    /// <summary>
    /// Class contain localization data
    /// </summary>
    public static class LocalizationData
    {
        public static List<string> IsGroupAdministrator = new List<string>{
            "Admin",        // English
            "Quản trị viên" // Vietnamese
        };

        public static List<string> PageNotFound = new List<string>
        {
            "The page you requested cannot be displayed right now." // English
        };
    }
}
