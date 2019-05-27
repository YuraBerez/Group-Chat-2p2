using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace GroupChat
{
    public class Win32API
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public struct My_lParam
        {
            public Socket t;
            public string s;
        }

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(
            IntPtr hWnd,        
            int Msg,            
            int wParam,         
            int lParam          
        );

        
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(
            IntPtr hWnd,        
            int Msg,            
            int wParam,         
            ref My_lParam lParam
        );

        
        [DllImport("User32.dll", EntryPoint = "PostMessage")]
        public static extern int PostMessage(
            IntPtr hWnd,        
            int Msg,            
            int wParam,         
            int lParam          
        );
    }
}
