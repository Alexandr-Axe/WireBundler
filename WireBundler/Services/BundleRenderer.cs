using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Text;
using System.Windows.Controls;
using System.Windows.Shapes;
using WireBundler.Models;

namespace WireBundler.Services
{
    public class BundleRenderer
    {
        private const double Padding = 20.0;
        public void Render(Canvas canvas, BundleResult bundleResult)
        {
            if (bundleResult == null || bundleResult.Wires.Count == 0 || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0)
                throw new ArgumentException("Invalid bundle result or canvas size.");

            double bundleRadius = bundleResult.BundleRadius;
            double availableWidth = Math.Max(1, canvas.ActualWidth - 2 * Padding);
            double availableHeight = Math.Max(1, canvas.ActualHeight - 2 * Padding);

            double scaleX = availableWidth / (2 * bundleRadius);
            double scaleY = availableHeight / (2 * bundleRadius);
            double scale = Math.Min(scaleX, scaleY);

            double canvasCenterX = canvas.ActualWidth / 2.0;
            double canvasCenterY = canvas.ActualHeight / 2.0;

            DrawBundleBoundary(canvas, bundleRadius, scale, canvasCenterX, canvasCenterY);

            foreach (WirePlacement wire in bundleResult.Wires)
            {
                DrawWire(canvas, wire, scale, canvasCenterX, canvasCenterY);
            }
        }

        private void DrawWire(Canvas canvas, WirePlacement wire, double scale, double canvasCenterX, double canvasCenterY)
        {
            double radius = wire.Radius * scale;
            double diameter = radius * 2.0;

            double x = canvasCenterX + wire.X * scale;
            double y = canvasCenterY- wire.Y * scale;

            Ellipse wireEllipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                Stroke = Brushes.MediumSlateBlue,
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(80, 123, 104, 238))
            };

            Canvas.SetLeft(wireEllipse, x - radius);
            Canvas.SetTop(wireEllipse, y - radius);

            canvas.Children.Add(wireEllipse);
        }

        private void DrawBundleBoundary(Canvas canvas, double bundleRadius, double scale, double canvasCenterX, double canvasCenterY)
        {
            double diameter = bundleRadius * 2.0 * scale;

            Ellipse bundleEllipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                Stroke = Brushes.ForestGreen,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(bundleEllipse, canvasCenterX - diameter / 2.0);
            Canvas.SetTop(bundleEllipse, canvasCenterY - diameter / 2.0);

            canvas.Children.Add(bundleEllipse);
        }
    }
}
