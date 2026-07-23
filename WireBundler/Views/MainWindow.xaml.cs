using Microsoft.Win32;
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
        private InputData? _inputData;
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
                SelectedFileTextBlock.Text = $"File: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
                InputStatusTextBlock.Text = "Input loaded successfully.";
                WireCountTextBlock.Text = $"Wire count: {_inputData.Radii.Count}";
                BundleDiameterTextBlock.Text = "Bundle diameter: not calculated yet";
                ArrangementStatusTextBlock.Text = "Arrangement status: input loaded, solver not implemented";

                MessageBox.Show("Radii: " + string.Join("\n", _inputData.Radii));
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
    }
}