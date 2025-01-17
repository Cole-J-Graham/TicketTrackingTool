using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TicketTrackingTool.Assets
{
    public partial class CSVModeForm : BaseForm
    {
        private List<string[]> _data; // Store CSV data in memory
        private List<string[]> _filteredData; // Store filtered data
        private int _columnCount;    // Track the number of columns

        public CSVModeForm()
        {
            InitializeComponent();
            AssetManager.SetPictureBox(pictureBox1, AssetManager.WacogImagePath);

            // Initialize DataGridView for virtual mode
            dataGridView1.VirtualMode = true;
            dataGridView1.CellValueNeeded += DataGridView1_CellValueNeeded;
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

                ParseCsvFile(filePath); // Parse and load data into memory
                _filteredData = _data;
            }
        }

        private void ParseCsvFile(string filePath)
        {
            try
            {
                // Show progress bar before starting
                progressBar1.Visible = true;
                progressBar1.Style = ProgressBarStyle.Continuous;
                progressBar1.Minimum = 0;
                progressBar1.Value = 0;

                _data = new List<string[]>(); // Initialize data storage

                using (var reader = new StreamReader(filePath))
                {
                    bool isFirstLine = true;

                    // Count the total lines in the file for progress tracking
                    var totalLines = File.ReadLines(filePath).Count();
                    progressBar1.Maximum = totalLines;

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        if (isFirstLine)
                        {
                            // Define column names using the header row
                            _columnCount = values.Length;
                            for (int i = 0; i < _columnCount; i++)
                            {
                                string columnName = string.IsNullOrWhiteSpace(values[i])
                                    ? $"Column{i + 1}"
                                    : values[i];
                                dataGridView1.Columns.Add($"Column{i + 1}", columnName);
                            }
                            isFirstLine = false;
                        }
                        else
                        {
                            // Add the parsed row to the data list
                            _data.Add(values);
                        }
                        // Update the progress bar
                        progressBar1.Value++;
                    }

                    // Set the row count for the DataGridView (virtual mode)
                    dataGridView1.RowCount = _data.Count;
                }
                // Hide progress bar after parsing is done
                progressBar1.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing file: {ex.Message}");
            }
        }

        // Handle the VirtualMode event to supply data on demand
        private void DataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            var sourceData = _filteredData ?? _data; // Use filtered data if available

            if (sourceData == null || e.RowIndex >= sourceData.Count || e.ColumnIndex >= _columnCount)
            {
                e.Value = null; // Ensure no out-of-bounds access
                return;
            }

            // Provide the value for the requested cell
            e.Value = sourceData[e.RowIndex][e.ColumnIndex];
        }


        private List<string[]> SearchData(string query)
        {
            if (_data == null || string.IsNullOrWhiteSpace(query))
            {
                return _data; // Return all data if no query is provided
            }

            // Perform a case-insensitive search across all columns
            return _data.Where(row => row.Any(cell => cell != null && cell.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
        }

        private void PerformSearch(string query)
        {
            try
            {
                // Filter data based on the query
                _filteredData = SearchData(query);

                // Update the DataGridView row count for virtual mode
                dataGridView1.RowCount = _filteredData.Count;

                // Refresh the DataGridView to display filtered results
                dataGridView1.Refresh();

                // Show a message if no results are found
                if (_filteredData.Count == 0)
                {
                    MessageBox.Show("No results found.", "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during search: {ex.Message}");
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            string query = textBox1.Text;
            PerformSearch(query);
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {
        }
    }
}
