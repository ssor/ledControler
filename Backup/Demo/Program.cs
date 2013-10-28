﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using nsConfigDB;
using rfidCheck;
using KeyChec;

namespace Demo
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
            initialConfig();
            //PayCheck.start_loop_check("yingkou2012");
            Application.Run(new frmMain());
        }

        static void initialConfig()
        {
            object o = null;
            o = ConfigDB.getConfig("ip");
            if (o != null)
            {
                sysConfig.restIp = (string)o;
            }
            o = ConfigDB.getConfig("port");
            if (o != null)
            {
                sysConfig.tcp_port = int.Parse((string)o);
            }
        }
    }
}
