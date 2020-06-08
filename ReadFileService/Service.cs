using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Xml.Linq;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace ReadFileService {
    public partial class Service : ServiceBase {
        private static IList<string> CallID;
        private static List<Call> Calls;
        public static Timer aTimer = null;
        private const short DefaultLines = 5;
        private static string file_in = "";
        private static int socket_port = 0;
        private static string queue = "";
        private static string license = "";
        private static int interval = 0;

        public Service() {
            InitializeComponent();
        }

        protected override void OnStart(string[] args) {
            TailfParameters prms = new TailfParameters(args);

            try {
                prms.Parse();

                XElement configXml = XElement.Load(AppDomain.CurrentDomain.BaseDirectory + @"/config.xml");
                file_in = configXml.Element("FileIn").Value.ToString();
                socket_port = int.Parse(configXml.Element("SocketPort").Value.ToString());
                queue = configXml.Element("Queue").Value.ToString();
                license = configXml.Element("LicenseKey").Value.ToString();
                interval = int.Parse(configXml.Element("TimeInterval").Value.ToString());
                CallID = new List<string>();

                if (!License.VerifyLicence(license)) {
                    //this.Stop();
                    Environment.Exit(0);
                }

                if (interval > 0) {
                    aTimer = new Timer(OnTimedEvent, null, interval, Timeout.Infinite);
                }

                Server.Start(socket_port);
                Calls = new List<Call>();

                int.TryParse(prms.NOfLines, out int n);

                if (n == 0) {
                    n = DefaultLines;
                }

                Tail tail = new Tail(file_in, n);
                tail.LineFilter = prms.Filter;
                tail.Changed += new EventHandler<Tail.TailEventArgs>(TailChanged);
                tail.Run();
            }
            catch (Exception e) {
                Util.Log("Method error OnStart: " + Util.FlattenException(e));
            }
        }

        protected override void OnStop() {
            Server.CloseAll();
        }

        private static void OnTimedEvent(object state) {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Server.Message("&");
            aTimer.Change(Math.Max(0, interval - watch.ElapsedMilliseconds), Timeout.Infinite);
        }

        private static void TailChanged(object sender, Tail.TailEventArgs e) {
            try {
                string tmp = "";
                string InternalParty = "";
                string ExternalParty = "";

                if (e.Line.Contains("Status=Connected")) {
                    int index1 = e.Line.IndexOf("ExternalParty=") + 14;
                    ExternalParty = e.Line.Substring(index1, e.Line.IndexOf("InternalParty") - index1);
                    ExternalParty = ExternalParty.Replace(Environment.NewLine, "").Trim();

                    if (queue.Contains(ExternalParty)) {
                        return;
                    }
                }

                if ((e.Line.Contains("Status=Ringing") || e.Line.Contains("Status=Connected")) && e.Line.Contains("Wqueue")) {
                    return;
                }

                if ((e.Line.Contains("Status=Connected") || e.Line.Contains("Status=Ringing")) && !e.Line.Contains("Wqueue")) {
                    int index0 = e.Line.IndexOf("HistoryIDOfTheCall=") + 19;
                    tmp = e.Line.Substring(index0, e.Line.IndexOf("OriginatedBy") - index0);
                    tmp = tmp.Replace(Environment.NewLine, "").Trim();
                }

                if (!string.IsNullOrEmpty(tmp) && (!Calls.Any(n => n.CallID == tmp && n.Connected == true))) {
                    if (e.Line.Contains("Status=Ringing") && (!CallID.Contains(tmp))) {
                        int index1 = e.Line.IndexOf("ExternalParty=") + 14;
                        ExternalParty = e.Line.Substring(index1, e.Line.IndexOf("InternalParty") - index1);
                        ExternalParty = ExternalParty.Replace(Environment.NewLine, "").Trim();

                        int index2 = e.Line.IndexOf("DN=Wextension") + 17;
                        InternalParty = e.Line.Substring(index2, e.Line.IndexOf("OtherCallParties") - index2);
                        InternalParty = InternalParty.Replace(":", "").Trim();
                        InternalParty = InternalParty.Replace(Environment.NewLine, "").Trim();

                        CallID.Add(tmp);
                        Calls.Add(new Call() { CallID = tmp, When = DateTime.Now, Internal = InternalParty, External = ExternalParty, Connected = false });

                        Server.Message(tmp + "|Discando|" + ExternalParty + "|" + InternalParty + "@");
                        return;
                    }

                    if (e.Line.Contains("Status=Connected") && (CallID.Contains(tmp))) {
                        foreach (var call in Calls.ToList()) {
                            if (call.CallID.Equals(tmp)) {
                                InternalParty = call.Internal.ToString();
                                ExternalParty = call.External.ToString();
                                Calls.RemoveAt(Calls.IndexOf(call));
                                Calls.Add(new Call() { CallID = tmp, When = DateTime.Now, Internal = InternalParty, External = ExternalParty, Connected = true });
                                break;
                            }
                        }

                        CallID.Remove(tmp);
                        Server.Message(tmp + "|Em Conversacao|" + ExternalParty + "|" + InternalParty + "@");
                        ClearCall();
                        return;
                    }
                }
            }
            catch (Exception ex) {
                Util.Log("Method error TailChanged: " + Util.FlattenException(ex));
            }
        }

        private static void ClearCall() {
            foreach (var call in Calls.ToList()) {
                if (call.When < DateTime.Now.AddMinutes(-30)) {
                    Calls.RemoveAt(Calls.IndexOf(call));
                    Util.Log("Call ID " + call.CallID + " " + call.When + " removed");
                }
            }
        }
    }

    public class Call {
        public string CallID { get; set; }
        public DateTime When { get; set; }
        public string Internal { get; set; }
        public string External { get; set; }
        public bool Connected { get; set; }
    }
}
