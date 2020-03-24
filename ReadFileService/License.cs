using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace ReadFileService {
    class License {
        private static string pwd = "PuR@94zG";
        private static readonly string fileName = AppDomain.CurrentDomain.BaseDirectory + @"/license.lic";

        public static void LicenseGenerator() {
            string uuid = Guid.NewGuid().ToString() + Environment.ProcessorCount + Environment.MachineName + Environment.OSVersion.Platform 
                + Environment.Is64BitOperatingSystem;
            uuid = uuid.Replace("-", "").Replace(" ", "").ToUpper();

            if (!File.Exists(fileName)) {
                StreamWriter file = new StreamWriter(fileName, true);
                file.WriteLine(uuid);
                file.Close();
                EncryptFile(fileName, pwd);
            }
        }

        public static bool VerifyLicence(string licence) {
            string uuid = DecryptFile(fileName, pwd);
            string hardware = Environment.ProcessorCount + Environment.MachineName + Environment.OSVersion.Platform + Environment.Is64BitOperatingSystem;
            hardware = hardware.Replace("-", "").Replace(" ", "").ToUpper();

            if (!uuid.Contains(hardware)) {
                File.Delete(fileName);
                LicenseGenerator();
                uuid = DecryptFile(fileName, pwd);
            }

            var sha1 = new SHA1Managed();
            var plaintextBytes = Encoding.UTF8.GetBytes(uuid);
            var hashBytes = sha1.ComputeHash(plaintextBytes);
            var sb = new StringBuilder();

            foreach (var hashByte in hashBytes) {
                sb.AppendFormat("{0:x2}", hashByte);
            }

            string licenseKey = FormatLicenseKey(GetMd5Sum(sb.ToString()));

            if (!licence.Equals(licenseKey)) {
                Util.Log("Licenca invalida, entre em contato com o suporte e informe o codigo " + (sb.ToString()));
                return false;
            }

            return true;
        }

        private static void EncryptFile(string sInputFilename, string sKey) {
            FileStream fsInput = new FileStream(sInputFilename, FileMode.Open, FileAccess.ReadWrite);
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            DES.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            DES.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
            ICryptoTransform desencrypt = DES.CreateEncryptor();
            CryptoStream cryptostream = new CryptoStream(fsInput, desencrypt, CryptoStreamMode.Write);
            byte[] bytearrayinput = new byte[fsInput.Length];
            fsInput.Read(bytearrayinput, 0, bytearrayinput.Length);
            fsInput.SetLength(0);
            cryptostream.Write(bytearrayinput, 0, bytearrayinput.Length);
            cryptostream.Close();
            fsInput.Close();
        }

        private static string DecryptFile(string sInputFilename, string sKey) {
            string key = "";
            var DES = new DESCryptoServiceProvider();
            DES.Key = Encoding.ASCII.GetBytes(sKey);
            DES.IV = Encoding.ASCII.GetBytes(sKey);
            ICryptoTransform desdecrypt = DES.CreateDecryptor();

            if (!File.Exists(sInputFilename)) {
                LicenseGenerator();
            }

            using (var fsread = new FileStream(sInputFilename, FileMode.Open, FileAccess.ReadWrite)) {
                using (var cryptostreamDecr = new CryptoStream(fsread, desdecrypt, CryptoStreamMode.Read)) {
                    int data;

                    fsread.Flush();

                    using (var ms = new MemoryStream()) {
                        while ((data = cryptostreamDecr.ReadByte()) != -1) {
                            ms.WriteByte((byte)data);
                        }

                        cryptostreamDecr.Close();
                        key = Encoding.ASCII.GetString(ms.ToArray());
                    }
                }
            }

            return key;
        }

        private static string GenerateLicenseKey(string productIdentifier) {
            return FormatLicenseKey(GetMd5Sum(productIdentifier));
        }

        private static string GetMd5Sum(string productIdentifier) {
            Encoder enc = Encoding.Unicode.GetEncoder();
            byte[] unicodeText = new byte[productIdentifier.Length * 2];
            enc.GetBytes(productIdentifier.ToCharArray(), 0, productIdentifier.Length, unicodeText, 0, true);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < result.Length; i++) {
                sb.Append(result[i].ToString("X2"));
            }

            return sb.ToString();
        }

        private static string FormatLicenseKey(string productIdentifier) {
            productIdentifier = productIdentifier.Substring(0, 28).ToUpper();
            char[] serialArray = productIdentifier.ToCharArray();
            StringBuilder licenseKey = new StringBuilder();

            int j = 0;

            for (int i = 0; i < 28; i++) {
                for (j = i; j < 4 + i; j++) {
                    licenseKey.Append(serialArray[j]);
                }
                if (j == 28) {
                    break;
                } else {
                    i = (j) - 1;
                    licenseKey.Append("-");
                }
            }

            return licenseKey.ToString();
        }
    }
}
