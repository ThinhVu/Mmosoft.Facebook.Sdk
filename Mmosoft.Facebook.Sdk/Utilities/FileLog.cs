namespace Mmosoft.Facebook.Utils
{
    using System.IO;

    public class FileLogger : ILogger
    {        
        private StreamWriter mWriter;        
        /// <summary>
        /// Init new instance of file logger
        /// </summary>
        /// <param name="logFolder">Folder contain log files</param>
        public FileLogger()
        {
            mWriter = new StreamWriter(File.OpenWrite("log.txt"));
        }
        public void WriteLine(string log)
        {            
            mWriter.WriteLine(log);            
        }        
        public void Dispose()
        {
            if (mWriter != null)
            {                
                mWriter.Flush();
                mWriter.Close();
                mWriter.Dispose();
            }            
        }
    }
}
