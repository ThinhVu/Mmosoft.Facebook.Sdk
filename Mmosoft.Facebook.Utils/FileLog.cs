namespace Mmosoft.Facebook.Utils
{
    using System.IO;

    public class FileLogger : ILogger
    {
        private const int MAXIMUM_LINE_EACH_FILE = 10000;
        private StreamWriter mWriter;
        private int mTotalLineLogged;
        private int mTotalFileLogged; // using file count as a file name.
        private string mLogFolder;
        
        /// <summary>
        /// Init new instance of file logger
        /// </summary>
        /// <param name="logFolder">Folder contain log files</param>
        public FileLogger(string logFolder)
        {                        
            mTotalLineLogged = 0;
            mTotalFileLogged = 0;

            mLogFolder = logFolder;
            if (!Directory.Exists(mLogFolder))
                Directory.CreateDirectory(mLogFolder);
             
            this.createLogFile();
        }
        public void WriteLine(string log)
        {
            if (mTotalLineLogged >= MAXIMUM_LINE_EACH_FILE)
                createLogFile();
            mWriter.WriteLine(log);
            mTotalLineLogged++;
        }
        private void createLogFile()
        {
            // if writer being use
            if (mWriter != null)
            {
                mWriter.Flush();
                mWriter.Close();
                mWriter.Dispose();
            }
            mWriter = new StreamWriter(File.OpenWrite(createFilePath()));
            mTotalLineLogged = 0;
        }
        private string createFilePath()
        {            
            mTotalFileLogged++;
            if (!mLogFolder.EndsWith("\\"))
                return mLogFolder + "\\" + mTotalFileLogged + ".txt";
            else
                return mLogFolder + mTotalFileLogged + ".txt";
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
