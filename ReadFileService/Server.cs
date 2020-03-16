using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
                Util.Log(ex.ToString());
            }
        }

        private static void AcceptCallback(IAsyncResult ar) {
            try {
                allDone.Set();
                listener = (Socket)ar.AsyncState;
                handler = listener.EndAccept(ar);
                handler.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                state.workSocket = handler;
                clientSockets.Add(handler);
                Util.Log("Client connected " + handler.RemoteEndPoint);
            }
            catch (Exception ex) {
                Util.Log(ex.ToString());
            }
        }

        public static void Message(string message) {
            try {
                byte[] msg = Encoding.ASCII.GetBytes(message);

                foreach (Socket socket in clientSockets) {
                    if (SocketConnected(socket)) {
                        socket.Send(msg);
                        Util.Log("Send client " + handler.RemoteEndPoint  + ", msg " + message);
                    }
                }
            }
            catch (Exception ex) {
                Util.Log(ex.ToString());
            }
        }

        private static bool SocketConnected(Socket s) {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);

            if (part1 && part2)
                return false;
            else
                return true;
        }

        public static void CloseAll() {
            try {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                handler.Dispose();
            }
            catch (Exception ex) {
                Util.Log(ex.ToString());
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
