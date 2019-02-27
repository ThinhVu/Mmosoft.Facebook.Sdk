using System;

namespace Mmosoft.Facebook.Utils
{
    public class ConsoleLogger : ILogger
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void Dispose()
        {            
        }
    }
}
