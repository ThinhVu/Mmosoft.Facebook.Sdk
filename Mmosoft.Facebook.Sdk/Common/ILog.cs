using System;
using System.IO;

namespace Mmosoft.Facebook.Sdk.Common
{
    public interface ILog : IDisposable
    {
        void Write(string log);
        void WriteL(string log);
    }

    public class ConsoleLog : ILog
    {
        public void Dispose()
        {
            
        }

        public void Write(string log)
        {
            Console.Write(log);
        }

        public void WriteL(string log)
        {
            Console.WriteLine(log);
        }
    }

    public class FileLog : ILog
    {
        private StreamWriter writer;

        public FileLog()
        {
            string dataPath = System.Configuration.ConfigurationManager.AppSettings["dataPath"];
            writer = File.AppendText(dataPath);
            writer.WriteLine("====================================================================");
            writer.WriteLine("=========== " + DateTime.Now.ToShortDateString() + " ===============");
        }

        public void Dispose()
        {            
            writer.Close();
            writer.Dispose();
        }

        public void Write(string log)
        {
            writer.Write(log);
        }

        public void WriteL(string log)
        {
            writer.WriteLine(log);
        }
    }
}
