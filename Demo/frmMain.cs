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

namespace Demo
{
    public partial class frmMain : Form
    {
        public ManualResetEvent Manualstate = new ManualResetEvent(true);
        SpeechSynthesizer _synth = new SpeechSynthesizer();
        List<CommandInfo> listToSpeak = new List<CommandInfo>();//存放待播放的文本
        //List<string> listToSpeak = new List<string>();//存放待播放的文本
        DataTable __dtCommandInfo = new DataTable();

        public frmMain()
        {
            InitializeComponent();

            //__timer = new System.Windows.Forms.Timer();
            //__timer.Interval = 300;
            //__timer.Tick += new EventHandler(__timer_Tick_getCommand);


            //__httpHelper.RequestCompleted += new deleGetRequestObject(__httpHelper_RequestCompleted_getCommand);

            _synth.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(_synth_SpeakCompleted);
            _synth.SelectVoice("Microsoft Lili");
            //_synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 1, new System.Globalization.CultureInfo("zh-CHS"));
            _synth.Rate = -2;

            __dtCommandInfo.Columns.Add("type", typeof(string));
            __dtCommandInfo.Columns.Add("info", typeof(string));
            __dtCommandInfo.Columns.Add("ip", typeof(string));
            __dtCommandInfo.Columns.Add("time", typeof(string));
            __dtCommandInfo.Columns.Add("state", typeof(string));

        }

        void _synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (__dtCommandInfo.Rows.Count > 0 && _synth.State != SynthesizerState.Speaking)
            {
                //CommandInfo c = listToSpeak[0];
                //string str = c.info;
                Manualstate.WaitOne();
                Manualstate.Reset();

                DataRow[] rows = __dtCommandInfo.Select(string.Format("state = 'new'"), "time asc");
                if (rows.Length > 0)//说明
                {
                    string str = rows[0]["info"].ToString();
                    rows[0]["state"] = "read";
                    if (__dtCommandInfo.Rows.Count > 100)
                    {
                        __dtCommandInfo.Rows.RemoveAt(100);
                    }
                    _synth.SpeakAsync(str);
                }
                Manualstate.Set();
            }
        }

        bool bRunning = false;//是否正在运行
        string lastUpdateTimeStamp = string.Empty;
        void setup_to_get_command(object sender, EventArgs e)
        {
            if (this.bRunning == false)
            {
                return;
            }
            //string url = sysConfig.getRestUrl();
            string url = sysConfig.getCommandRestUrl();
            //string url = sysConfig.getCommandRestUrlComet();

            //CommandInfo li = new CommandInfo(string.Empty, string.Empty, this.lastUpdateTimeStamp, string.Empty);
            LedInfo li = new LedInfo(string.Empty, this.lastUpdateTimeStamp, string.Empty);
            Debug.WriteLine("__timer_Tick_getCommand -> 时间标识：" + this.lastUpdateTimeStamp);
            string jsonString = fastJSON.JSON.Instance.ToJSON(li);
            //this.appendLog(jsonString);
            HttpWebConnect httpHelper = new HttpWebConnect();
            httpHelper.AcceptException += new deleRaiseException(httpHelper_AcceptException);
            httpHelper.RequestCompleted += new deleGetRequestObject(__httpHelper_RequestCompleted_getCommand);
            httpHelper.TryPostData(url, jsonString);
        }

        void httpHelper_AcceptException(HttpException httpException)
        {
            this.setup_to_get_command(null, null);
        }
        //通过网络读取到命令后，对命令进行解析
        void __httpHelper_RequestCompleted_getCommand(object o)
        {
            deleControlInvoke dele = delegate(object oc)
            {
                string cmds = (string)oc;
                if (cmds == "timeout")
                {
                    Debug.WriteLine(
                        string.Format("frmMain.__httpHelper_RequestCompleted_getCommand  ->  = {0}"
                        , cmds));
                    this.setup_to_get_command(null, null);
                    return;
                }
                if (this.bRunning == false)
                {
                    return;
                }
                Debug.WriteLine(
                    string.Format("frmMain.__httpHelper_RequestCompleted_getCommand  ->  = {0}"
                    , cmds));
                int index = cmds.IndexOf("[");
                if (index >= 0)
                {
                    cmds = cmds.Substring(index);
                }
                else
                {
                    return;
                }
                //this.appendLog(cmds);
                // return;
                object olist = fastJSON.JSON.Instance.ToObjectList(cmds, typeof(List<CommandInfo>), typeof(CommandInfo));
                foreach (CommandInfo c in (List<CommandInfo>)olist)
                {
                    if (string.Compare(this.lastUpdateTimeStamp, c.timeStamp, true) < 0)//如果命令的时间较晚
                    {
                        this.lastUpdateTimeStamp = c.timeStamp;
                    }
                    switch (c.name)
                    {
                        case "tts":
                            Debug.WriteLine(
                                            string.Format("frmMain.获取到tts发布信息，内容为  -> tts = {0}   {1}"
                                                            , c.info, c.timeStamp));

                            this.appendLog(string.Format("获取到tts发布信息，内容为 {0} {1}", c.info, c.timeStamp));
                            if (this.ckbtts.Checked)
                            {
                                bool bAlreadyExist = false;
                                Manualstate.WaitOne();
                                Manualstate.Reset();

                                string strSelect = string.Format("type = '{0}' and info = '{1}' and time = '{2}'",
                                                c.name, c.info, c.timeStamp);
                                DataRow[] rows = __dtCommandInfo.Select(strSelect);
                                if (rows.Length > 0)//说明
                                {
                                    bAlreadyExist = true;
                                }
                                else
                                {
                                    Debug.WriteLine("添加新行到表中 " + strSelect);
                                    __dtCommandInfo.Rows.Add(new object[] {
                                    c.name,c.info,c.ledIP,c.timeStamp,"new"
                                });
                                }

                                Manualstate.Set();

                                if (bAlreadyExist == false)
                                {
                                    _synth_SpeakCompleted(null, null);
                                }
                            }

                            break;
                        case "led":
                            //发现新的led信息，需要发送到相应的led屏上
                            Debug.WriteLine(
                                string.Format("frmMain.__httpHelper_RequestCompleted_getCommand  -> led = {0}    {1}  {2}"
                                , c.ledIP, c.info, c.timeStamp));
                            c.ledIP = "172.16.13.99";
                            this.appendLog(string.Format("获取到led发布信息，ip为 {0}   内容为 {1}  {2}", c.ledIP, c.info, c.timeStamp));
                            if (this.ckbled.Checked)
                            {
                                try
                                {
                                    this.sendMsgToLED(c.ledIP, c.info);
                                }
                                catch (System.Exception ex)
                                {
                                    appendLog("发送到LED屏时发生异常：" + ex.Message);
                                }
                            }

                            break;

                    }

                }
                this.setup_to_get_command(null, null);
            };
            this.Invoke(dele, o);
        }

        void sendMsgToLED(string IP, string Text)
        {
            //if (Text.Length > 5)
            //{
            //    Text = Text.Substring(0, 3);
            //}
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

        void appendLog(string log)
        {
            if (this.txtLog.Text != null && this.txtLog.Text.Length > 4096)
            {
                this.txtLog.Text = string.Empty;
            }
            this.txtLog.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + log + "\r\n" + this.txtLog.Text;// +" " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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
            }
            else
            {
                this.bRunning = true;
                this.btnStart.Text = "停止";
                //重置状态
                this.lastUpdateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                //this.__timer.Enabled = true;
                this.matrixCircularProgressControl1.Start();

                this.setup_to_get_command(null, null);

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
    }
}
