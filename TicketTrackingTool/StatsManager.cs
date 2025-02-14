using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System.Xml.Linq;
using System;
using System.Linq;
using System.Data;

namespace Stats
{
    public class StatsManager
    {
        //Constructor
        public StatsManager() 
        { 
        }

        //Calculation Functions
        public static void calculateStats(List<string[]> data, DataGridView dataView, Chart chart, TextBox trendLabel)
        {
            // Ensure that data exists.
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("No data available to calculate statistics.");
                return;
            }

            //Locate the "Created" column (case-insensitive).
            int createdColumnIndex = -1;
            foreach (DataGridViewColumn column in dataView.Columns)
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

            //Build a dictionary mapping dates to the ticket count
            Dictionary<DateTime, int> ticketCounts = new Dictionary<DateTime, int>();
            foreach (var row in data)
            {
                if (row.Length > createdColumnIndex)
                {
                    //Remove extra quotes and trim whitespace
                    string dateString = row[createdColumnIndex].Replace("\"", "").Trim();

                    //Try to parse the date
                    if (DateTime.TryParse(dateString, out DateTime createdDate))
                    {
                        DateTime dateOnly = createdDate.Date;
                        if (ticketCounts.ContainsKey(dateOnly))
                            ticketCounts[dateOnly]++;
                        else
                            ticketCounts[dateOnly] = 1;
                    }
                }
            }

            //Sort the data by date
            var sortedTicketCounts = ticketCounts.OrderBy(kvp => kvp.Key).ToList();
            if (sortedTicketCounts.Count == 0)
            {
                MessageBox.Show("No valid date entries were found.");
                return;
            }
            
            //Configure the chart area for clear, zoomed-in display
            chart.Series.Clear();
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "MM/dd/yyyy";
            chart.ChartAreas[0].AxisX.Interval = 1;
            chart.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;
            chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();

            //Create the main series (line chart with markers and labels)
            Series mainSeries = new Series("Tickets Created")
            {
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.Date,
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8,
                IsValueShownAsLabel = true,
                LabelForeColor = System.Drawing.Color.DarkBlue
            };
            foreach (var kvp in sortedTicketCounts)
            {
                mainSeries.Points.AddXY(kvp.Key, kvp.Value);
            }
            //Add tooltips for each data point
            foreach (DataPoint point in mainSeries.Points)
            {
                point.ToolTip = $"Date: {DateTime.FromOADate(point.XValue):MM/dd/yyyy}\nCount: {point.YValues[0]}";
            }
            chart.Series.Add(mainSeries);

            //Calculate a 7-day moving average to smooth out the trend
            int windowSize = 7;
            List<double> movingAverages = new List<double>();
            for (int i = 0; i < sortedTicketCounts.Count; i++)
            {
                int start = Math.Max(0, i - windowSize + 1);
                double average = sortedTicketCounts.Skip(start)
                                      .Take(i - start + 1)
                                      .Average(kvp => kvp.Value);
                movingAverages.Add(average);
            }
            //Create a trend series to display the moving average
            Series trendSeries = new Series("7-Day Moving Average")
            {
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.Date,
                BorderDashStyle = ChartDashStyle.Dash,
                BorderWidth = 2,
                Color = System.Drawing.Color.Red,
                IsVisibleInLegend = true
            };
            for (int i = 0; i < sortedTicketCounts.Count; i++)
            {
                trendSeries.Points.AddXY(sortedTicketCounts[i].Key, movingAverages[i]);
            }
            chart.Series.Add(trendSeries);

            //Determine an optimal zoom range
            DateTime minDate = sortedTicketCounts.First().Key;
            DateTime maxDate = sortedTicketCounts.Last().Key;
            //If the range is more than 30 days, zoom in to the last 30 days
            if ((maxDate - minDate).TotalDays > 30)
            {
                DateTime zoomStart = maxDate.AddDays(-30);
                chart.ChartAreas[0].AxisX.ScaleView.Zoom(zoomStart.ToOADate(), maxDate.ToOADate());
            }
            else
            {
                chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            }

            //Optionally, update a label with a summary of the trend
            if (sortedTicketCounts.Count >= 2)
            {
                int firstCount = sortedTicketCounts.First().Value;
                int lastCount = sortedTicketCounts.Last().Value;
                string trend = lastCount > firstCount ? "increasing" :
                               lastCount < firstCount ? "decreasing" : "stable";
                trendLabel.Text = $"From {minDate:MM/dd/yyyy} to {maxDate:MM/dd/yyyy}, the trend is {trend}.";
            }
        }

        public static void CalculateDeviceTrends(
            string deviceColumnName,
            string dateColumnName,
            List<string[]> data,
            DataGridView dataView,
            Chart chart)
        {
            // Ensure that data exists.
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("No data available to calculate statistics.");
                return;
            }

            // Locate the device and date columns (case-insensitive).
            int deviceColumnIndex = -1;
            int dateColumnIndex = -1;
            foreach (DataGridViewColumn column in dataView.Columns)
            {
                if (column.HeaderText.Equals(deviceColumnName, StringComparison.OrdinalIgnoreCase))
                    deviceColumnIndex = column.Index;
                if (column.HeaderText.Equals(dateColumnName, StringComparison.OrdinalIgnoreCase))
                    dateColumnIndex = column.Index;
            }
            if (deviceColumnIndex == -1)
            {
                MessageBox.Show($"The '{deviceColumnName}' column was not found in the data.");
                return;
            }
            if (dateColumnIndex == -1)
            {
                MessageBox.Show($"The '{dateColumnName}' column was not found in the data.");
                return;
            }

            // Build a dictionary that maps each device to a dictionary mapping date -> count.
            // For example: { "WACOG-1900": { 2/13/2025: 3, 2/14/2025: 1 }, ... }
            Dictionary<string, Dictionary<DateTime, int>> deviceDateCounts = new Dictionary<string, Dictionary<DateTime, int>>();
            foreach (var row in data)
            {
                if (row.Length > deviceColumnIndex && row.Length > dateColumnIndex)
                {
                    // Clean up the device string.
                    string deviceText = row[deviceColumnIndex].Replace("\"", "").Trim();
                    // Clean up the date string.
                    string dateString = row[dateColumnIndex].Replace("\"", "").Trim();
                    if (DateTime.TryParse(dateString, out DateTime createdDate))
                    {
                        DateTime dateOnly = createdDate.Date;
                        if (!deviceDateCounts.ContainsKey(deviceText))
                        {
                            deviceDateCounts[deviceText] = new Dictionary<DateTime, int>();
                        }
                        if (deviceDateCounts[deviceText].ContainsKey(dateOnly))
                        {
                            deviceDateCounts[deviceText][dateOnly]++;
                        }
                        else
                        {
                            deviceDateCounts[deviceText][dateOnly] = 1;
                        }
                    }
                }
            }

            // Clear and configure the chart.
            chart.Series.Clear();
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "MM/dd/yyyy";
            chart.ChartAreas[0].AxisX.Interval = 1;
            chart.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;
            chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();

            // Determine global min and max dates across all devices.
            DateTime? globalMinDate = null;
            DateTime? globalMaxDate = null;

            // For each device, create a series.
            foreach (var deviceEntry in deviceDateCounts)
            {
                // Sort the dates for this device.
                var sortedDates = deviceEntry.Value.OrderBy(kvp => kvp.Key).ToList();
                if (sortedDates.Count == 0)
                    continue;

                // Update global min/max.
                DateTime localMin = sortedDates.First().Key;
                DateTime localMax = sortedDates.Last().Key;
                if (!globalMinDate.HasValue || localMin < globalMinDate.Value)
                    globalMinDate = localMin;
                if (!globalMaxDate.HasValue || localMax > globalMaxDate.Value)
                    globalMaxDate = localMax;

                Series series = new Series(deviceEntry.Key)
                {
                    ChartType = SeriesChartType.Line,
                    XValueType = ChartValueType.Date,
                    BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6,
                    IsValueShownAsLabel = false // Set to true if you want labels on every point
                };

                foreach (var dateCount in sortedDates)
                {
                    series.Points.AddXY(dateCount.Key, dateCount.Value);
                }

                // Optionally, add tooltips.
                foreach (DataPoint pt in series.Points)
                {
                    pt.ToolTip = $"Device: {deviceEntry.Key}\nDate: {DateTime.FromOADate(pt.XValue):MM/dd/yyyy}\nCount: {pt.YValues[0]}";
                }
                chart.Series.Add(series);
            }

            // Set a zoom range for the X-axis if needed.
            if (globalMinDate.HasValue && globalMaxDate.HasValue)
            {
                if ((globalMaxDate.Value - globalMinDate.Value).TotalDays > 30)
                {
                    DateTime zoomStart = globalMaxDate.Value.AddDays(-30);
                    chart.ChartAreas[0].AxisX.ScaleView.Zoom(zoomStart.ToOADate(), globalMaxDate.Value.ToOADate());
                }
                else
                {
                    chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                }
            }
        }


    }
}