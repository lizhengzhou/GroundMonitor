using GroundMonitor.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GroundMonitor
{
    public partial class Form1 : Form
    {
        string Ip = "10.10.100.254";
        Thread _thread_connect;
        Thread _thread_recieve;
        static TcpClient client;

        PictureBox[] Devices;
        static bool[] DeviceStatus;
        static bool IsClosing = false;

        static object _pb_lock = new object();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ServerIP"]))
            {
                Ip = ConfigurationManager.AppSettings["ServerIP"];
            }

            Devices = new PictureBox[4] { pb_device1, pb_device2, pb_device3, pb_device4 };
            DeviceStatus = new bool[4] { true, true, true, true };
            client = new TcpClient();

            _thread_recieve = new Thread(Recieve);
            _thread_recieve.Start();

            _thread_connect = new Thread(Connect);
            _thread_connect.Start();
        }

        void Connect()
        {
            while (!IsClosing)
            {
                if (!client.Connected)
                {
                    try
                    {
                        client.Connect(Ip, 8899);

                        UpdateMsg("连接成功");
                    }
                    catch
                    {
                        UpdateMsg($"连接失败->{Ip}:8899");
                    }
                }

                Thread.Sleep(1000);
            }

        }

        void Recieve()
        {
            while (!IsClosing)
            {
                try
                {
                    if (client.Connected && client.Available > 0)
                    {
                        var stream = client.GetStream();
                        var buf = new byte[client.Available];
                        stream.Read(buf, 0, client.Available);

                        UpdateMsg(BitConverter.ToString(buf));

                        handleData(buf);

                    }
                }
                catch (Exception ex)
                {
                    UpdateMsg(ex.Message);
                }

                Thread.Sleep(500);
            }
        }

        void handleData(byte[] buf)
        {
            if (buf.Length > 13)
            {
                for (int i = 0; i + 13 < buf.Length; i++)
                {
                    if (buf[i] == 0xa5 && buf[i + 1] == 0x5a)
                    {
                        var index = buf[i + 9];
                        var status = buf[10] == 0x01;

                        UpdateStatus(index, status);

                    }
                }
            }
        }

        void UpdateMsg(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>((msg) =>
                {
                    lb_msg.Text = "#0 " + msg;
                }), message);
            }
            else
            {
                lb_msg.Text = "#0 " + message;
            }
        }

        void UpdateStatus(int index, bool status)
        {
            if (DeviceStatus.Length >= index && DeviceStatus[index - 1] != status)
            {
                var statusDesc = status ? "正常" : "异常";
                UpdateMsg($"设备{index}接地{statusDesc}");

                var pic = status ? Resources.green : Resources.red;

                lock (_pb_lock)
                {
                    Devices[index - 1].Image = pic;
                    DeviceStatus[index - 1] = status;
                }

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsClosing = true;
        }
    }
}
