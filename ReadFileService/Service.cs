using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.IO;
using System.Xml.Linq;

namespace ReadFileService {
    public partial class Service : ServiceBase {
        public static IList<string> CallID;
        private const short DefaultLines = 5;
        public static String Internal = "";
        public static String External = "";
        public static String file_in = "";
        public static String file_out = "";
        public static String queue = "";
        public static String license = "";
        public static StreamWriter file = null;

        public Service() {
            InitializeComponent();
        }

        protected override void OnStart(string[] args) {
            TailfParameters prms = new TailfParameters(args);

            try {
                prms.Parse();

                XElement configXml = XElement.Load(System.AppDomain.CurrentDomain.BaseDirectory + @"\config.xml");
                file_in = configXml.Element("FileIn").Value.ToString();
                file_out = configXml.Element("FileOut").Value.ToString();
                queue = configXml.Element("Queue").Value.ToString();
                license = configXml.Element("LicenseKey").Value.ToString();
                CallID = new List<string>();

                if (!License.VerifyLicence(license)) {
                    this.Stop();
                }

                int.TryParse(prms.NOfLines, out int n);

                if (n == 0) {
                    n = DefaultLines;
                }

                Tail tail = new Tail(file_in, n);
                tail.LineFilter = prms.Filter;
                tail.Changed += new EventHandler<Tail.TailEventArgs>(Tail_Changed);
                tail.Run();
                
                //tail.Stop();
            }
            catch (Exception e) {
                Util.Log(e.ToString());
            }
        }

        protected override void OnStop() {
        }

        private static void Tail_Changed(object sender, Tail.TailEventArgs e) {
            try {
                String tmp = "";

                if (e.Line.Contains("Status=Connected")) {
                    int index1 = e.Line.IndexOf("ExternalParty=") + 14;
                    External = e.Line.Substring(index1, e.Line.IndexOf("InternalParty") - index1);
                    External = External.Replace(System.Environment.NewLine, "").Trim();

                    if (queue.Contains(External)) {
                        return;
                    }
                }

                if ((e.Line.Contains("Status=Ringing") || e.Line.Contains("Status=Connected")) && e.Line.Contains("Wqueue")) {
                    return;
                }

                if ((e.Line.Contains("Status=Connected") || e.Line.Contains("Status=Ringing")) && !e.Line.Contains("Wqueue")) {
                    int index0 = e.Line.IndexOf("HistoryIDOfTheCall=") + 19;
                    tmp = e.Line.Substring(index0, e.Line.IndexOf("OriginatedBy") - index0);
                    tmp = tmp.Replace(System.Environment.NewLine, "").Trim();
                }

                if (!string.IsNullOrEmpty(tmp)) {
                    if (e.Line.Contains("Status=Ringing") && (!CallID.Contains(tmp))) {
                        CallID.Add(tmp);

                        int index1 = e.Line.IndexOf("ExternalParty=") + 14;
                        External = e.Line.Substring(index1, e.Line.IndexOf("InternalParty") - index1);
                        External = External.Replace(System.Environment.NewLine, "").Trim();

                        int index2 = e.Line.IndexOf("DN=Wextension") + 17;
                        Internal = e.Line.Substring(index2, e.Line.IndexOf("OtherCallParties") - index2);
                        Internal = Internal.Replace(System.Environment.NewLine, "").Trim();

                        file = new StreamWriter(file_out, true);
                        file.WriteLine(tmp + "|Discando|" + External + "|" + Internal);
                        file.Close();
                        return;
                    }

                    if (e.Line.Contains("Status=Connected") && (CallID.Contains(tmp))) {
                        CallID.Remove(tmp);

                        int index1 = e.Line.IndexOf("ExternalParty=") + 14;
                        External = e.Line.Substring(index1, e.Line.IndexOf("InternalParty") - index1);
                        External = External.Replace(System.Environment.NewLine, "").Trim();

                        int index2 = e.Line.IndexOf("DN=Wextension") + 17;
                        Internal = e.Line.Substring(index2, e.Line.IndexOf("OtherCallParties") - index2);
                        Internal = Internal.Replace(System.Environment.NewLine, "").Trim();

                        file = new StreamWriter(file_out, true);
                        file.WriteLine(tmp + "|Em Conversação|" + External + "|" + Internal);
                        file.Close();
                        return;
                    }
                }
            }
            catch (Exception ex) {
                Util.Log(ex.ToString());
            }
        }
    }
}
