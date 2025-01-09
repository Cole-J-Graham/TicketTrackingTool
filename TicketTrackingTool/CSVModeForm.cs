using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
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
            // Show progress bar before starting
            progressBar1.Visible = true;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;

            using (var reader = new StreamReader(filePath))
            {
                bool columnsAdded = false;

                // Count the total lines in the file for progress tracking
                var totalLines = File.ReadLines(filePath).Count();
                progressBar1.Maximum = totalLines;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    // Add columns only once
                    if (!columnsAdded)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            dataGridView1.Columns.Add($"Column{i + 1}", $"Column {i + 1}");
                        }
                        columnsAdded = true; // Prevent further column additions
                    }

                    // Add the row to the DataGridView
                    dataGridView1.Rows.Add(values);

                    // Update the progress bar
                    progressBar1.Value++;
                }
            }

            // Hide progress bar after parsing is done
            progressBar1.Hide();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
    }
}
