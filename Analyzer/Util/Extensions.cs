﻿using CsvHelper;
using Readers;

namespace Analyzer.Util
{
    public static class Extensions
    {
        public static string[] GetDirectories(this string directoryPath)
        {
            return Directory.GetDirectories(directoryPath);
        }

        public static bool ValidateMyColumn(this IReaderRow row)
        {
            // if I remove the HasHeaderRecord check here and set the CsvConfig HasHeaderRecord = false
            // the code all works I would have originally expected, e.g. header row gets ignored and all othe
            // rows are included.
            if (row.Configuration.HasHeaderRecord && row.Parser.Row == 1)
            {
                return true;
            }

            // Do other checks, for example:

            if (int.TryParse(row[0], out var _))
            {
                return true;
            }

            // Logging to objectForLogRef
            return false;
        }

        /// <summary>
        /// Calculate the rolling average of a list of doubles
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="windowSize"></param>
        /// <returns></returns>
        public static List<double> MovingAverage(this IEnumerable<double> numbers, int windowSize)
        {
            var result = new List<double>();

            for (int i = 0; i < numbers.Count() - windowSize + 1; i++)
            {
                var window = numbers.Skip(i).Take(windowSize);
                result.Add(window.Average());
            }

            return result;
        }

        /// <summary>
        /// Calculates a moving average and skips zeros, then replaces teh zero value with the moving average of teh window around it
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="windowSize"></param>
        /// <returns></returns>
        public static List<double> MovingAverageZeroFill(this IEnumerable<double> numbers, int windowSize)
        {
            var result = new List<double>();

            var enumerable = numbers as double[] ?? numbers.ToArray();
            for (int i = 0; i < enumerable.Count() - windowSize + 1; i++)
            {
                var window = enumerable.Skip(i).Take(windowSize).ToList();
                if (window.Contains(0))
                {
                    var average = window.Where(p => p != 0).Average();
                    result.Add(average);
                }
                else
                {
                    result.Add(window.Average());
                }
            }

            return result;
        }


        public static bool TryGetFile<T>(this string filePath, out T? result) 
            where T : IResultFile, new()
        {
            result = default(T);
            if (File.Exists(filePath))
            {
                try
                {
                    result = FileReader.ReadFile<T>(filePath);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }
    }
}
