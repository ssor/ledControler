using System;
using System.Collections.Generic;
using System.Text;

namespace rfidCheck
{
    public static class sysConfig
    {

        //public static string comportName="com1";

        //public static string baudRate = "57600";

        public static int tcp_port = 9002;
        public static string restIp = "127.0.0.1";
        //public static string restIp = "192.168.1.100";
        public static string wssUrl;

        public static string restUrl =
            "http://" + restIp + ":" + tcp_port + "/index.php/LED/led/getLedInfos";
        public static string getRestUrlComet()
        {
            return "http://" + restIp + ":" + tcp_port + "/index.php/LED/led/get_led_info_comet";
        }
        public static string getRestUrl()
        {
            return "http://" + restIp + ":" + tcp_port + "/index.php/LED/led/getLedInfos";
        }
        public static string getCommandRestUrl()
        {
            return "http://" + restIp + ":" + tcp_port + "/index.php/LED/CommandInfo/getCommandInfos";
        }
        public static string getCommandRestUrlComet()
        {
            return "http://" + restIp + ":" + tcp_port + "/index.php/LED/CommandInfo/get_command_info_comet";
        }
        public static string getWssUrl()
        {
            return "ws://192.168.57.105:3003";
        }
    }
}
