using System;
using System.Collections.Generic;

namespace UtilmeSdpTransform 
{
    public static class UtilityExtensions
    {


        private static int ToIntIfInt(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        public static List<List<SimulcastFormat>> ParseSimulcastStreamList(string str)
        {
            var simulcastStreams = new List<List<SimulcastFormat>>();

            foreach (var stream in str.Split(';'))
            {
                var formats = new List<SimulcastFormat>();

                foreach (var format in stream.Split(','))
                {
                    int scid;
                    bool paused = false;

                    if (format[0] != '~')
                    {
                        scid = ToIntIfInt(format);
                    }
                    else
                    {
                        scid = ToIntIfInt(format.Substring(1));
                        paused = true;
                    }

                    formats.Add(new SimulcastFormat
                    {
                        Scid = scid,
                        Paused = paused
                    });
                }

                simulcastStreams.Add(formats);
            }

            return simulcastStreams;
        }

    }

    public class SimulcastFormat
    {
        public int Scid { get; set; }
        public bool Paused { get; set; }
    }




}


