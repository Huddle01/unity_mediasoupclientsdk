using System;
using System.Text.RegularExpressions;

namespace Mediasoup 
{
    public class ScalabilityMode
    {
        public int spatialLayers { get; set; }
        public int temporalLayers { get; set; }
    }

    public static class ScalabilityModeParser
    {
        private static readonly Regex ScalabilityModeRegex = new Regex(@"^[LS]([1-9]\d{0,1})T([1-9]\d{0,1})");

        public static ScalabilityMode Parse(string scalabilityMode)
        {
            var match = ScalabilityModeRegex.Match(scalabilityMode ?? "");

            if (match.Success)
            {
                return new ScalabilityMode
                {
                    spatialLayers = int.Parse(match.Groups[1].Value),
                    temporalLayers = int.Parse(match.Groups[2].Value),
                };
            }
            else
            {
                return new ScalabilityMode
                {
                    spatialLayers = 1,
                    temporalLayers = 1,
                };
            }
        }
    }


}

