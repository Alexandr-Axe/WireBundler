using System;
using System.Collections.Generic;
using System.Text;

namespace WireBundler.Models
{
    /// <summary>
    /// Represents the input data for the wire bundling problem, containing a list of wire radii.
    /// </summary>
    public class InputData
    {
        public List<double> Radii { get; set; } = new();
    }
}
