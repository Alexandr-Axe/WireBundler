using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using WireBundler.Models;

namespace WireBundler.Services
{
    /// <summary>
    /// Parses input data from a text file and loads it into an InputData object.
    /// </summary>
    public class InputParser
    {
        /// <summary>
        /// Loads input data from a specified text file and returns an InputData object containing the parsed radii values.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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
