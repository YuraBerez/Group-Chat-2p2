using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Peer_To_Peer_Chat
{
    public partial class UserNameForm : Form
    {
        public UserNameForm()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(LoginForm_FormClosing);
            btnCreate.Click += new EventHandler(btnCreate_Click);
        }

        string userName = "";

        public string UserName
        {
            get { return userName; }
        }

        delegate void AddMessage(string message);

        void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            userName = "";
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            userName = txtUserName.Text.Trim();

            if (string.IsNullOrEmpty(userName))
            {
                MessageBox.Show("Please select a user name up to 32 characters.");
                return;
            }

            this.FormClosing -= LoginForm_FormClosing;
            this.Close();
        }

        private void UserNameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            userName = "";
        }
    }
}
