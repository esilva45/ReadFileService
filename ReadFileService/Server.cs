using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace ReadFileService {
    class Server {
        private static List<Socket> clientSockets = new List<Socket>();
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static Thread _socketThread;
        private static Socket handler;
        private static Socket listener;
        private static StateObject state = new StateObject();
        private static int socket_port = 0;

        public static void Start(int port) {
            socket_port = port;
            _socketThread = new Thread(SocketThreadFunc);
            _socketThread.Start();
            Util.Log("Server started, waiting for a connection");
        }

        private static void SocketThreadFunc(object state) {
            try {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ipAddress = IPAddress.Any;
                listener.Bind(new IPEndPoint(ipAddress, socket_port));
                listener.Listen(100);

                while (true) {
                    allDone.Reset();
                    listener.BeginAccept(AcceptCallback, listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception ex) {
                Util.Log("Method error SocketThreadFunc: " + Util.FlattenException(ex));
            }
        }

        private static void AcceptCallback(IAsyncResult ar) {
            try {
                XElement configXml = XElement.Load(AppDomain.CurrentDomain.BaseDirectory + @"\config.xml");
                string accept = configXml.Element("AcceptIP").Value.ToString();

                allDone.Set();
                listener = (Socket)ar.AsyncState;
                handler = listener.EndAccept(ar);

                if (accept.Contains(((IPEndPoint)handler.RemoteEndPoint).Address.ToString())) {
                    handler.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    state.workSocket = handler;
                    clientSockets.Add(handler);
                    Util.Log("Client " + handler.RemoteEndPoint + " connected");
                } else {
                    Util.Log("Access denied " + handler.RemoteEndPoint);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Disconnect(true);
                    handler.Close();
                    handler.Dispose();
                }
            }
            catch (Exception ex) {
                Util.Log("Method error AcceptCallback: " + Util.FlattenException(ex));
            }
        }

        public static void Message(string message) {
            List<Socket> clearClient = new List<Socket>();
            Socket tmpSocket = null;

            try {
                byte[] msg = Encoding.ASCII.GetBytes(message);

                foreach (Socket socket in clientSockets) {
                    try {
                        tmpSocket = socket;

                        if (SocketConnected(socket)) {
                            socket.Send(msg);
                            Util.Log("Sending to " + socket.RemoteEndPoint + ", msg [" + message + "]");                        
                        } else {
                            clearClient.Add(socket);
                        }
                    }
                    catch (SocketException exception) {
                        clearClient.Add(socket);
                        Util.Log("Error when sending message: " + socket + " - " + Util.FlattenException(exception));
                    }
                }
            }
            catch (Exception ex) {
                clearClient.Add(tmpSocket);
                Util.Log("Method error Message: " + Util.FlattenException(ex));
            }
            finally {
                foreach (Socket client in clearClient) {
                    Util.Log("Client " + client.RemoteEndPoint + " removed");
                    clientSockets.Remove(client);
                }
            }
        }

        private static bool SocketConnected(Socket s) {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);

            if (part1 && part2) {
                return false;
            } else {
                return true;
            }
        }

        public static void CloseAll() {
            try {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                handler.Dispose();
            }
            catch (Exception ex) {
                Util.Log("Method error CloseAll: " + Util.FlattenException(ex));
            }
        }
    }

    public class StateObject {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }
}
