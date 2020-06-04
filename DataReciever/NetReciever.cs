using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// 参考ってかかなりコピー
// https://docs.microsoft.com/ja-jp/dotnet/framework/network-programming/asynchronous-server-socket-example
// https://www.excellence-blog.com/2018/06/08/c-非同期ソケット通信で簡易サーバーを作成/


namespace DataReciever
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }


    /// <summary>
    /// 通信クラス
    /// </summary>
    class NetReciever
    {
        private const int ipport = 11000;   //ポート番号、面倒なので固定

        public static ManualResetEvent allDone = new ManualResetEvent(false);   // Thread signal. 
        private Task taskListen = null;

        //private bool IsEnableRecv = true;    //受信待機中かのフラグ
        //public IPEndPoint IPEndPoint { get; }           // サーバーのエンドポイント

        //  非同期処理をCancelするためのToken
        CancellationTokenSource tokenSource = null;

        // ======== イベント =========
        //データ受信イベント
        public delegate void ReceiveEventHandler(object sender, string e);
        public event ReceiveEventHandler OnReceiveData;

        ////データ送信イベント
        //public delegate void SendEventHandler(object sender, string e);
        //public event SendEventHandler OnSendData;

        ////接続OKイベント
        //public delegate void ConnectedEventHandler(EventArgs e, string s);
        //public event ConnectedEventHandler OnConnected;

        //Errorイベント
        public delegate void ErrorEventHandler(object sender, EventArgs e, string s);
        public event ErrorEventHandler OnError;




        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NetReciever()
        {

        }

        /// <summary>
        /// 処理開始
        /// </summary>
        /// <returns></returns>
        public int Start()
        {
            if (taskListen != null)
            {
                return 1;   // already run.
            }

            tokenSource = new CancellationTokenSource();
            taskListen = Task.Factory.StartNew(() =>
            {
                StartListening(tokenSource.Token);
            });

            return 0;
        }

        /// <summary>
        /// 処理停止
        /// </summary>
        /// <returns></returns>
        public int Stop()
        {
            tokenSource.Cancel();
            taskListen.Wait();

            return 0;
        }


        /// <summary>
        /// 接続待ちタスク
        /// </summary>
        private void StartListening(CancellationToken cancellation)
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            //////IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //////IPAddress ipAddress = ipHostInfo.AddressList[0];
            //////IPEndPoint localEndPoint = new IPEndPoint(ipAddress, ipport);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, ipport);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            //////Socket listener = new Socket(ipAddress.AddressFamily,
            //////    SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // キャンセルされてたら終了
                    if (cancellation.IsCancellationRequested)
                    {
                        listener.Shutdown(SocketShutdown.Both);
                        listener.Close();
                        listener.Dispose();
                        break;
                    }

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();

                }

            }
            catch (Exception e)
            {
                OnError(this, new EventArgs(), e.Message);
            }

        }

        /// <summary>
        /// AcceptCallback
        /// </summary>
        /// <param name="ar"></param>
        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                // JSONでもらうのでUTF-8固定
                state.sb.Append(Encoding.UTF8.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // 受信フォーマットはJSON＋<EOF>とする。
                    // All the data has been read from the client.  
                    OnReceiveData(this, content);

                    //オウム返しで応答
                    // Echo the data back to the client.  
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                //int bytesSent = handler.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}
