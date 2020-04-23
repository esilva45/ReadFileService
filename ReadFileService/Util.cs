using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ReadFileService {
    class Util {
        public static void Log(string lines) {
            VerifyDir(AppDomain.CurrentDomain.BaseDirectory + @"/logs");
            string fileName = DateTime.Now.Day.ToString("00") + DateTime.Now.Month.ToString("00") + DateTime.Now.Year.ToString() + "_Logs.out";

            try {
                StreamWriter file = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"/logs/" + fileName, true);
                file.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff", CultureInfo.InvariantCulture) + " " + lines);
                file.Close();
            }
            catch (Exception) { }
        }

        private static void VerifyDir(string path) {
            try {
                DirectoryInfo dir = new DirectoryInfo(path);

                if (!dir.Exists) {
                    dir.Create();
                }
            }
            catch { }
        }

        public static string FlattenException(Exception exception) {
            var stringBuilder = new StringBuilder();

            while (exception != null) {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);
                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
