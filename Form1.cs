using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        int conf_threadnum = 30;
        int conf_waittime = 2000;
        string conf_exefile = @"proxy_yoke";

        Process p;
        ProcessStartInfo s;
        Thread thread;
        JsonConfig configX;
        int currect_connect = -1;
        string init_server = "";
        string rc_path = @"rc.txt";
        string config_path = @"gui-config.json";

        private void Form1_Load(object sender, EventArgs e)
        {

            //this.notifyIcon1.ShowBalloonTip(500, "yoke_ss", "I am here...", ToolTipIcon.Info);

            s = new ProcessStartInfo();
            s.RedirectStandardInput = true;
            s.RedirectStandardOutput = true;
            s.CreateNoWindow = true;
            s.UseShellExecute = false;
            s.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                getNow();
            }
            catch (Exception e2)
            {
                SetText(e2.StackTrace);
            }

            try
            {
                loadConfig();
                startProxy();
            }
            catch (Exception e2)
            {
                SetText(e2.StackTrace);
            }
        }

        private void getNow()
        {
            StreamReader sr = new StreamReader(rc_path, Encoding.Default);
            String line;
            StringBuilder sb = new StringBuilder();
            while ((line = sr.ReadLine()) != null)
            {
                sb.AppendLine(line);
            }
            Match match = Regex.Match(sb.ToString(), "@(.*):\\d+");
            init_server = match.Groups[1].Value;
            sr.Close();
        }

        private void startProxy()
        {
            killProxy();
            thread = new Thread(new ThreadStart(startThread));
            thread.IsBackground = true;
            thread.Start();
        }

        private void killProxy()
        {
            try
            {
                Process[] proc = Process.GetProcessesByName(conf_exefile);
                for (int i = 0; i < proc.Length; i++)
                {
                    proc[i].Kill();
                }
            }
            catch (Exception e2)
            {
                SetText(e2.StackTrace);
            }
        }

        private void startThread()
        {
            if (currect_connect != -1)
            {
                configX.configs[currect_connect].bbb = "已连接";
                dataGridView1.UpdateCellValue(5, currect_connect);
            }
            try
            {
                s.FileName = conf_exefile;
                p = Process.Start(s);
                while (!p.StandardOutput.EndOfStream)
                {
                    string str = p.StandardOutput.ReadLine();
                    SetText(str);
                }
                p.WaitForExit();
                p.Close();
            }
            catch (InvalidOperationException e1)
            {
                SetText(e1.StackTrace);

            }
            catch (Exception e2)
            {
                Console.WriteLine(e2);
                Application.Exit();
                return;
            }
        }
        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.richTextBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.richTextBox1.AppendText(text);
                this.richTextBox1.AppendText("\n");
            }
        }

        private void loadConfig()
        {
            try
            {
                using (StreamReader reader = File.OpenText(config_path))
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    configX = (JsonConfig)serializer.Deserialize(reader.ReadToEnd(), typeof(JsonConfig));
                    this.dataGridView1.DataSource = configX.configs;

                    for (int i = 0; i < configX.configs.Count; i++)
                    {
                        JsonConfigItem item = configX.configs[i];
                        if (item.server.Equals(init_server))
                        {
                            currect_connect = i;
                            item.bbb = "已连接";
                        }
                    }
                }
            }
            catch (Exception e2)
            {
                Console.WriteLine(e2);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Show();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮 
            if (WindowState == FormWindowState.Minimized)
            {
                //隐藏任务栏区图标 
                this.ShowInTaskbar = false;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.notifyIcon1.Visible = false;
            closeProxy();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            loadConfig();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            startPing();
        }

        private void startPing()
        {
            ThreadPool.SetMaxThreads(conf_threadnum, conf_threadnum);

            for (int i = 0; i < configX.configs.Count; i++)
            {
                ThreadPool.QueueUserWorkItem(_pingThread, i);
            }
        }

        public static string Hostname2ip(string hostname)
        {
            try
            {
                IPAddress ip;
                if (IPAddress.TryParse(hostname, out ip))
                    return ip.ToString();
                else
                    return Dns.GetHostEntry(hostname).AddressList[0].ToString();
            }
            catch (Exception)
            {
                throw new Exception("IP Address Error");
            }
        }

        private void _pingThread(object state)
        {
            int i = (int)state;
            try
            {
                IPAddress ip = IPAddress.Parse(Hostname2ip(configX.configs[i].server));
                IPEndPoint point = new IPEndPoint(ip, configX.configs[i].server_port);

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                int t = Environment.TickCount;
                IAsyncResult connresult = sock.BeginConnect(point, new AsyncCallback(Connect), i);
                if (connresult.AsyncWaitHandle.WaitOne(conf_waittime, false))
                {
                    int t2 = Environment.TickCount;
                    configX.configs[i].aaa = (t2 - t) + "ms";
                    sock.Close();
                }
                else
                {
                    sock.Close();
                    configX.configs[i].aaa = "-";
                }
            }
            catch (Exception)
            {
                configX.configs[i].aaa = "-";
                Console.WriteLine(configX.configs[i].server + "---Error");
            }
            finally
            {

                dataGridView1.UpdateCellValue(4, i);
            }
            /*
            Ping ping = new Ping();
                ping.PingCompleted += Ping_PingCompleted;
                string _ipAddress = configX.configs[i].server;
                ping.SendAsync(_ipAddress, 5000, i);
            */
        }

        void Connect(IAsyncResult iar)
        {
        }

        private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (e.Reply == null || e.Reply.RoundtripTime == 0)
            {
                configX.configs[(int)e.UserState].aaa = "-";
            }
            else
            {
                configX.configs[(int)e.UserState].aaa = e.Reply.RoundtripTime.ToString() + "ms";
            }
            dataGridView1.UpdateCellValue(4, (int)e.UserState);
        }

        int toolStrip_select_row;
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            closeProxy();
            killProxy();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"listen = http://0.0.0.0:4411");
            sb.AppendLine("");
            JsonConfigItem item = configX.configs[toolStrip_select_row];
            string proxy = string.Format(@"proxy = ss://{0}:{1}@{2}:{3}", item.method, item.password, item.server, item.server_port);
            sb.AppendLine(proxy);

            FileStream fs = new FileStream(rc_path, FileMode.Truncate);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(sb.ToString());
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();

            currect_connect = toolStrip_select_row;
            startProxy();
        }

        private void closeProxy()
        {
            if (currect_connect != -1)
            {
                configX.configs[currect_connect].bbb = "";
                dataGridView1.UpdateCellValue(5, currect_connect);
            }
        }

        private void dataGridView1_RowContextMenuStripNeeded(object sender, DataGridViewRowContextMenuStripNeededEventArgs e)
        {
            dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[0];
            toolStrip_select_row = e.RowIndex;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            richTextBox1.Visible = !richTextBox1.Visible;
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Show();
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            thread = new Thread(new ThreadStart(getNewConfig));
            thread.IsBackground = true;
            thread.Start();
        }

        private void getNewConfig()
        {
            try 
            {
                IList<JsonConfigItem> configs = configX.configs;
                for (int i = 0; i < configX.serverSubscribes.Count; i++)
                {
                    ServerSubscribe serverSubscribe = configX.serverSubscribes[i];
                    string subscribe64Str = httpGet(serverSubscribe.URL);
                    string subscribeStr = Base64Helper.Base64Decode(subscribe64Str);
                    for (int w = configs.Count - 1; w >= 0; w--)
                    {
                        if (configs[w].group == serverSubscribe.Group)
                            configs.Remove(configs[i]);
                    }
                    string[] list = subscribeStr.Split('\n');
                    for(int j =0;i<list.Length;j++)
                    {
                        string str64 = list[j].Replace("ssr://", "");
                        string one = Base64Helper.Base64Decode(str64);
                        string[] arr = one.Split('/');
                        if(arr.Length == 1)
                        {
                            break;
                        }
                        string[] serverConfig = arr[0].Split(':');
                        Dictionary<string, string> param = ParseQueryString(arr[1]);
                        JsonConfigItem item = new JsonConfigItem();
                        item.server = serverConfig[0];
                        item.server_port = Convert.ToInt32(serverConfig[1]);
                        item.protocol = serverConfig[2];
                        item.method = serverConfig[3];
                        item.obfs = serverConfig[4];
                        item.password = Base64Helper.Base64Decode(serverConfig[5]);
                        item.obfsparam = Base64Helper.Base64Decode(param["obfsparam"]);
                        item.protocolparam = Base64Helper.Base64Decode(param["protoparam"]);
                        item.remarks = Base64Helper.Base64Decode(param["remarks"]);
                        item.remarks_base64 = param["remarks"];
                        item.group = Base64Helper.Base64Decode(param["group"]);
                        configs.Add(item);
                    }
                }

                using (StreamWriter file = File.CreateText(config_path))
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    String jsonStr = serializer.Serialize(configX);
                    SetText(jsonStr);
                    file.Write(jsonStr);
                }

                SetText(".................download config ok..................");
                
                this.Invoke(new EventHandler(delegate
                {
                    loadConfig();
                }));
            }
            catch (Exception e)
            {
                SetText(e.StackTrace);
            }

        }

        public Dictionary<string, string> ParseQueryString(String query)
        {
            Dictionary<String, String> queryDict = new Dictionary<string, string>();
            foreach (String token in query.TrimStart(new char[] { '?' }).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = token.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                    queryDict[parts[0].Trim()] = HttpUtility.UrlDecode(parts[1]).Trim();
                else
                    queryDict[parts[0].Trim()] = "";
            }
            return queryDict;
        }
        private string httpGet(string url)
        {

            try
            {
                CookieContainer cookieContainer = new CookieContainer();

                ///////////////////////////////////////////////////
                // 1.打开 MyLogin.aspx 页面，获得 GetVeiwState & EventValidation
                ///////////////////////////////////////////////////                
                // 设置打开页面的参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.KeepAlive = false;

                // 接收返回的页面
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                System.IO.Stream responseStream = response.GetResponseStream();
                System.IO.StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (WebException we)
            {
                string msg = we.Message;
                return msg;
            }
        }

        private void contextMenuStrip2_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
