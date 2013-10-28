using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using httpHelper;
using rfidCheck;
using System.Diagnostics;
using System.Speech.Synthesis;
using Ledsoft.ls.dll;
using System.Threading;
using WebSocketSharp;

namespace Demo
{
    public partial class frmMain : Form
    {
        string wssUrl = "";
        string LED_IP = "192.168.0.98";

        SpeechSynthesizer _synth = new SpeechSynthesizer();
        List<command> listToSpeak = new List<command>();//存放待播放的文本
        //List<string> listToSpeak = new List<string>();//存放待播放的文本
        WebSocket ws = null;
        bool bRunning = false;//是否正在运行
        string lastUpdateTimeStamp = string.Empty;


        public frmMain()
        {
            InitializeComponent();

            wssUrl = sysConfig.wssUrl;

            _synth.SpeakCompleted += (sender, e) =>
            {
                startSpeakProcess();
            };
            _synth.SelectVoice("Microsoft Lili");
            //_synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 1, new System.Globalization.CultureInfo("zh-CHS"));
            _synth.Rate = -2;
        }
        void startSpeakProcess()
        {
            if (listToSpeak.Count > 0 && _synth.State != SynthesizerState.Speaking)
            {
                command c = listToSpeak[listToSpeak.Count - 1];
                listToSpeak = listToSpeak.GetRange(0, listToSpeak.Count - 1);
                //listToSpeak.RemoveAt(listToSpeak.Count - 1);
                _synth.SpeakAsync(c.para);
            }
        }
        //void _synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        //{
        //    if (__dtCommandInfo.Rows.Count > 0 && _synth.State != SynthesizerState.Speaking)
        //    {
        //        //CommandInfo c = listToSpeak[0];
        //        //string str = c.info;
        //        Manualstate.WaitOne();
        //        Manualstate.Reset();

        //        DataRow[] rows = __dtCommandInfo.Select(string.Format("state = 'new'"), "time asc");
        //        if (rows.Length > 0)//说明
        //        {
        //            string str = rows[0]["info"].ToString();
        //            rows[0]["state"] = "read";
        //            if (__dtCommandInfo.Rows.Count > 100)
        //            {
        //                __dtCommandInfo.Rows.RemoveAt(100);
        //            }
        //            _synth.SpeakAsync(str);
        //        }
        //        Manualstate.Set();
        //    }
        //}


        void setup_to_get_command(object sender, EventArgs e)
        {
            if (this.bRunning == false)
            {
                return;
            }
            //string url = sysConfig.getRestUrl();
            //string url = sysConfig.getCommandRestUrl();
            //string url = sysConfig.getCommandRestUrlComet();

            //CommandInfo li = new CommandInfo(string.Empty, string.Empty, this.lastUpdateTimeStamp, string.Empty);
            //LedInfo li = new LedInfo(string.Empty, this.lastUpdateTimeStamp, string.Empty);
            //Debug.WriteLine("__timer_Tick_getCommand -> 时间标识：" + this.lastUpdateTimeStamp);
            //string jsonString = fastJSON.JSON.Instance.ToJSON(li);
            //this.appendLog(jsonString);
            //HttpWebConnect httpHelper = new HttpWebConnect();
            //httpHelper.RequestCompleted += new deleGetRequestObject(__httpHelper_RequestCompleted_getCommand);
            //httpHelper.TryPostData(url, jsonString);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //启动控制器
            //启动以后，控制器会不断的访问网络以获取最新的命令
            if (this.bRunning)
            {
                //this.__timer.Enabled = false;
                this.btnStart.Text = "运行";
                this.bRunning = false;
                this.matrixCircularProgressControl1.Stop();
                closeWebsocket();
            }
            else
            {
                this.bRunning = true;
                this.btnStart.Text = "停止";
                //重置状态
                //this.lastUpdateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                //this.__timer.Enabled = true;
                this.matrixCircularProgressControl1.Start();

                //this.setup_to_get_command(null, null);


                //建立websocket链接，并注册推送服务类型
                setupWebsocket(wssUrl);
            }
        }
        void closeWebsocket()
        {
            try
            {
                if (ws != null)
                {
                    ws.Close();
                    ws = null;
                }
            }
            catch
            {
            }
        }
        void setupWebsocket(string url)
        {
            closeWebsocket();
            try
            {
                ws = new WebSocket(url);
                ws.OnOpen += (sender, e) =>
                {
                    command regCmd = new command("led", "");
                    regCmd.msgType = "reg";
                    string strcmd = Newtonsoft.Json.JsonConvert.SerializeObject(regCmd);
                    ws.Send(strcmd);
                    regCmd = new command("tts", "");
                    regCmd.msgType = "reg";
                    strcmd = Newtonsoft.Json.JsonConvert.SerializeObject(regCmd);
                    ws.Send(strcmd);
                };

                ws.OnMessage += (sender, e) =>
                {
                    Debug.WriteLine(e.Data);
                    object o = Newtonsoft.Json.JsonConvert.DeserializeObject<statusMsg>(e.Data);
                    statusMsg status = (statusMsg)o;
                    if (status.status == "success" && status.cmds != null)
                    {
                        List<command> list = (List<command>)Newtonsoft.Json.JsonConvert.DeserializeObject<List<command>>(status.cmds);
                        foreach (command _cmd in list)
                        {
                            Debug.WriteLine(string.Format("name: {0}    para: {1}", _cmd.name, _cmd.para));

                            switch (_cmd.name)
                            {
                                case "led":
                                    //设置LED字幕
                                    if (this.ckbled.Checked)
                                    {
                                        try
                                        {
                                            appendLog("获取到LED屏内容：" + _cmd.para + "  " + _cmd.timeStamp);
                                            this.sendMsgToLED(this.LED_IP, _cmd.para);
                                        }
                                        catch (System.Exception ex)
                                        {
                                            appendLog("发送到LED屏时发生异常：" + ex.Message);
                                        }
                                    }
                                    break;
                                case "tts":
                                    appendLog("获取到tts内容：" + _cmd.para + "  " + _cmd.timeStamp);
                                    listToSpeak.Insert(0, _cmd);
                                    //启动语音播放
                                    startSpeakProcess();
                                    break;
                            }
                        }
                    }
                };

                ws.OnError += (sender, e) =>
                {
                    Debug.WriteLine(e.Message);
                    setupWebsocket(wssUrl);
                };

                ws.OnClose += (sender, e) =>
                {
                    Debug.WriteLine(e.Data);
                };

                ws.Connect();
            }
            catch (Exception e)
            {

            }
        }
        private void 设置TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmSysSetting frm = new frmSysSetting();
            frm.ShowDialog();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            this.sendMsgToLED("192.168.0.98", "98aaaaaaaaaaaaaaaaaa");
        }

        #region helper function

        //void dispatchCommand(object oc)
        //{
        //    string cmds = (string)oc;
        //    if (cmds == "timeout")
        //    {
        //        Debug.WriteLine(
        //            string.Format("frmMain.__httpHelper_RequestCompleted_getCommand  ->  = {0}"
        //            , cmds));
        //        this.setup_to_get_command(null, null);
        //        return;
        //    }
        //    if (this.bRunning == false)
        //    {
        //        return;
        //    }
        //    Debug.WriteLine(
        //        string.Format("frmMain.__httpHelper_RequestCompleted_getCommand  ->  = {0}"
        //        , cmds));
        //    int index = cmds.IndexOf("[");
        //    if (index >= 0)
        //    {
        //        cmds = cmds.Substring(index);
        //    }
        //    else
        //    {
        //        return;
        //    }
        //    //this.appendLog(cmds);
        //    // return;
        //    object olist = fastJSON.JSON.Instance.ToObjectList(cmds, typeof(List<command>), typeof(command));
        //    foreach (command c in (List<command>)olist)
        //    {
        //        if (string.Compare(this.lastUpdateTimeStamp, c.timeStamp, true) < 0)//如果命令的时间较晚
        //        {
        //            this.lastUpdateTimeStamp = c.timeStamp;
        //        }
        //        switch (c.name)
        //        {
        //            case "tts":
        //                Debug.WriteLine(
        //                                string.Format("frmMain.获取到tts发布信息，内容为  -> tts = {0}   {1}"
        //                                                , c.para, c.timeStamp));

        //                this.appendLog(string.Format("获取到tts发布信息，内容为 {0} {1}", c.para, c.timeStamp));
        //                if (this.ckbtts.Checked)
        //                {
        //                    bool bAlreadyExist = false;
        //                    Manualstate.WaitOne();
        //                    Manualstate.Reset();

        //                    string strSelect = string.Format("type = '{0}' and info = '{1}' and time = '{2}'",
        //                                    c.name, c.para, c.timeStamp);
        //                    DataRow[] rows = __dtCommandInfo.Select(strSelect);
        //                    if (rows.Length > 0)//说明
        //                    {
        //                        bAlreadyExist = true;
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine("添加新行到表中 " + strSelect);
        //                        //__dtCommandInfo.Rows.Add(new object[] {
        //                        //    c.name,c.para,c.ledIP,c.timeStamp,"new"
        //                        //});
        //                    }

        //                    Manualstate.Set();

        //                    if (bAlreadyExist == false)
        //                    {
        //                        //_synth_SpeakCompleted(null, null);
        //                    }
        //                }

        //                break;
        //            case "led":
        //                //发现新的led信息，需要发送到相应的led屏上
        //                //Debug.WriteLine(
        //                //    string.Format("frmMain.__httpHelper_RequestCompleted_getCommand  -> led = {0}    {1}  {2}"
        //                //    , c.ledIP, c.para, c.timeStamp));
        //                //c.ledIP = "172.16.13.99";
        //                //this.appendLog(string.Format("获取到led发布信息，ip为 {0}   内容为 {1}  {2}", c.ledIP, c.para, c.timeStamp));
        //                if (this.ckbled.Checked)
        //                {
        //                    try
        //                    {
        //                        //this.sendMsgToLED(c.ledIP, c.para);
        //                    }
        //                    catch (System.Exception ex)
        //                    {
        //                        appendLog("发送到LED屏时发生异常：" + ex.Message);
        //                    }
        //                }

        //                break;

        //        }

        //    }
        //    this.setup_to_get_command(null, null);
        //}


        void sendMsgToLED(string IP, string Text)
        {
            try
            {

                Debug.WriteLine(
                    string.Format("frmMain.sendMsgToLED  -> ip = {0}   msg = {1}"
                    , IP, Text));
                int result;


                //LEDDLL.SetTransMode(3, 3);"192.168.1.99"
                //LEDDLL.SetSerialPortPara(2, 1, 115200);
                string ip = IP;
                LEDDLL.SetNetworkPara(1, ip.ToCharArray());

                //   result = LEDDLL.SendScreenPara(2, 1, 64, 32);
                //result = LEDDLL.SendScreenPara(2, 1, 96, 16);
                LEDDLL.StartSend();  //初始化数据结构

                LEDDLL.AddControl(1, 1);        //参数依次为：屏号，单双色
                LEDDLL.AddProgram(1, 1, 0);     //参数依次为：屏号，节目号，节目播放时间

                LEDDLL.AddFileArea(1, 1, 1, 0, 0, 224, 16);
                LEDDLL.AddFileString(1, 1, 1, 1, Text, "宋体", 12, 255, false, false, false, 1, 64, 32, 32, 255, 100, 2, 1);
                LEDDLL.SendControl(1, 1, IntPtr.Zero);
                //string temp;
                //switch (result)
                //{
                //    case 1: temp = "发送成功"; break;
                //    case 2: temp = "通讯失败"; break;
                //    case 3: temp = "发送过程中出错"; break;
                //    default: temp = ""; break;
                //}
                //    MessageBox.Show(temp);

            }
            catch (Exception ex)
            {
                appendLog("发送到LED屏时发生异常：" + ex.Message);

            }

        }

        void appendLog(string appendedStr)
        {
            Action<string> act = (log) =>
            {
                if (this.txtLog.Text != null && this.txtLog.Text.Length > 4096)
                {
                    this.txtLog.Text = string.Empty;
                }
                this.txtLog.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + log + "\r\n" + this.txtLog.Text;// +" " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            };
            this.Invoke(act, appendedStr);
        }
        #endregion
    }
}
