using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TicketTrackingTool.Assets;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TicketTrackingTool
{
    public partial class MainForm : BaseForm
    {
        public MainForm()
        {
            InitializeComponent();
            //Initialize Assets
            AssetManager.SetPictureBox(pictureBox1, AssetManager.WacogImagePath);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CSVModeForm CSVMode = new CSVModeForm();
            this.Hide();
            CSVMode.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("API is not developed yet.");
        }
    }
}
