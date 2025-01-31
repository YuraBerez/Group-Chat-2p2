﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace GroupChat
{
    enum MessageType
    {
        WM_USER = 0x0400,
        Broadcast, BroadcastReply, ChatMessage, FileMessage, FileRequest,
        ClearUsers, UpdateProgressBar, FileReceiveSuccess, FileReceiveError, ShowWindow
    }

    public partial class ChatRoom : Form
    {

        public static readonly int FILE_EXISTS_MINUTE = 10;
        public static readonly int UDP_DATA_MAX_SIZE = 1024;
        public static readonly int TCP_DATA_MAX_SIZE = 10 * 1024 * 1024;
        public static readonly int TCP_LISTEN_MAX_SIZE = 1024;
        public static readonly int LISTEN_PORT = 2048;
        public static readonly int FILE_PORT = 2049;
        public static readonly char SEPARATOR = '`';
        public static readonly string WINDOWS_NAME = "Чат групи LAN";
        public static readonly string COMPUTER_NAME = Dns.GetHostName();
        public static readonly string IP_ADDRESS = GetInternalIP();
        public static readonly string DOWNLOAD_DIR = @".\Downloads";

        //获取内网IP
        public static string GetInternalIP()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private static ChatRoom chatRoom;
        private static readonly object locker = new object();

        private static Socket sendFileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static IPEndPoint tcpPoint = new IPEndPoint(IPAddress.Parse(IP_ADDRESS), FILE_PORT);

        public static Socket fileAcceptSocket()
        {
            return sendFileSocket.Accept();
        }

        private ChatRoom()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            SendFileClass.Initialize(sendFileSocket, tcpPoint);
        }

        public static Form GetRoom()
        {

            if (chatRoom == null)
            {

                lock (locker)
                {

                    IntPtr handle = Win32API.FindWindow(null, WINDOWS_NAME);
                    if (handle != IntPtr.Zero)
                    {
                        Win32API.SendMessage(handle, (int)MessageType.ShowWindow, 0, 0);
                        Process.GetCurrentProcess().Kill();
                    }
                    if (chatRoom == null)
                    {
                        chatRoom = new ChatRoom();
                    }
                }
            }
            return chatRoom;
        }

        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch ((MessageType)m.Msg)
            {
                case MessageType.Broadcast:
                    {
                        Win32API.My_lParam ml = new Win32API.My_lParam();
                        Type t = ml.GetType();
                        ml = (Win32API.My_lParam)m.GetLParam(t);
                        updateUserList(ml.s);
                    }
                    break;

                case MessageType.BroadcastReply:
                    {
                        Win32API.My_lParam ml = new Win32API.My_lParam();
                        Type t = ml.GetType();
                        ml = (Win32API.My_lParam)m.GetLParam(t);
                        updateUserList(ml.s);
                    }
                    break;

                case MessageType.ChatMessage:
                    {
                        Win32API.My_lParam ml = new Win32API.My_lParam();
                        Type t = ml.GetType();
                        ml = (Win32API.My_lParam)m.GetLParam(t);
                        string[] msg = ml.s.Split(SEPARATOR);

                        string owner = msg[0] + "(" + msg[1] + ")";
                        string updateTime = DateTime.Now + "";
                        string data = msg[2];

                        string str = owner;
                        str += updateTime;

                        int headLength = str.Length;

                        str += Environment.NewLine;
                        str += data;
                        str += Environment.NewLine;
                        str += Environment.NewLine;

                        int newRecordStart = record_chat.TextLength;

                        record_chat.AppendText(str);

                        record_chat.Select(newRecordStart, headLength);
                        record_chat.SelectionColor = Color.Blue;

                        record_chat.Select(record_chat.TextLength, 0);
                        record_chat.Focus();

                        message_chat.Focus();
                    }
                    break;

                case MessageType.FileMessage:
                    {
                        Win32API.My_lParam ml = new Win32API.My_lParam();
                        Type t = ml.GetType();
                        ml = (Win32API.My_lParam)m.GetLParam(t);
                        string[] msg = ml.s.Split(SEPARATOR);

                        string owner = msg[0] + "(" + msg[1] + ")";
                        string updateTime = DateTime.Now + "";
                        string filePath = msg[2];
                        string fileSize = msg[3];

                        string str = owner;
                        str += updateTime;

                        int headLength = str.Length;

                        str += Environment.NewLine;
                        str += "Надіслати файл：" + Path.GetFileName(filePath);
                        str += "；Розмір (байти)：" + fileSize;
                        str += Environment.NewLine;
                        str += Environment.NewLine;

                        int newRecordStart = record_chat.TextLength;
                        
                        record_chat.AppendText(str);

                        record_chat.Select(newRecordStart, headLength);
                        record_chat.SelectionColor = Color.Blue;

                        record_chat.Select(record_chat.TextLength,0);
                        record_chat.Focus();

                        message_chat.Focus();

                        updateFileList(filePath, updateTime, fileSize, msg[1]);
                    }
                    break;
                case MessageType.FileRequest:
                    {
                        Win32API.My_lParam ml = new Win32API.My_lParam();
                        Type t = ml.GetType();
                        ml = (Win32API.My_lParam)m.GetLParam(t);

                        string filePath = ml.s;
                        Socket sendFileAccept = ml.t;

                        byte[] reply = new byte[TCP_DATA_MAX_SIZE];

                        if (File.Exists(filePath))
                        {
                            reply = Encoding.Default.GetBytes("Дійсний файл");
                            sendFileAccept.Send(reply, SocketFlags.None);

                            Thread sendFileThread = new Thread(new ParameterizedThreadStart(SendFileClass.SendFile));
                            sendFileThread.IsBackground = true;
                            sendFileThread.Start(ml);
                        }
                        else
                        {
                            reply = Encoding.Default.GetBytes("Недійсний файл");
                            sendFileAccept.Send(reply, SocketFlags.None);
                            sendFileAccept.Close();
                        }
                    }
                    break;
                case MessageType.ClearUsers:
                    {
                        list_user.Items.Clear();
                    }
                    break;
                case MessageType.ShowWindow:
                    {
                        this.Activate();
                    }
                    break;

                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }



        private void updateUserList(string msg)
        {
            string[] user = msg.Split(SEPARATOR);

            ListViewItem lviComputerName = new ListViewItem();
            ListViewItem.ListViewSubItem lviIP = new ListViewItem.ListViewSubItem();

            lviComputerName.Text = user[0];
            lviIP.Text = user[1];

            lviComputerName.SubItems.Add(lviIP);

            bool flag = true;
            for (int i = 0; i < this.list_user.Items.Count; i++)
            {
                if (lviIP.Text == this.list_user.Items[i].SubItems[1].Text)
                {
                    flag = false;
                    break;
                }
            }

            if (flag)
            {
                this.list_user.Items.Add(lviComputerName);
            }

            lbUserCount.Text = "Кількість користувачів онлайн：  " + this.list_user.Items.Count;
        }

        private void updateFileList(string filePath, string updateTime, string fileSize, string owner)
        {

            ListViewItem lviFilePath = new ListViewItem();
            ListViewItem.ListViewSubItem lviUpdateTime = new ListViewItem.ListViewSubItem();
            ListViewItem.ListViewSubItem lviFileSize = new ListViewItem.ListViewSubItem();
            ListViewItem.ListViewSubItem lviOwner = new ListViewItem.ListViewSubItem();

            lviFilePath.Text = filePath;
            lviUpdateTime.Text = updateTime;
            lviFileSize.Text = fileSize;
            lviOwner.Text = owner;

            lviFilePath.SubItems.Add(lviUpdateTime);
            lviFilePath.SubItems.Add(lviFileSize);
            lviFilePath.SubItems.Add(lviOwner);

            bool flag = true;
            for (int i = 0; i < this.list_user.Items.Count; i++)
            {
                if (lviFilePath.Text == this.list_user.Items[i].SubItems[0].Text && lviOwner.Text == this.list_user.Items[i].SubItems[3].Text)
                {
                    flag = false;
                    break;
                }
            }

            if (flag)
            {
                this.list_file.Items.Add(lviFilePath);
            }

        }

        private void btn_quit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ChatRoom_Load(object sender, EventArgs e)
        {
            this.Text = WINDOWS_NAME;
            ControlMessage();
            UpdateFriends();
        }

        private void ControlMessage()
        {
            Thread listenThread = new Thread(new ThreadStart(ListenClass.StartListen));
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        private void UpdateFriends()
        {
            Thread listenThread = new Thread(new ThreadStart(Broadcast.IntervalBroadcast));
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            string data = message_chat.Text.Trim();
            if (data == String.Empty)
            {
                MessageBox.Show("Не може бути порожнім!");
            }
            else
            {
                string info = string.Join(SEPARATOR + "", new string[] { MessageType.ChatMessage + "", COMPUTER_NAME, IP_ADDRESS, data });
                UdpSendMessage.SendToAll(info);
            }
            message_chat.Clear();
        }

        private void btn_upload_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog Dlg = new OpenFileDialog();
            FileInfo FI;
            Dlg.Filter = "Всі файли(*.*)|*.*";
            Dlg.CheckFileExists = true;

            if (Dlg.ShowDialog() == DialogResult.OK)
            {
                FI = new FileInfo(Dlg.FileName);
                if (FI.Length == 0)
                {
                    MessageBox.Show("Не вдається завантажити порожні файли");
                }
                else
                {
                    string info = string.Join(SEPARATOR + "", new string[] { MessageType.FileMessage + "", COMPUTER_NAME, IP_ADDRESS, Dlg.FileName, FI.Length + "" });
                    UdpSendMessage.SendToAll(info);
                }
            }
            else
            {
                MessageBox.Show("Скасувати завантаження");
            }
        }

        private void list_file_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (list_file.SelectedItems.Count == 1)
            {
                DateTime now = DateTime.Now;
                DateTime fileCreateTime = Convert.ToDateTime(list_file.SelectedItems[0].SubItems[1].Text);
                if ((now - fileCreateTime).Minutes >= FILE_EXISTS_MINUTE)
                {
                    MessageBox.Show("Термін дії файлу закінчився");
                    list_file.Items.Remove(list_file.SelectedItems[0]);
                    return;
                }
                DialogResult result = MessageBox.Show("Визначте приймальний файл（" + list_file.SelectedItems[0].Text + "?", "tips", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    string ipAddress = list_file.SelectedItems[0].SubItems[3].Text;
                    string filePath = list_file.SelectedItems[0].SubItems[0].Text;
                    string fileSize = list_file.SelectedItems[0].SubItems[2].Text;

                    Transmission receiveForm = new Transmission(ipAddress, filePath, fileSize);
                    receiveForm.Show();
                }
                else
                {
                    MessageBox.Show("Скасування отримання");
                }
            }
            else
            {
                MessageBox.Show("Необхідно вибрати тільки один файл");
            }
        }



    }
}
