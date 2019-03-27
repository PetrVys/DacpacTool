using System;
using System.IO;

namespace DacpacTool
{
    public static class Assert
    {
        public static void FileExists(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.Error.WriteLine(string.Format("ERROR: Input file {0} does not exist!", fileName));
                Environment.Exit(1);
            }
        }
    }
}
