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

        const int port = 5500;
        const string broadcastAddress = "127.0.0.1";

        public ChatForm()
        {
            using (UserNameForm loginForm = new UserNameForm())
            {
                try
                {
                    loginForm.ShowDialog();

                    if (loginForm.UserName == "")
                        this.Close();
                    else
                    {
                        userName = loginForm.UserName;
                        loginForm.Dispose();
                        this.Show();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            InitializeComponent();
            this.Text += " (" + userName + ")";
            this.Load += new EventHandler(ChatForm_Load);
            btnSend.Click += new EventHandler(btnSend_Click);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            { 
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), Convert.ToInt32(port));

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipPoint);
                byte[] data = Encoding.Unicode.GetBytes(txtMessage.Text);
                socket.Send(data);

                socket.Shutdown(SocketShutdown.Both);
                data = new byte[256]; 
                StringBuilder builder = new StringBuilder();
                int bytes = 0; 

                do
                {
                    bytes = socket.Receive(data, data.Length, 0);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);
                txtHistoryMessage.Text += ">>>  \n";
                socket.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            txtHistoryMessage.Text += (userName + ": " + txtMessage.Text) + "\n";
            txtMessage.Text = "";
            txtMessage.Focus();
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            txtMessage.Focus();

            InitializeSender();
        }

        private void InitializeSender()
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), port);

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(ipPoint);

                listenSocket.Listen(10);

                while (true)
                {
                    Socket handler = listenSocket.Accept();

                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];

                    do
                    {
                        bytes = handler.Receive(data);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (handler.Available > 0);
                    txtHistoryMessage.Text += (userName + ": " + builder.ToString()) + "\n";
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
