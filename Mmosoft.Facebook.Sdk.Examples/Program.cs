using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mmosoft.Facebook.Sdk;

namespace ExecutableProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define fb client
            var fb = new FacebookClient(user: "your email", password: "your pass");
            // And invoke method
            fb.PostToWall("Send from Facebook SDK");
            fb.PostToGroup("Send from Facebook SDK-Group", "529073513939720");        }
    }
}
