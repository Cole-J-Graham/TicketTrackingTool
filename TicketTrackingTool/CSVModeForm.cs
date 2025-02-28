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

        //Restriction Functions
        private void restrictTabInput(object sender, TabControlCancelEventArgs e)
        {
            if (_data == null || !_data.Any())
            {
                MessageBox.Show("Must have data loaded first to view other tabs!");
                e.Cancel = true;  // This stops the tab from switching.
            }
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
                Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"You selected: {filePath}");

                parseCsvFile(filePath); // Parse and load data into memory
                _filteredData = _data;
            }
        }
        private void parseCsvFile(string filePath)
        {
            try
            {
                // Show progress bar before starting
                progressBar1.Visible = true;
                progressBar1.Style = ProgressBarStyle.Continuous;
                progressBar1.Minimum = 0;
                progressBar1.Value = 0;

                if (_data != null)
                {
                    _data.Clear();
                }
                else
                {
                    _data = new List<string[]>();
                }

                // Clear any existing columns in the DataGridView so we get a fresh start
                dataGridView1.Columns.Clear();

                using (var reader = new StreamReader(filePath))
                {
                    bool isFirstLine = true;
                    // Count the total lines in the file for progress tracking
                    var totalLines = File.ReadLines(filePath).Count();
                    progressBar1.Maximum = totalLines;

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        // Use custom CSV parser instead of Split(',')
                        var values = ParseCsvLine(line);

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
                // Run all statistics functions for the "Ticket Analytics" tab
                runStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing file: {ex.Message}");
            }
        }
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var field = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // If already in quotes and the next character is also a quote, treat it as an escaped quote.
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++; // Skip the escaped quote.
                    }
                    else
                    {
                        // Toggle the inQuotes flag.
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Indicate new field.
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }
            // Add the last field.
            fields.Add(field.ToString());

            return fields.ToArray();
        }

        private void runStatistics()
        {
            StatsManager.calculateStats(_data, dataGridView1, chart1, trendLabel);
            StatsManager.CalculateDeviceTrends("Device", "Created", _data, dataGridView1, chart2);
            StatsManager.DisplayComprehensiveHandlingTimeStats(_data, dataGridView1, summaryDataGrid);
        }

        //Search Functions
        private void searchKeyDown(object sender, KeyPressEventArgs e)
        {
            string query = searchBar.Text;
            //Check if there is text in the search bar, if so then perform the search
            if (e.KeyChar == 13)
            {
                if(query == "")
                {
                    MessageBox.Show("Query must have text!");
                } else
                {
                    performSearch(query);
                }
            } 
        }
        private void searchButton(object sender, EventArgs e)
        {
            //Search button function
            string query = searchBar.Text;
            performSearch(query);
        }
        private void performSearch(string query)
        {
            //Check if there is any data in the list, if there is none notify the user
            if (_data != null && _data.Any())
            {
                try
                {
                    //Filter data based on the query
                    _filteredData = searchData(query);

                    //Update the DataGridView row count for virtual mode
                    dataGridView1.RowCount = _filteredData.Count;

                    //Refresh the DataGridView to display filtered results
                    dataGridView1.Refresh();

                    //Show a message if no results are found
                    if (_filteredData.Count == 0)
                    {
                        MessageBox.Show("No results found.", "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    //Show a message if an error occurs
                    MessageBox.Show($"Error during search: {ex.Message}");
                }
            } else
            {
                //Show a message if there is no data to search
                MessageBox.Show("Cannot search without any data!");
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
