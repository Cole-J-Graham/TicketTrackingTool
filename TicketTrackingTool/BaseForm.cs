using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicketTrackingTool
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            this.FormClosing += BaseForm_FormClosing;
        }

        /// <summary>
        /// Handles the form closing event with a confirmation dialog.
        /// </summary>
        private void BaseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Ask for confirmation before closing
                if (!Program.ConfirmExit())
                {
                    e.Cancel = true; // Prevent closing if user clicks "No"
                }
                else
                {
                    Application.Exit(); // Ensures a clean shutdown
                }
            }
        }
    }
}
