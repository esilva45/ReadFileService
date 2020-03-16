using System;
using System.Management;
using System.Text;
using System.Security.Cryptography;

namespace ReadFileService {
    class License {
        public static bool VerifyLicence(string licence) {
            string hddSerial = GetHDDSerialNo();
            string mACAddress = GetMACAddress();
            string boardProductId = GetBoardProductId();
            string hashId = "DCOMApplicationSetting";

            string productIdentifier = (hddSerial + "-" + mACAddress + "-" + boardProductId + "-" + hashId).ToLower();
            var sha1 = new SHA1Managed();
            var plaintextBytes = Encoding.UTF8.GetBytes(productIdentifier);
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

        public static string GetBoardProductId() {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BaseBoard");

            foreach (ManagementObject wmi in searcher.Get()) {
                try {
                    return wmi.GetPropertyValue("Product").ToString();
                }
                catch { }
            }

            return "Unknown";
        }

        public static string GetHDDSerialNo() {
            ManagementClass mangnmt = new ManagementClass("Win32_LogicalDisk");
            ManagementObjectCollection mcol = mangnmt.GetInstances();
            string result = "";

            foreach (ManagementObject strt in mcol) {
                result += Convert.ToString(strt["VolumeSerialNumber"]);
            }
            return result;
        }

        public static string GetMACAddress() {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            string MACAddress = string.Empty;

            foreach (ManagementObject mo in moc) {
                if (MACAddress == string.Empty) {
                    if ((bool)mo["IPEnabled"] == true) MACAddress = mo["MacAddress"].ToString();
                }
                mo.Dispose();
            }

            MACAddress = MACAddress.Replace(":", "");
            return MACAddress;
        }

        static string GenerateLicenseKey(string productIdentifier) {
            return FormatLicenseKey(GetMd5Sum(productIdentifier));
        }

        static string GetMd5Sum(string productIdentifier) {
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

        static string FormatLicenseKey(string productIdentifier) {
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
