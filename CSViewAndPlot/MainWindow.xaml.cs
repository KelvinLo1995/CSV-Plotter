using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.Collections.Generic;
using System.Windows.Controls;

namespace CSViewPlotTool
{
    public partial class MainWindow : Window
    {
        private PlotModel lastPlotModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "CSV files (.csv)|*.csv" };
            if (openFileDialog.ShowDialog() == true)
            {
                LoadCsv(openFileDialog.FileName);
            }
        }

        private void LoadCsv(string path)
        {
            DataTable dt = new DataTable();
            using (var reader = new StreamReader(path))
            {
                var headers = reader.ReadLine().Split(',');
                foreach (var header in headers)
                {
                    dt.Columns.Add(header);
                }
                while (!reader.EndOfStream)
                {
                    var rows = reader.ReadLine().Split(',');
                    dt.Rows.Add(rows);
                }
            }
            dataGrid.ItemsSource = dt.DefaultView;

            // Populate ComboBox and ListBox with column names
            xAxisComboBox.ItemsSource = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            yAxisListBox.ItemsSource = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        }

        private void PlotButton_Click(object sender, RoutedEventArgs e)
        {
            var xColumn = xAxisComboBox.SelectedItem?.ToString();
            var yColumns = yAxisListBox.SelectedItems.Cast<string>().ToList();

            if (!yColumns.Any())
            {
                MessageBox.Show("Please select at least one Y-axis column.");
                return;
            }

            lastPlotModel = PlotData(xColumn, yColumns);
        }

        private PlotModel PlotData(string xColumn, List<string> yColumns)
        {
            string szTilte = "";

            if (!string.IsNullOrEmpty(xColumn))
            {
                szTilte += xColumn + " vs ";
            }

            for (int i = 0; i < yColumns.Count - 1; i++)
            {
                szTilte += yColumns[i] + " ,";
            }

            szTilte += yColumns[yColumns.Count - 1];


            var plotModel = new PlotModel { Title = szTilte};

            foreach (var yColumn in yColumns)
            {
                var series = new ScatterSeries { Title = yColumn };
                if (string.IsNullOrEmpty(xColumn))
                {
                    // Use the index for X values if no X-axis is selected
                    int index = 0;
                    foreach (DataRowView row in (DataView)dataGrid.ItemsSource)
                    {
                        double y;
                        if (double.TryParse(row[yColumn]?.ToString(), out y))
                        {
                            series.Points.Add(new ScatterPoint(index++, y));
                        }
                    }
                }
                else
                {
                    series.Title = xColumn + " vs " + yColumn; 
                    foreach (DataRowView row in (DataView)dataGrid.ItemsSource)
                    {
                        double x, y;
                        if (double.TryParse(row[xColumn]?.ToString(), out x) &&
                            double.TryParse(row[yColumn]?.ToString(), out y))
                        {
                            series.Points.Add(new ScatterPoint(x, y));
                        }
                    }
                }

                plotModel.Series.Add(series);
            }

            var plotView = new PlotView { Model = plotModel };
            var plotWindow = new Window { Title = "Plot", Content = plotView, Width = 800, Height = 600 };
            plotWindow.ShowDialog();

            return plotModel;
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "CSV files (.csv)|*.csv" };
            if (saveFileDialog.ShowDialog() == true)
            {
                ExportCsv(saveFileDialog.FileName);
            }
        }

        private void ExportCsv(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                // Write header
                foreach (DataGridColumn column in dataGrid.Columns)
                {
                    writer.Write(column.Header + ",");
                }
                writer.WriteLine();

                // Write rows
                foreach (DataRowView row in (DataView)dataGrid.ItemsSource)
                {
                    foreach (var item in row.Row.ItemArray)
                    {
                        writer.Write(item + ",");
                    }
                    writer.WriteLine();
                }
            }
        }

        private void SavePlotButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "PNG files (.png)|*.png" };
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    var exporter = new PngExporter { Width = 800, Height = 600, Resolution = 96 };
                    exporter.Export(lastPlotModel, stream);
                }

                MessageBox.Show("Plot saved successfully!");
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}