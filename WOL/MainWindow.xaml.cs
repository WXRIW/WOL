using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WOL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //通过正则表达式设定MAC地址筛选标准，关于正则表达式请自行百度
        const string macCheckRegexString = @"^([0-9a-fA-F]{2})(([/\s:-][0-9a-fA-F]{2}){5})$";

        private static readonly Regex MacCheckRegex = new Regex(macCheckRegexString);

        public MainWindow()
        {
            InitializeComponent();
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wol.ini");//在当前程序路径创建
            if (File.Exists(filePath))
            {
                TextBoxIP.Text = INIHelper.Read("WOL", "IP", "", filePath);
                TextBoxMAC.Text = INIHelper.Read("WOL", "MAC", "", filePath);
                TextBoxPort.Text = INIHelper.Read("WOL", "Port", "3389", filePath);
            }
        }

        //唤醒主要逻辑方法
        public static bool WakeUp(string mac, string ip, int port)
        {
            //查看该MAC地址是否匹配正则表达式定义，（mac，0）前一个参数是指mac地址，后一个是从指定位置开始查询，0即从头开始
            if (MacCheckRegex.IsMatch(mac, 0) || mac.Length == 16)
            {
                byte[] macByte = FormatMac(mac);
                try
                {
                    WakeUpCore(macByte, ip, port);
                }
                catch
                {
                    return false;
                }
                return true;
            }

            return false;

        }

        private static void WakeUpCore(byte[] mac, string ip, int port)
        {
            //发送方法是通过UDP
            UdpClient client = new UdpClient();
            //Broadcast内容为：255,255,255,255.广播形式，所以不需要IP
            //client.Connect(System.Net.IPAddress.Broadcast, 50000);
            client.Connect(System.Net.IPAddress.Parse(ip), port);

            //下方为发送内容的编制，6遍“FF”+17遍mac的byte类型字节。
            byte[] packet = new byte[17 * 6];
            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;
            for (int i = 1; i <= 16; i++)
                for (int j = 0; j < 6; j++)
                    packet[i * 6 + j] = mac[j];
            //唤醒动作
            int result = client.Send(packet, packet.Length);
        }

        private static byte[] FormatMac(string macInput)
        {
            byte[] mac = new byte[6];

            string str = macInput;
            //消除MAC地址中的“-”符号
            string[] sArray = str.Split('-');


            //mac地址从string转换成byte
            for (var i = 0; i < 6; i++)
            {
                var byteValue = Convert.ToByte(sArray[i], 16);
                mac[i] = byteValue;
            }

            return mac;
        }

        /// <summary>
        /// 传入域名返回对应的IP 
        /// </summary>
        /// <param name="domainName">域名</param>
        /// <returns></returns>
        public static string GetIp(string domainName)
        {
            domainName = domainName.Replace("http://", "").Replace("https://", "");
            IPHostEntry hostEntry = Dns.GetHostEntry(domainName);
            IPEndPoint ipEndPoint = new IPEndPoint(hostEntry.AddressList[0], 0);
            return ipEndPoint.Address.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string ip = TextBoxIP.Text;
            if (!System.Net.IPAddress.TryParse(ip, out _))
            {
                ip = GetIp(ip);
            }
            bool result = WakeUp(TextBoxMAC.Text, ip, int.Parse(TextBoxPort.Text));
            if (result)
            {
                TextBlockResult.Foreground = Brushes.Green;
                TextBlockResult.Text = string.Format("A magic packet has been sent via UDP to IP address: {0}:{1}", ip, TextBoxPort.Text);
            }
            else
            {
                TextBlockResult.Foreground = Brushes.Red;
                TextBlockResult.Text = "Failed";
            }
        }



    }
}
