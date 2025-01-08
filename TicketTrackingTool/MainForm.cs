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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TicketTrackingTool
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            // Get the project root dynamically
            string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;

            // Build the path to the image
            string imagePath = Path.Combine(projectRoot, "Assets", "WACOG.jpg");
            if (File.Exists(imagePath))
            {
                pictureBox1.Image = Image.FromFile(imagePath);
            }
            else
            {
                MessageBox.Show($"Error: Image not found at path:\n{imagePath}");
            }
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Launched CSV Parser!");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Launched API Runner!");
        }
    }
}
