using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicketTrackingTool.Assets
{
    public partial class CSVModeForm : BaseForm
    {
        public CSVModeForm()
        {
            InitializeComponent();
            //Initialize Assets
            AssetManager.SetPictureBox(pictureBox1, AssetManager.WacogImagePath);
        }

        private void CSVModeForm_Load(object sender, EventArgs e)
        {

        }
    }
}
