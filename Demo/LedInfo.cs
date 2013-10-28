using System;
using System.Collections.Generic;
using System.Text;

namespace Demo
{
    public delegate void deleControlInvoke(object o);

    public class command
    {
        public string name = string.Empty;
        public string para = string.Empty;
        public string timeStamp = string.Empty;
        //public string ledIP = string.Empty;
        //public string status = string.Empty;
        public string msgType;

        public command()
        {

        }
        public command(string _commandName, string _para)
        {
            this.name = _commandName;
            this.para = _para;
            //this.timeStamp = _time;
            //this.ledIP = _ip;
        }
    }
    public class statusMsg
    {
        public string status;
        public string cmds;
        public statusMsg() { }
        public statusMsg(string _status)
        {
            this.status = _status;
        }
    }
    public class LedInfo
    {
        public string info = string.Empty;
        public string startTime = string.Empty;
        public string ledIP = string.Empty;
        public string state = string.Empty;

        public LedInfo()
        {

        }
        public LedInfo(string _info, string _time, string _ip)
        {
            this.info = _info;
            this.startTime = _time;
            this.ledIP = _ip;
        }
    }
}
