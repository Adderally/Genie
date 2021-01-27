using SharpAdbClient;
using SharpAdbClient.DeviceCommands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Genie
{
    class AdbEssentials
    {
        public 
            int StartAdbClientFromNAS()
        {
            try
            {
                AdbServer server = new AdbServer();
                //  AdbClient client = new AdbClient();

                var result = server.StartServer(
                    @"PATH",
                    restartServerIfNewer: true
                    );

                return 1;

            }
            catch (Exception)
            {
                return 0;
            }
        }


        //---------------------------------------------------------------------------
        public 
            bool isAdbRunningAtStartup()
        {
            Process[] processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                if (process.ProcessName.Contains("adb"))
                {
                    MessageBox.Show("Client detected running in the background!\n\nI will not start build routine.", "CLIENT HELPER", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }

            if (StartAdbClientFromNAS() != 1)
            {
                MessageBox.Show("Client failed to start!\n\nPlease check internet connection or ask Software for help.", "CLIENT HELPER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            else
            {
                MessageBox.Show("Default client has been started for you!", "CLIENT HELPER", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
        }


        //<summary>
        //Method runs on its own thread per device
        //</summary>


        //---------------------------------------------------------------------------
        public 
            void massUnlockDevices(object device)
        {

            try
            {
                AdbClient client = new AdbClient();


                ConsoleOutputReceiver deviceimei = new ConsoleOutputReceiver();

                client.ExecuteRemoteCommand("service call iphonesubinfo 1 | toybox cut -d \"'\" -f2 | toybox grep -Eo '[0-9]' | toybox xargs | toybox sed 's/\\ //g'",
                    (DeviceData)device,
                    deviceimei
                    );

                ConsoleOutputReceiver devicename = new ConsoleOutputReceiver();
                client.ExecuteRemoteCommand("settings get global device_name", (DeviceData)device, devicename);
                var devicecarrier = new ConsoleOutputReceiver();
                var deviceoriginalcarrier = new ConsoleOutputReceiver();


                client.ExecuteRemoteCommand(
                "getprop ro.boot.carrierid",
                (DeviceData)device,
                devicecarrier
                );


                client.ExecuteRemoteCommand(
                            "getprop persist.sys.prev_salescode",
                            (DeviceData)device,
                            deviceoriginalcarrier
                            );



                string data = deviceimei + "Model: " + devicename + "\nOriginal " + devicecarrier + "Current  " + deviceoriginalcarrier;
/*
                if (deviceoriginalcarrier.ToString() != devicecarrier.ToString())
                {
                    //string data = deviceimei + "Model: " + devicename + "\nOriginal " + devicecarrier + "Current  " + deviceoriginalcarrier;
                    Thread startShow = new Thread(showIfAlreadyUnlocked);
                    startShow.Start(data);


                    if (devicecarrier == null)
                    {
                        string data1 = deviceimei + "Model: " + devicename + "\nOriginal " + devicecarrier + "Current  NULL\n\nIssues With Reading Carrier Config! - > This is Usually Due To Older Android Versions And How Variables Are Stored!!";
                        Thread nullCode = new Thread(showIfNoCarrier);
                        nullCode.Start(data1);

                        Thread.CurrentThread.Abort();
                        return;
                    }


                    Thread.CurrentThread.Abort();
                    return;


                }
*/

                bool codefound = false;


                foreach (string line in File.ReadLines(@"PATH"))
                {
                    if (line.Contains(deviceimei.ToString().Remove(15)))
                    {


                        if (line.Length <= 17)
                        {
                            MessageBox.Show("Model: " + devicename + "IMEI: " + deviceimei + "\n\nIMEI only");                            
                            Thread.CurrentThread.Abort();
                            break;
                        }

                        if (line.Contains("NOT FOUND"))
                        {
                            MessageBox.Show("Model: " + devicename + "IMEI: " + deviceimei + "\n\nCode = NOT FOUND");                            
                            Thread.CurrentThread.Abort();
                            break;
                        }





                        if (line.Contains(","))
                        {
                            codefound = true;
                            string code = line.Split(',')[1];

                            client.ExecuteShellCommand((DeviceData)device, "input keyevent 5", null);
                            client.ExecuteShellCommand((DeviceData)device, "input text '#7465625*638*#'", null);
                            client.ExecuteShellCommand((DeviceData)device, "input text '" + code + "'", null);

                            if (devicename.ToString().Contains("S20"))
                            {
                                client.ExecuteShellCommand((DeviceData)device, "input tap 360 1400", null);
                            }
                            else
                            {
                                client.ExecuteShellCommand((DeviceData)device, "input tap 360 1300", null);
                            }
                            

                            Thread.CurrentThread.Abort();
                            break;
                        }
                        else
                        {

                            codefound = true;
                            string codespace = line.Split(null)[1];




                            client.ExecuteShellCommand((DeviceData)device, "input keyevent 5", null);
                            client.ExecuteShellCommand((DeviceData)device, "input text '#7465625*638*#'", null);    //  Engineering code for Network unlock prompts
                            client.ExecuteShellCommand((DeviceData)device, "input text '" + codespace + "'", null);


                            if (devicename.ToString().Contains("S20"))
                            {
                                client.ExecuteShellCommand((DeviceData)device, "input tap 360 1400", null);
                            }
                            else
                            {
                                client.ExecuteShellCommand((DeviceData)device, "input tap 360 1300", null);
                            }                           
                            Thread.CurrentThread.Abort();
                            break;

                        }
                    }
                }
                if (!codefound)
                {                  
                    MessageBox.Show("Model: " + devicename + "IMEI: " + deviceimei + "\n\nI could not find that IMEI in the system!\n\nTry updating?");
                }
            }
            catch (Exception)
            {             
                Thread.CurrentThread.Abort();
            }
            Thread.CurrentThread.Abort();
        }


        //---------------------------------------------------------------------------
        public 
            void reboot_device_option(object device, object command)
        {
            try
            {

                AdbClient client = new AdbClient();

                client.ExecuteRemoteCommand(
                        (string)command,
                        (DeviceData)device,
                         null
                        );
            }
            catch (Exception)
            {

                ErrorEssentials essentials = new ErrorEssentials();
                essentials.device_cmd_failed();

            }
        }


        //---------------------------------------------------------------------------
        public
            void copyAllIMEI()
        {

            AdbClient client = new AdbClient();
            

            StringBuilder build = new StringBuilder();
            build.Clear();


            foreach (var device in client.GetDevices())
            {
                try
                {

                    ConsoleOutputReceiver deviceimei = new ConsoleOutputReceiver();

                    client.ExecuteRemoteCommand(
                            "service call iphonesubinfo 1 | toybox cut -d \"'\" -f2 | toybox grep -Eo '[0-9]' | toybox xargs | toybox sed 's/\\ //g'",
                            device,
                            deviceimei
                            );
                    string built = deviceimei.ToString();                   
                    build.Append(built);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            Clipboard.Clear();
            Clipboard.SetText(build.ToString());
            MessageBox.Show(build.ToString(), "CLIPBOARD", MessageBoxButtons.OK, MessageBoxIcon.Information);
            build.Clear();
        }
    }
}
