using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using WireBundler.Models;

namespace WireBundler.Services
{
    public class InputParser
    {
        public InputData LoadFromFile(string filePath)
        {
            InputData inputData = new();
            string[] lines = File.ReadAllLines(filePath);
            string trimmedLine = string.Empty;

            if(lines.Length <= 0)
                throw new ArgumentException("File is empty");

            foreach (string line in lines)
            {
                trimmedLine = line.Trim();

                if(string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if(!double.TryParse(trimmedLine, NumberStyles.Float, CultureInfo.InvariantCulture, out double radius))
                    throw new FormatException($"Invalid radius value: {trimmedLine}");

                if(radius <= 0)
                    throw new ArgumentOutOfRangeException($"Radius must be positive: {trimmedLine}");

                inputData.Radii.Add(radius);
            }

            if(inputData.Radii.Count == 0)
                throw new ArgumentException("No valid radius values found in the file");

            return inputData;
        }
    }
}
