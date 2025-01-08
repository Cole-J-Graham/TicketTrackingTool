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

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a file to parse",
                Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"You selected: {filePath}");

                ParseFile(filePath); // Call the parse function
            }
        }

        private void ParseFile(string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".txt")
                {
                    ParseTextFile(filePath);
                }
                else if (extension == ".csv")
                {
                    ParseCsvFile(filePath);
                }
                else
                {
                    MessageBox.Show("Unsupported file format.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing file: {ex.Message}");
            }
        }

        // Text File Parser
        private void ParseTextFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                Console.WriteLine($"Line: {line}");
            }
        }

        // CSV File Parser
        private void ParseCsvFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    foreach (var value in values)
                    {
                        Console.WriteLine($"Value: {value}");
                    }
                }
            }
        }
    }
}
