using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpAdbClient;
namespace Genie
{





    public partial class main_form : Form
    {
        public main_form()
        {
            InitializeComponent();
        }


        AdbClient client = new AdbClient(); //  Global of our client, defaults are typically
                                            //  ADBServer port = 5037
                                            //  Port           = 5555

        
        //---------------------------------------------------------------------------
        



        /* updateDevicesOnChange: 
         -- refreshing list of connected devices through events --
         *  
         *  ( Events )
         * Device is connected.
         * Device is disconnected.
         * 
         */

        //---------------------------------------------------------------------------
        public
            void updateDevicesOnChange(object sender, DeviceDataEventArgs e)
        {

                    
            updateViaThread.ControlInvoke(connected_devices_lv, () =>
            connected_devices_lv.Items.Clear()
            );


            //  AdbClient client = new AdbClient();
            List<DeviceData> devices = client.GetDevices();
            

            foreach (var device in devices)
            {
                
                
                ListViewItem itm;
                string[] arr = new string[4];

                ConsoleOutputReceiver imei = new ConsoleOutputReceiver();     //  Keep device IMEI
                ConsoleOutputReceiver devicename = new ConsoleOutputReceiver();     //  Keep device model nme
                ConsoleOutputReceiver devicecarrier = new ConsoleOutputReceiver();     //  Keep device carrier nme
                ConsoleOutputReceiver deviceoriginalcarrier = new ConsoleOutputReceiver();     //  Keep device original carrier nme


                try
                {
                    
                    client.ExecuteRemoteCommand("service call iphonesubinfo 1 | toybox cut -d \"'\" -f2 | toybox grep -Eo '[0-9]' | toybox xargs | toybox sed 's/\\ //g'", device, imei);   //  Command to catch device IMEI
                                                                                                                                                                                            //  Works with Google phones and Samsung
                    client.ExecuteRemoteCommand("settings get global device_name", device, devicename);
                    client.ExecuteRemoteCommand("getprop ro.boot.carrierid", device, devicecarrier);
                    client.ExecuteRemoteCommand("getprop persist.sys.prev_salescode", device, deviceoriginalcarrier);
                }
                catch (Exception)
                {
                    
                    //  TODO: Make less simple ...
                    
                    arr[0] = "CONNECTION BAD";
                    arr[1] = "CONNECTION NOT COMPLETE";
                    itm = new ListViewItem(arr);


                    updateViaThread.ControlInvoke(connected_devices_lv, () =>
                    connected_devices_lv.Items.Add(itm)
                    );

                    continue;
                }
                    
                    
                                

                try
                {
                    arr[0] = imei.ToString();
                    arr[1] = devicename.ToString();
                    arr[2] = devicecarrier.ToString();
                    arr[3] = deviceoriginalcarrier.ToString();

                    itm = new ListViewItem(arr);


                    updateViaThread.ControlInvoke(connected_devices_lv, () =>
                    connected_devices_lv.Items.Add(itm)
                    );
                }
                catch (NullReferenceException)  //  For now since commands are not being handled serperatly, if one
                                                //  fail then by default they get put as nil/null
                {

                    arr[0] = "nil";
                    arr[1] = "nil";
                    arr[2] = "nil";
                    arr[3] = "nil";



                    itm = new ListViewItem(arr);


                    updateViaThread.ControlInvoke(connected_devices_lv, () =>
                    connected_devices_lv.Items.Add(itm)
                    );
                }
            }                   
        }

        
        //---------------------------------------------------------------------------
        private 
            void updateListOnDisconnect()
        {
            try
            {
                var monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));                
                monitor.DeviceDisconnected += updateDevicesOnChange;
                monitor.Start();
            }
            catch (Exception)
            {
               //   TODO: 
            }
        }


        //---------------------------------------------------------------------------
        private
            void updateConnectedDevices_Lbl()
        {
            //  TODO:   Put in catch block.
            while (true)

                updateViaThread.ControlInvoke(connected_devices_lv, () =>
                toolstrip_deviceCounter.Text = client.GetDevices().Count.ToString()
                ); ;
        }


        //---------------------------------------------------------------------------
        private
            void main_form_Load(object sender, EventArgs e)
        {
            connected_devices_lv.View = View.Details;
            connected_devices_lv.Columns.Add("IMEI / SN#", 150);
            connected_devices_lv.Columns.Add("Name", 190);
            connected_devices_lv.Columns.Add("Build Carrier", 120);
            connected_devices_lv.Columns.Add("Current Carrier", 120);



            AdbEssentials adb_helper     = new AdbEssentials();
            ErrorEssentials error_dialog = new ErrorEssentials();


                if (!adb_helper.isAdbRunningAtStartup())
                    error_dialog.failedStartingClientAtStartup();
            

            Thread update_device_list = new Thread(updateListOnDisconnect);
            update_device_list.Start();

            Thread update_connected_device_count = new Thread(updateConnectedDevices_Lbl);
            update_connected_device_count.Start();


            

            
        }


        //---------------------------------------------------------------------------
        private
            void connected_devices_lv_KeyPress(object sender, KeyPressEventArgs e)
        {
            AdbEssentials essentials = new AdbEssentials();

            if (e.KeyChar == (char)Keys.Enter)

                foreach (var device in client.GetDevices())
                {
                    Thread start_mass_unlock = new Thread(essentials.massUnlockDevices);
                    start_mass_unlock.Start(device);
                }        
        }


        //---------------------------------------------------------------------------
        private
            void toolstrip_listDevices_Click(object sender, EventArgs e)
        {
            Thread show_connected_devices = new Thread(() => updateDevicesOnChange(null, null));
            show_connected_devices.Start();
        }

        private void connected_devices_lv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                Thread show_connected_devices = new Thread(() => updateDevicesOnChange(null, null));
                show_connected_devices.Start();
            }


            if (e.Control && e.KeyCode == Keys.C)
            {
                AdbEssentials essentials = new AdbEssentials();

                Thread copy_imei = new Thread(essentials.copyAllIMEI);
                copy_imei.SetApartmentState(ApartmentState.STA);        //  [STAThread]
                copy_imei.Start();
            }
        }


        //---------------------------------------------------------------------------
        private
            void connected_devices_lv_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                try
                {
                    if (connected_devices_lv.FocusedItem.Bounds.Contains(e.Location))
                        contextmenu_solo_phone_options.Show(Cursor.Position);
                }
                catch (Exception)
                {
                    // TODO
                }
            }
        }


        //---------------------------------------------------------------------------
        private
            void toolstrip_unlockDevices_Click(object sender, EventArgs e)
        {
            AdbEssentials essentials = new AdbEssentials();

            foreach (var device in client.GetDevices())
            {
                Thread start_mass_unlock = new Thread(essentials.massUnlockDevices);
                start_mass_unlock.Start(device);
            }
        }


        //---------------------------------------------------------------------------
        private
            void toolstrip_rebootDeviceDownload_Click(object sender, EventArgs e)
        {
            AdbEssentials essentials = new AdbEssentials();

            foreach (var device in client.GetDevices())
            {
                Thread start_mass_enter_download = new Thread(() => essentials.reboot_device_option(device, "reboot download"));
                start_mass_enter_download.Start();
            }
        }


        //---------------------------------------------------------------------------
        private
            void toolstrip_shutdownDevice_Click(object sender, EventArgs e)
        {

            AdbEssentials essentials = new AdbEssentials();

            foreach (var device in client.GetDevices())
            {
                Thread start_mass_shutdown = new Thread(() => essentials.reboot_device_option(device, "reboot -p"));
                start_mass_shutdown.Start();
            }

        }


        //---------------------------------------------------------------------------
        private
            void toolstrip_RebootDevice_Click(object sender, EventArgs e)
        {
            AdbEssentials essentials = new AdbEssentials();

            foreach (var device in client.GetDevices())
            {
                Thread start_mass_reboot = new Thread(() => essentials.reboot_device_option(device, "reboot"));
                start_mass_reboot.Start();
            }
        }


        //---------------------------------------------------------------------------
        private
            void toolitem_enterRecovery_Click(object sender, EventArgs e)
        {
            AdbEssentials essentials = new AdbEssentials();

            foreach (var device in client.GetDevices())
            {
                Thread start_mass_enter_recovery = new Thread(() => essentials.reboot_device_option(device, "reboot recovery"));
                start_mass_enter_recovery.Start();
            }
        }


        //---------------------------------------------------------------------------


        //  <summary> 
        //  Devices get booted into the bootloader, for most devices this is the same as "reboot download"
        //  </summary>


        private
            void toolitem_enterBootloader_Click(object sender, EventArgs e)
        {
            AdbEssentials essentials = new AdbEssentials();

            foreach (var device in client.GetDevices())
            {
                Thread start_mass_enter_fastboot = new Thread(() => essentials.reboot_device_option(device, "am broadcast -a android.intent.action.MASTER_CLEAR"));
                start_mass_enter_fastboot.Start();
            }
        }


        //---------------------------------------------------------------------------      
        private
            void toolitem_copyAllIMEIs_Click(object sender, EventArgs e)
        {
            AdbEssentials essentials = new AdbEssentials();

            Thread copy_imei = new Thread(essentials.copyAllIMEI);
            copy_imei.SetApartmentState(ApartmentState.STA);        //  [STAThread]
            copy_imei.Start();
        }

        
    }








    class updateViaThread
    {
        delegate void UniversalVoidDelegate();

        public static void ControlInvoke(Control control, Action function)
        {
            if (control.IsDisposed || control.Disposing)
                return;

            if (control.InvokeRequired)
            {
                            
              control.Invoke(new UniversalVoidDelegate(() => ControlInvoke(control, function)));              
              return;
                               
            }
            function();
        }
    }
}
