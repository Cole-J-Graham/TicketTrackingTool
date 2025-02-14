using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Stats;

namespace TicketTrackingTool.Assets
{
    public partial class CSVModeForm : BaseForm
    {
        private List<string[]> _data; //Store CSV data in memory
        private List<string[]> _filteredData; //Store filtered data
        private int _columnCount; //Track the number of columns

        //Constructor
        public CSVModeForm()
        {
            InitializeComponent();
            AssetManager.SetPictureBox(pictureBox1, AssetManager.WacogImagePath);
            
            // Initialize DataGridView for virtual mode
            dataGridView1.VirtualMode = true;
            dataGridView1.CellValueNeeded += checkCellValuesNeeded;

            // Enable double buffering to reduce flicker
            typeof(DataGridView)
                .InvokeMember("DoubleBuffered",
                              System.Reflection.BindingFlags.SetProperty |
                              System.Reflection.BindingFlags.Instance |
                              System.Reflection.BindingFlags.NonPublic,
                              null, dataGridView1, new object[] { true });

            this.Resize += CSVModeForm_Resize;
        }

        //Clean-up Functions
        private void CSVModeForm_Resize(object sender, EventArgs e)
        {
            dataGridView1.Refresh();
        }
        private void checkCellValuesNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            var sourceData = _filteredData ?? _data; // Use filtered data if available

            if (sourceData == null || e.RowIndex >= sourceData.Count)
            {
                e.Value = null;
                return;
            }

            var row = sourceData[e.RowIndex];

            // Check if the current row has the requested column
            if (e.ColumnIndex < row.Length)
            {
                e.Value = row[e.ColumnIndex];
            }
            else
            {
                e.Value = string.Empty; // Or any default value you prefer
            }
        }

        //Primary Functions
        private void selectFile(object sender, EventArgs e)
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
                Stats.StatsManager.calculateStats(_data, dataGridView1, chart1, trendLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing file: {ex.Message}");
            }
        }

        //Search Functions
        private void searchButton(object sender, EventArgs e)
        {
            string query = textBox1.Text;
            performSearch(query);
        }
        private void performSearch(string query)
        {
            try
            {
                // Filter data based on the query
                _filteredData = searchData(query);

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
        private List<string[]> searchData(string query)
        {
            if (_data == null || string.IsNullOrWhiteSpace(query))
            {
                return _data; // Return all data if no query is provided
            }

            // Perform a case-insensitive search across all columns
            return _data.Where(row => row.Any(cell => cell != null && cell.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
        }
    }
}
