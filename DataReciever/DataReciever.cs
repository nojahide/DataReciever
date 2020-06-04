using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DataReciever
{
    public partial class DataReciever : Form
    {
        NetReciever reciever = new NetReciever();       //通信クラス

        public DataReciever()
        {
            InitializeComponent();
        }

        private void DataReciever_Load(object sender, EventArgs e)
        {
            reciever.OnError += new NetReciever.ErrorEventHandler(NetReciever_OnError);
            reciever.OnReceiveData += new NetReciever.ReceiveEventHandler(NetReciever_Recieve);

        }


        /// <summary>
        /// 受信開始ボタン：アプリ起動時は自動的に受信開始にする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            reciever.Start();

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            reciever.Stop();
        }

        /// <summary>
        /// データ受信イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NetReciever_Recieve(object sender, string s)
        {
            this.Invoke(new DelegateRecvProc(RecieveProc), s);
        }

        /// <summary>
        /// スレッドデリゲート用
        /// </summary>
        /// <param name="s"></param>
        delegate void DelegateRecvProc(string s);
        private void RecieveProc(string s)
        {
            AddMessageText(s);
        }

        /// <summary>
        /// エラーイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="s"></param>
        private void NetReciever_OnError(object sender, EventArgs e, string s)
        {
            this.Invoke(new DelegateRecvProc(RecieveProc), s);
        }

        private string JsonParse(string s)
        {
            //JsonTextReader jrader = new JsonTextReader();
            //jrader.
            return null;
        }

        /// <summary>
        /// テキストボックスにメッセージ追加
        /// </summary>
        /// <param name="s"></param>
        private void AddMessageText(string s)
        {
            string dt = DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss] ");
            string message = dt + s + "\r\n";
            textMessage.HideSelection = false;
            textMessage.AppendText(message);
            
            //キャレットを末尾に
            textMessage.Select(this.textMessage.Text.Length, 0);

        }

        private void btnTextClear_Click(object sender, EventArgs e)
        {
            textMessage.Clear();
        }

    }
}
