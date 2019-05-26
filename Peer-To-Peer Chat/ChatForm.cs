using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Peer_To_Peer_Chat
{
    public partial class ChatForm : Form
    {
        delegate void AddMessage(string message);

        string userName;

        const int port = 4000;
        const string broadcastAddress = "127.0.0.1";

        UdpClient receivingClient;
        UdpClient sendingClient; 

        Thread receivingThread;

        public ChatForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(ChatForm_Load);
            btnSend.Click += new EventHandler(btnSend_Click);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            txtMessage.Text = txtMessage.Text.TrimEnd();

            if (!string.IsNullOrEmpty(txtMessage.Text))
            {
                string toSend = userName + ":\n" + txtMessage.Text;
                byte[] data = Encoding.ASCII.GetBytes(toSend);
                sendingClient.Send(data, data.Length);
                txtMessage.Text = "";
            }

            txtMessage.Focus();
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            this.Hide();

            using (UserNameForm loginForm = new UserNameForm())
            {
                loginForm.ShowDialog();

                if (loginForm.UserName == "")
                    this.Close();
                else
                {
                    userName = loginForm.UserName;
                    this.Show();
                }
            }

            txtMessage.Focus();

            InitializeSender();
            InitializeReceiver();
        }

        private void InitializeSender()
        {
            sendingClient = new UdpClient(broadcastAddress, port);
            sendingClient.EnableBroadcast = true;
        }

        private void InitializeReceiver()
        {
            receivingClient = new UdpClient(port);

            ThreadStart start = new ThreadStart(Receiver);
            receivingThread = new Thread(start);
            receivingThread.IsBackground = true;
            receivingThread.Start();
        }

        private void Receiver()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            AddMessage messageDelegate = MessageReceived;

            while (true)
            {
                byte[] data = receivingClient.Receive(ref endPoint);
                string message = Encoding.ASCII.GetString(data);
                Invoke(messageDelegate, message);
            }
        }

        private void MessageReceived(string message)
        {
            txtHistoryMessage.Text += message + "\n";
        }

    }
}
