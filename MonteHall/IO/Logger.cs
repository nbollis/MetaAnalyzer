using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonteCarlo.IO
{
    public static class Logger
    {
        private static readonly object _lock = new();

        public static void Log(string message, uint level = 0)
        {
            var intermediate = level == 0 ? "" : Enumerable.Range(0, (int)level).Select(_ => "  ").Aggregate((a, b) => a + b);
            lock (_lock)
            {
                Console.WriteLine($"[{DateTime.Now:dd HH:mm:ss}]: {intermediate}{message}");
            }
        }

        public static void LogError(string message)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:dd HH:mm:ss}] ERROR: {message}");
                Console.ResetColor();
            }
        }

        public static void LogWarning(string message)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now:dd HH:mm:ss}] WARNING: {message}");
                Console.ResetColor();
            }
        }
    }
}
