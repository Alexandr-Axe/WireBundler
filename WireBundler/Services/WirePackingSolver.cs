using System;
using System.Collections.Generic;
using System.Text;
using WireBundler.Models;

namespace WireBundler.Services
{
    public class WirePackingSolver
    {
        public BundleResult Solve(InputData inputData)
        {
            if (inputData == null || inputData.Radii.Count == 0)
                throw new ArgumentException("Input data is null or empty");

            List<double> sortedRadii = inputData.Radii
                .OrderByDescending(r => r)
                .ToList();

            BundleResult result = new();
            double currentX = 0.0;

            foreach (double radius in sortedRadii)
            {
                result.Wires.Add(new WirePlacement
                {
                    Radius = radius,
                    X = currentX + radius,
                    Y = 0
                });

                currentX += radius * 2.0;
            }

            // abych měl střed v [0, 0]
            double centerX = result.Wires.Average(w => w.X);

            foreach (WirePlacement wire in result.Wires)
            {
                wire.X -= centerX;
            }

            result.BundleRadius = result.Wires
                .Max(w => Math.Sqrt(w.X * w.X + w.Y * w.Y) + w.Radius);

            return result;
        }
    }
}
