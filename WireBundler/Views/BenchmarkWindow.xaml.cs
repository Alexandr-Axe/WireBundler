using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WireBundler.Services;

namespace WireBundler.Views
{
    public partial class BenchmarkWindow : Window
    {
        private (int fallbackDirections, int survivors, double fineOffset)? _bestConfig;
        private double _bestDiameter = double.MaxValue;

        public BenchmarkWindow()
        {
            InitializeComponent();

            DataContext = BENCHMARK.CurrentConfig;
        }

        private async void RunBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            string inputFilePath = BenchmarkInputFileTextBox.Text;
            string orderLabel = "DESC";

            if (string.IsNullOrWhiteSpace(inputFilePath))
            {
                BenchmarkStatusTextBlock.Text = "Please specify input file path.";
                return;
            }

            if (!File.Exists(inputFilePath))
            {
                BenchmarkStatusTextBlock.Text = "Input file does not exist.";
                return;
            }

            RunBenchmarkButton.IsEnabled = false;
            BenchmarkProgressBar.Value = 0;
            BenchmarkStatusTextBlock.Text = "Benchmark running...";
            EstimatedTotalTimeTextBlock.Text = "-";
            BestConfigTextBox.Text = string.Empty;
            _bestConfig = null;
            _bestDiameter = double.MaxValue;

            try
            {
                await Task.Run(() =>
                {
                    BENCHMARK.RunSolverBenchmark(
                        inputFilePath,
                        orderLabel,
                        (done, total, estimatedSeconds) =>
                        {
                            double percentage = done * 100.0 / total;

                            Dispatcher.Invoke(() =>
                            {
                                BenchmarkProgressBar.Value = percentage;

                                if (estimatedSeconds.HasValue)
                                {
                                    EstimatedTotalTimeTextBlock.Text =
                                        $"{estimatedSeconds.Value / 60.0:F1} min (approx)";
                                }

                                BenchmarkStatusTextBlock.Text =
                                    $"Benchmark running... {done} / {total} ({percentage:F1}%).";
                            });
                        },
                        (config, diameter, elapsedMs) =>
                        {
                            if (diameter < _bestDiameter)
                            {
                                _bestDiameter = diameter;
                                _bestConfig = config;

                                Dispatcher.Invoke(() =>
                                {
                                    BestConfigTextBox.Text =
                                        $"Diameter: {_bestDiameter:F2} mm\n" +
                                        $"Fallback directions: {config.fallbackDirections}\n" +
                                        $"Coarse survivors: {config.survivors}\n" +
                                        $"Fine offset: {config.fineOffset:F1}°\n" +
                                        $"Ordering: {orderLabel}\n" +
                                        $"Last elapsed: {elapsedMs} ms";
                                });
                            }
                        });
                });

                BenchmarkStatusTextBlock.Text = "Benchmark finished.";
            }
            catch (Exception ex)
            {
                BenchmarkStatusTextBlock.Text = "Benchmark failed.";
                MessageBox.Show(
                    $"Benchmark failed.\n\n{ex.Message}",
                    "Benchmark Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                RunBenchmarkButton.IsEnabled = true;
            }
        }
    }
}