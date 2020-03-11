using System;
using System.IO;

namespace ReadFileService {
    class Util {
        public static void Log(string lines) {
            VerifyDir(System.AppDomain.CurrentDomain.BaseDirectory + @"\logs\");
            string fileName = DateTime.Now.Day.ToString("00") + DateTime.Now.Month.ToString("00") + DateTime.Now.Year.ToString() + "_Logs.txt";

            try {
                StreamWriter file = new StreamWriter(System.AppDomain.CurrentDomain.BaseDirectory + @"\logs\" + fileName, true);
                file.WriteLine(DateTime.Now.ToString() + ": " + lines);
                file.Close();
            }
            catch (Exception) { }
        }

        public static void VerifyDir(string path) {
            try {
                DirectoryInfo dir = new DirectoryInfo(path);

                if (!dir.Exists) {
                    dir.Create();
                }
            }
            catch { }
        }
    }
}
