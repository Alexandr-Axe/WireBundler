using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WireBundler.Services;

namespace WireBundler.Views
{
    /// <summary>
    /// WPF window that provides UI controls to run the solver benchmark, show progress, and collect results.
    /// Allows running benchmark sweeps and exporting aggregated results.
    /// </summary>
    public partial class BenchmarkWindow : Window
    {
        private static readonly string[] AllOrderLabels = { "DESC", "ASC", "ALT" };

        private (int fallbackDirections, int survivors, double fineOffset)? _bestConfig;
        private double _bestDiameter = double.MaxValue;
        private string _bestOrderLabel = string.Empty;
        string bestConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BestBenchmarkConfig.txt");
        string allResultsCsvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchmarkAllResults.csv");

        public BenchmarkWindow()
        {
            InitializeComponent();

            DataContext = BENCHMARK.Config;
        }

        private async void RunBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            string inputFilePath = BenchmarkInputFileTextBox.Text;

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

            string[] orderLabelsToRun = RunAllOrdersCheckBox.IsChecked == true
                ? AllOrderLabels
                : new[] { "DESC" };

            RunBenchmarkButton.IsEnabled = false;
            BenchmarkProgressBar.Value = 0;
            BenchmarkStatusTextBlock.Text = "Benchmark running...";
            BestConfigTextBox.Text = string.Empty;
            _bestConfig = null;
            _bestDiameter = double.MaxValue;
            _bestOrderLabel = string.Empty;

            try
            {
                File.WriteAllText(allResultsCsvPath, "OrderLabel,FallbackDirections,Survivors,FineOffset,Diameter,ElapsedMs,IsBestSoFar\n");
            }
            catch (Exception ex)
            {
                AppLog.Write(LogLevel.WAR, $"Failed to initialize benchmark results CSV: {ex.Message}");
            }

            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < orderLabelsToRun.Length; i++)
                    {
                        string orderLabel = orderLabelsToRun[i];
                        int orderIndexCaptured = i;

                        BENCHMARK.RunSolverBenchmark(
                            inputFilePath,
                            orderLabel,
                            (done, total) =>
                            {
                                double percentageWithinOrder = done * 100.0 / total;
                                double overallPercentage = (orderIndexCaptured * 100.0 + percentageWithinOrder) / orderLabelsToRun.Length;

                                Dispatcher.Invoke(() =>
                                {
                                    BenchmarkProgressBar.Value = overallPercentage;

                                    BenchmarkStatusTextBlock.Text =
                                        $"Benchmark running... order '{orderLabel}' " +
                                        $"({orderIndexCaptured + 1}/{orderLabelsToRun.Length}): " +
                                        $"{done} / {total} ({percentageWithinOrder:F1}%).";
                                });
                            },
                            (config, diameter, elapsedMs) =>
                            {
                                bool isNewBest = diameter < _bestDiameter;
                                bool isTieWithBest = !isNewBest && Math.Abs(diameter - _bestDiameter) < 1e-6;

                                try
                                {
                                    string csvLine =
                                    $"{orderLabel},{config.fallbackDirections},{config.survivors}," +
                                    $"{config.fineOffset.ToString("F1", CultureInfo.InvariantCulture)}," +
                                    $"{diameter.ToString("F4", CultureInfo.InvariantCulture)}," +
                                    $"{elapsedMs},{(isNewBest ? "NEW_BEST" : isTieWithBest ? "TIE" : "")}\n";

                                    File.AppendAllText(allResultsCsvPath, csvLine);
                                }
                                catch (Exception ex)
                                {
                                    AppLog.Write(LogLevel.WAR, $"Failed to append benchmark result to CSV: {ex.Message}");
                                }

                                Dispatcher.Invoke(() =>
                                {
                                    CurrentConfigTextBlock.Text =
                                        $"Testing [{orderLabel}]: fallback={config.fallbackDirections}, survivors={config.survivors}, " +
                                        $"fineOffset={config.fineOffset:F1} degrees  ->  diameter={diameter:F2} mm ({elapsedMs} ms)";
                                });

                                if (isNewBest)
                                {
                                    _bestDiameter = diameter;
                                    _bestConfig = config;
                                    _bestOrderLabel = orderLabel;

                                    string bestConfigText =
                                        $"Diameter: {_bestDiameter.ToString("F2", CultureInfo.InvariantCulture)} mm\n" +
                                        $"FallbackDirectionCount: {config.fallbackDirections}\n" +
                                        $"CoarseSurvivorCount: {config.survivors}\n" +
                                        $"FineAngularOffsetDegrees: {config.fineOffset.ToString("F1", CultureInfo.InvariantCulture)}\n" +
                                        $"OrderLabel: {orderLabel}\n" +
                                        $"LastElapsedMs: {elapsedMs}\n";

                                    Dispatcher.Invoke(() =>
                                    {
                                        BestConfigTextBox.Text = bestConfigText;
                                    });

                                    try
                                    {
                                        File.WriteAllText(bestConfigFilePath, bestConfigText);
                                    }
                                    catch (Exception ex)
                                    {
                                        AppLog.Write(LogLevel.WAR, $"Failed to write best benchmark config to file: {ex.Message}");
                                    }
                                }
                            });
                    }
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