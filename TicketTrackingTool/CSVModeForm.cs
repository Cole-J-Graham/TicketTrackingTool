using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

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

            // Enable double buffering to reduce flicker
            typeof(DataGridView)
                .InvokeMember("DoubleBuffered",
                              System.Reflection.BindingFlags.SetProperty |
                              System.Reflection.BindingFlags.Instance |
                              System.Reflection.BindingFlags.NonPublic,
                              null, dataGridView1, new object[] { true });

            this.Resize += CSVModeForm_Resize;
        }

        private void CSVModeForm_Resize(object sender, EventArgs e)
        {
            dataGridView1.Refresh();
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
                calculateStats();
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

        private void calculateStats()
        {
            // Ensure that data exists.
            if (_data == null || _data.Count == 0)
            {
                MessageBox.Show("No data available to calculate statistics.");
                return;
            }

            // Find the index of the "Created" column based on the header text.
            int createdColumnIndex = -1;
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.HeaderText.Equals("Created", StringComparison.OrdinalIgnoreCase))
                {
                    createdColumnIndex = column.Index;
                    break;
                }
            }

            if (createdColumnIndex == -1)
            {
                MessageBox.Show("The 'Created' column was not found in the data.");
                return;
            }

            // Create a dictionary to hold ticket counts per day.
            Dictionary<DateTime, int> ticketCounts = new Dictionary<DateTime, int>();

            // Loop through each data row.
            foreach (var row in _data)
            {
                // Check that the row has the expected number of columns.
                if (row.Length > createdColumnIndex)
                {
                    // Remove extra quotes and trim whitespace.
                    string dateString = row[createdColumnIndex].Replace("\"", "").Trim();

                    // Try to parse the date.
                    if (DateTime.TryParse(dateString, out DateTime createdDate))
                    {
                        // Use only the date portion.
                        DateTime dateOnly = createdDate.Date;
                        if (ticketCounts.ContainsKey(dateOnly))
                        {
                            ticketCounts[dateOnly]++;
                        }
                        else
                        {
                            ticketCounts[dateOnly] = 1;
                        }
                    }
                    else
                    {
                        // Optional: Log or display an error if parsing fails.
                        // MessageBox.Show($"Could not parse date: {dateString}");
                    }
                }
            }

            // Sort the data by date.
            var sortedTicketCounts = ticketCounts.OrderBy(kvp => kvp.Key).ToList();

            // Set up or clear the chart.
            chart1.Series.Clear();
            Series series = new Series("Tickets Created")
            {
                ChartType = SeriesChartType.Column, // or Column, if you prefer
                XValueType = ChartValueType.Date
            };

            // Populate the series with the calculated data.
            foreach (var kvp in sortedTicketCounts)
            {
                series.Points.AddXY(kvp.Key, kvp.Value);
            }

            chart1.Series.Add(series);
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "MM/dd/yyyy";

            // Optional: Provide a simple trend analysis.
            if (sortedTicketCounts.Count >= 2)
            {
                int firstCount = sortedTicketCounts.First().Value;
                int lastCount = sortedTicketCounts.Last().Value;
                string trend = lastCount > firstCount
                    ? "increasing"
                    : lastCount < firstCount
                        ? "decreasing"
                        : "stable";

                string trendText = $"From {sortedTicketCounts.First().Key:MM/dd/yyyy} to {sortedTicketCounts.Last().Key:MM/dd/yyyy}, the ticket creation trend is {trend}.";
                trendLabel.Text = trendText;

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

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }
    }
}
