using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonteCarlo.IO
{
    public static class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
        public static void LogError(string message)
        {
            Console.WriteLine($"{DateTime.Now}: ERROR: {message}");
        }
        public static void LogWarning(string message)
        {
            Console.WriteLine($"{DateTime.Now}: WARNING: {message}");
        }
        public static void LogInfo(string message)
        {
            Console.WriteLine($"{DateTime.Now}: INFO: {message}");
        }
    }
}
