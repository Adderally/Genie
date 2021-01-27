using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Genie
{
    class ErrorEssentials
    {
        public 
            void missingClientConnection()
        {
            MessageBox.Show("Possible socket issue!\n\nSome functionality may not work", "Socket::connection::errorhandle", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public 
            void failedStartingClientAtStartup()
        {
            MessageBox.Show("Client failed to start!\n\nCheck connection, all tests have failed!!", "CLIENT HELPER", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public 
            void device_cmd_failed()
        {
            MessageBox.Show("Failed to execute remote shell!\n\nTry reconnecting this device", "SHELL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}