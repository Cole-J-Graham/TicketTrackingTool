using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System.Xml.Linq;
using System;
using System.Linq;

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

        public static void calculateCategory()
        {

        }
    }
}