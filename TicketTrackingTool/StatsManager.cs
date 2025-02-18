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
            //Ensure that data exists
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("No data available to calculate statistics.");
                return;
            }

            //Locate the "Created" column (case-insensitive)
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
                    string dateString = row[createdColumnIndex].Replace("\"", "").Trim();
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

            //Clear and reset the chart
            chart.Series.Clear();
            chart.Titles.Clear();
            chart.Legends.Clear();
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "MM/dd/yyyy";
            chart.ChartAreas[0].AxisX.Interval = 1;
            chart.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;
            chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();

            //Create the main series
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
            foreach (DataPoint point in mainSeries.Points)
            {
                point.ToolTip = $"Date: {DateTime.FromOADate(point.XValue):MM/dd/yyyy}\nCount: {point.YValues[0]}";
            }
            chart.Series.Add(mainSeries);

            //Calculate a 7-day moving average
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

            //Set zoom range
            DateTime minDate = sortedTicketCounts.First().Key;
            DateTime maxDate = sortedTicketCounts.Last().Key;
            if ((maxDate - minDate).TotalDays > 30)
            {
                DateTime zoomStart = maxDate.AddDays(-30);
                chart.ChartAreas[0].AxisX.ScaleView.Zoom(zoomStart.ToOADate(), maxDate.ToOADate());
            }
            else
            {
                chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            }

            //Update trend label
            if (sortedTicketCounts.Count >= 2)
            {
                int firstCount = sortedTicketCounts.First().Value;
                int lastCount = sortedTicketCounts.Last().Value;
                string trend = lastCount > firstCount ? "increasing" :
                               lastCount < firstCount ? "decreasing" : "stable";
                trendLabel.Text = $"From {minDate:MM/dd/yyyy} to {maxDate:MM/dd/yyyy}, the trend is {trend}.";
            }

            //Force a refresh
            chart.Invalidate();
            chart.Update();
            chart.Refresh();
        }
        public static void CalculateDeviceTrends(
            string deviceColumnName,
            string dateColumnName,
            List<string[]> data,
            DataGridView dataView,
            Chart chart)
        {
            //Ensure data exists
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("No data available to calculate statistics.");
                return;
            }

            //Completely reset the chart control
            chart.Series.Clear();
            chart.Legends.Clear();
            chart.Titles.Clear();

            //Reset the chart area
            ChartArea chartArea = chart.ChartAreas[0];
            //Reset axis settings to their defaults
            chartArea.AxisX.Minimum = double.NaN;
            chartArea.AxisX.Maximum = double.NaN;
            chartArea.AxisX.ScaleView.ZoomReset();
            chartArea.RecalculateAxesScale();

            //Locate the device and date columns in the DataGridView
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

            //Build dictionaries for per-day counts and total counts
            Dictionary<string, Dictionary<DateTime, int>> deviceDateCounts = new Dictionary<string, Dictionary<DateTime, int>>();
            Dictionary<string, int> deviceTotalCounts = new Dictionary<string, int>();

            foreach (var row in data)
            {
                if (row.Length > deviceColumnIndex && row.Length > dateColumnIndex)
                {
                    string deviceText = row[deviceColumnIndex].Replace("\"", "").Trim();
                    string dateString = row[dateColumnIndex].Replace("\"", "").Trim();
                    if (DateTime.TryParse(dateString, out DateTime createdDate))
                    {
                        DateTime dateOnly = createdDate.Date;
                        if (!deviceDateCounts.ContainsKey(deviceText))
                        {
                            deviceDateCounts[deviceText] = new Dictionary<DateTime, int>();
                            deviceTotalCounts[deviceText] = 0;
                        }
                        if (deviceDateCounts[deviceText].ContainsKey(dateOnly))
                            deviceDateCounts[deviceText][dateOnly]++;
                        else
                            deviceDateCounts[deviceText][dateOnly] = 1;
                        deviceTotalCounts[deviceText]++;
                    }
                }
            }

            //Sort devices by total count descending
            var sortedDevices = deviceTotalCounts.OrderByDescending(kvp => kvp.Value).ToList();

            //Set up the legend and title
            Legend legend = new Legend("Devices") { Docking = Docking.Right };
            chart.Legends.Add(legend);
            chart.Titles.Add("Device Trends (Device Name (Total Count))");

            //Configure the chart area axes
            chartArea.AxisX.LabelStyle.Format = "MM/dd/yyyy";
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Days;
            chartArea.AxisX.Title = "Date";
            chartArea.AxisY.Title = "Daily Ticket Count";

            //Determine the global minimum and maximum dates
            DateTime? globalMinDate = null;
            DateTime? globalMaxDate = null;
            foreach (var device in deviceDateCounts)
            {
                foreach (var kvp in device.Value)
                {
                    if (!globalMinDate.HasValue || kvp.Key < globalMinDate.Value)
                        globalMinDate = kvp.Key;
                    if (!globalMaxDate.HasValue || kvp.Key > globalMaxDate.Value)
                        globalMaxDate = kvp.Key;
                }
            }
            if (globalMinDate.HasValue && globalMaxDate.HasValue)
            {
                chartArea.AxisX.Minimum = globalMinDate.Value.ToOADate();
                chartArea.AxisX.Maximum = globalMaxDate.Value.ToOADate();

                //Enable scrolling: show 30 days at a time
                chartArea.AxisX.ScrollBar.Enabled = true;
                chartArea.AxisX.ScaleView.Size = 30;
                chartArea.AxisX.ScaleView.SmallScrollSize = 7; // Adjust for desired scroll increment.
                chartArea.AxisX.ScaleView.Position = globalMinDate.Value.ToOADate();
            }
            else
            {
                chartArea.AxisX.ScaleView.ZoomReset();
            }

            //Create and add series for each device
            foreach (var device in sortedDevices)
            {
                string deviceName = device.Key;
                if (!deviceDateCounts.ContainsKey(deviceName))
                    continue;

                var sortedDates = deviceDateCounts[deviceName].OrderBy(kvp => kvp.Key).ToList();
                if (sortedDates.Count == 0)
                    continue;

                int totalCount = deviceTotalCounts[deviceName];
                string seriesName = $"{deviceName} ({totalCount})";
                Series series = new Series(seriesName)
                {
                    ChartType = SeriesChartType.Line,
                    XValueType = ChartValueType.Date,
                    BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6,
                    Legend = "Devices",
                    LegendText = seriesName
                };

                foreach (var dateCount in sortedDates)
                    series.Points.AddXY(dateCount.Key, dateCount.Value);

                foreach (DataPoint pt in series.Points)
                    pt.ToolTip = $"Device: {deviceName}\nDate: {DateTime.FromOADate(pt.XValue):MM/dd/yyyy}\nCount: {pt.YValues[0]}";

                chart.Series.Add(series);
            }

            //Force the chart to recalculate axes after all series are added
            chartArea.RecalculateAxesScale();
            //Finally, refresh the chart
            chart.Invalidate();
            chart.Update();
            chart.Refresh();
        }
    }
}