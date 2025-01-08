using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TicketTrackingTool.Assets;

namespace TicketTrackingTool
{
    internal static class Program
    {
        //The main entry point for the application.
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Global cleanup handler
            Application.ApplicationExit += OnApplicationExit;

            //Start the main form
            Application.Run(new MainForm());
        }

        //Global cleanup logic on application exit.
        private static void OnApplicationExit(object sender, EventArgs e)
        {
            Console.WriteLine("Application is exiting. Performing final cleanup...");
            //Add global cleanup logic here, e.g., close database connections, save logs, etc.
        }

        //Global confirmation for application closure.
        public static bool ConfirmExit()
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit the application?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return result == DialogResult.Yes;
        }
    }
}
