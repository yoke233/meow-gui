﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Process p;
        ProcessStartInfo s;
        Thread thread;
        JsonConfig configX;
        int currect_connect = -1;
        string exefile = @"proxy_yoke";
        string init_server = "";
        string rc_path = @"rc.txt";
        string config_path = @"gui-config.json";

        private void Form1_Load(object sender, EventArgs e)
        {

            this.notifyIcon1.ShowBalloonTip(500, "yoke_ss", "I am here...", ToolTipIcon.Info);

            s = new ProcessStartInfo();
            s.RedirectStandardInput = true;
            s.RedirectStandardOutput = true;
            s.CreateNoWindow = true;
            s.UseShellExecute = false;
            s.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                getNow();
                loadConfig();
                startProxy();
            }
            catch (Exception e2)
            {
                Console.WriteLine(e2);
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
                Process[] proc = Process.GetProcessesByName(exefile);
                for (int i = 0; i < proc.Length; i++)
                {
                    proc[i].Kill();
                }
            }
            catch (Exception e2)
            {
                Console.WriteLine(e2);
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
                s.FileName = exefile;
                p = Process.Start(s);
                while (!p.StandardOutput.EndOfStream)
                {
                    string str = p.StandardOutput.ReadLine();
                    SetText(str);
                }
                p.WaitForExit();
                p.Close();
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

            //this.listView1.BeginUpdate();   //数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度

            using (StreamReader reader = File.OpenText(config_path))
            {
                JsonSerializer serializer = new JsonSerializer();
                configX = (JsonConfig)serializer.Deserialize(reader, typeof(JsonConfig));
                exefile = configX.exec;
                this.dataGridView1.DataSource = configX.configs;

                for (int i = 0; i < configX.configs.Count; i++)
                {
                    JsonConfigItem item = configX.configs[i];
                    if (item.server.Equals(init_server))
                    {
                        currect_connect = i;
                        item.bbb = "已连接";
                    }
                    /*Console.WriteLine(item);

                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = item.remarks;
                    lvi.Tag = item;
                    lvi.SubItems.Add(item.server.Trim());
                    lvi.SubItems.Add("");
                    lvi.SubItems.Add("");
                    this.listView1.Items.Add(lvi);*/
                }
            }
            /*
            this.listView1.EndUpdate();  //结束数据处理，UI界面一次性绘制。
            this.listView1.ListViewItemSorter = new ListViewColumnSorter();
            */
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
            Thread thread = new Thread(new ThreadStart(_pingThread));
            thread.IsBackground = true;
            thread.Start();
        }

        private void _pingThread()
        {
            for (int i = 0; i < configX.configs.Count; i++)
            {
                Ping ping = new Ping();
                ping.PingCompleted += Ping_PingCompleted;
                string _ipAddress = configX.configs[i].server;
                ping.SendAsync(_ipAddress, 5000, i);
            }
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
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            postParams.Add("username", configX.user);
            postParams.Add("password", configX.pass);

            string returnHtml = GetAspNetCodeResponseDataFromWebSite(postParams,
                "https://www.shadowsu.com/clientarea.php",
                "https://www.shadowsu.com/dologin.php",
                "https://www.shadowsu.com/clientarea.php?action=productdetails&id=484");

            JsonConfig configY = new JsonConfig();
            configY.exec = configX.exec;
            configY.user = configX.user;
            configY.pass = configX.pass;
            configY.configs = new List<JsonConfigItem>();

            Match match = Regex.Match(returnHtml, @"<td>密码</td><td>(.*)</td>");
            string password = match.Groups[1].Value;
            match = Regex.Match(returnHtml, @"<td>端口</td><td>(.*)</td>");
            string server_port = match.Groups[1].Value;

            MatchCollection coll = Regex.Matches(returnHtml, "<tr><td>([^<]*)</td><td>([^<]*)</td><td>([^<]*)</td><td>([^<]*)</td><td>([^<]*)</td><td><a href=\"//[^\"]*\" target=\"_blank\">获取</a></td></tr>");
            for(int i = 0; i < coll.Count; i++)
            {
                match = coll[i];
                JsonConfigItem item = new JsonConfigItem();
                item.server = match.Groups[3].Value;
                item.name = match.Groups[2].Value;
                item.method = match.Groups[5].Value;
                item.server_port = Int32.Parse(server_port);
                item.password = password;
                item.status = match.Groups[1].Value;
                configY.configs.Add(item);
            }
            File.Delete(config_path + ".bak");
            File.Move(config_path, config_path + ".bak");

            using (StreamWriter file = File.CreateText(config_path))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, configY);
            }

        }

        private string GetAspNetCodeResponseDataFromWebSite(Dictionary<string, string> postParams, 
            string getTokenUrl, string loginUrl, string getDataUrl)
        {

            try
            {
                CookieContainer cookieContainer = new CookieContainer();

                ///////////////////////////////////////////////////
                // 1.打开 MyLogin.aspx 页面，获得 GetVeiwState & EventValidation
                ///////////////////////////////////////////////////                
                // 设置打开页面的参数
                HttpWebRequest request = WebRequest.Create(getTokenUrl) as HttpWebRequest;
                request.Method = "GET";
                request.KeepAlive = false;

                // 接收返回的页面
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                System.IO.Stream responseStream = response.GetResponseStream();
                System.IO.StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                string srcString = reader.ReadToEnd();

                // 获取页面的 token,分析返回的页面，解析出 token 的值          
                string viewStateFlag = "id=\"token\" value=\"";
                int i = srcString.IndexOf(viewStateFlag) + viewStateFlag.Length;
                int j = srcString.IndexOf("\"", i);
                string token = srcString.Substring(i, j - i);

                postParams.Add("token", token);


                ///////////////////////////////////////////////////
                // 2.自动填充并提交 Login.aspx 页面，提交Login.aspx页面，来保存Cookie
                ///////////////////////////////////////////////////


                // 要提交的字符串数据。格式形如:user=uesr1&password=123
                string postString = "";
                foreach (KeyValuePair<string, string> de in postParams)
                {
                    //把提交按钮中的中文字符转换成url格式，以防中文或空格等信息
                    postString += System.Web.HttpUtility.UrlEncode(de.Key.ToString()) + "=" + System.Web.HttpUtility.UrlEncode(de.Value.ToString()) + "&";
                }

                // 将提交的字符串数据转换成字节数组
                byte[] postData = Encoding.ASCII.GetBytes(postString);

                // 设置提交的相关参数
                request = WebRequest.Create(loginUrl) as HttpWebRequest;
                request.Method = "POST";
                request.KeepAlive = false;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = cookieContainer;
                request.ContentLength = postData.Length;
                request.AllowAutoRedirect = false;

                // 提交请求数据
                System.IO.Stream outputStream = request.GetRequestStream();
                outputStream.Write(postData, 0, postData.Length);
                outputStream.Close();

                // 接收返回的页面
                response = request.GetResponse() as HttpWebResponse;
                responseStream = response.GetResponseStream();
                reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                srcString = reader.ReadToEnd();

                ///////////////////////////////////////////////////
                // 3.打开需要抓取数据的页面
                ///////////////////////////////////////////////////
                // 设置打开页面的参数
                request = WebRequest.Create(getDataUrl) as HttpWebRequest;
                request.Method = "GET";
                request.KeepAlive = false;
                request.CookieContainer = cookieContainer;

                // 接收返回的页面
                response = request.GetResponse() as HttpWebResponse;
                responseStream = response.GetResponseStream();
                reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                srcString = reader.ReadToEnd();
                return srcString;
                ///////////////////////////////////////////////////
                // 4.分析返回的页面
                ///////////////////////////////////////////////////
                // ...... ......
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