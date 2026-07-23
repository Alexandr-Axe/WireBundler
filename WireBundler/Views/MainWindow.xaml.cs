using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using WireBundler.Models;
using WireBundler.Services;

namespace WireBundler.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly InputParser _inputParser = new();
        private readonly WirePackingSolver _wirePackingSolver = new();

        private InputData? _inputData;
        private BundleResult? _bundleResult;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                _inputData = _inputParser.LoadFromFile(openFileDialog.FileName);
                _bundleResult = _wirePackingSolver.Solve(_inputData);

                SelectedFileTextBlock.Text = $"File: {Path.GetFileName(openFileDialog.FileName)}";
                InputStatusTextBlock.Text = "Input loaded successfully.";
                WireCountTextBlock.Text = $"Wire count: {_inputData.Radii.Count}";
                BundleDiameterTextBlock.Text = $"Bundle diameter: {_bundleResult.BundleDiameter:F2} mm";
                ArrangementStatusTextBlock.Text = "Arrangement status: initial arrangement created";
                WirePositionsTextBox.Text = BuildWirePositionsText(_bundleResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load input file.\n\n{ex.Message}",
                    "Input Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                _inputData = null;
                SelectedFileTextBlock.Text = "No file loaded yet.";
                InputStatusTextBlock.Text = "Input loading failed.";
                WireCountTextBlock.Text = "Wire count: -";
                BundleDiameterTextBlock.Text = "Bundle diameter: -";
                ArrangementStatusTextBlock.Text = "Arrangement status: waiting for implementation";
            }
        }

        private string BuildWirePositionsText(BundleResult bundleResult)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bundleResult.Wires.Count; i++)
            {
                var wire = bundleResult.Wires[i];
                sb.AppendLine(
                    $"Wire {i + 1}: r={wire.Radius:F2}, x={wire.X:F2}, y={wire.Y:F2}");
            }

            return sb.ToString();
        }
    }
}