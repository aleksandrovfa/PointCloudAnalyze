using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
//using Newtonsoft.Json;
using PointCloudAnalyze.Core;
using PointCloudAnalyze.Views;
using Revit.Async;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using static System.Net.WebRequestMethods;

namespace PointCloudAnalyze
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PointCloudAnalyzeCmd : IExternalCommand
    {
        public static PointCloudAnalyzeView _view;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Debug.Listeners.Clear();
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string name = Assembly.GetExecutingAssembly().GetName().Name;
            Debug.Listeners.Add(new RbsLogger.Logger($"{name}_{version}"));
            RevitApi.CommandData = commandData;

            RevitTask.Initialize(RevitApi.AppUI);

            DateTime time = GetNetworkTime();

            if (time > new DateTime(2024, 6, 23))
            {
                if(MessageBox.Show("Триальная версия закончилась. https://t.me/aleksandrov_fa", "Ошибка!", MessageBoxButton.OK,MessageBoxImage.Error) == MessageBoxResult.OK)
                {
                    System.Diagnostics.Process.Start("https://t.me/aleksandrov_fa");
                }
                //если вы это читаете пожалуйста свяжитесь со мной
                return Result.Cancelled;
            }




            try
            {
                if (_view == null)
                {
                    Debug.WriteLine(" Start!!! Create!!!");

                    //RevitApi.CommandData = commandData;
                    //RevitTask.Initialize(RevitApi.AppUI);
                    _view = new PointCloudAnalyzeView();

                    WindowInteropHelper helper = new WindowInteropHelper(_view);
                    helper.Owner = commandData.Application.MainWindowHandle;

                    _view.Show();
                }
                else
                {
                    Debug.WriteLine(" Create!!! Activate!!!");
                    _view.Activate();

                }
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + e.StackTrace);
                MessageBox.Show(e.Message + e.StackTrace, "Что то поломалось... Скоро починим");
                return Result.Failed;
            }
        }

       
        public static DateTime GetNetworkTime()
        {
            const string ntpServer = "pool.ntp.org";
            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //UTC time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        // stackoverflow.com/a/3294698/162671
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
    }
}
