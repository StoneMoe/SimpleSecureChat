using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Log
{
    public class Logger
    {
        public static void Error(string format, params object[] args)
        {
            Write("ERRO", format, args);
        }
        public static void Warn(string format, params object[] args)
        {
            Write("WARN", format, args);
        }
        public static void Info(string format, params object[] args)
        {
            Write("INFO", format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            Write("DBUG", format, args);
        }

        private static string Prefix(string level)
        {
            return $"[{DateTime.Now}][{level}] ";
        }
        private static void Write(string level, string format, params object[] args)
        {
            Console.WriteLine(Prefix(level) + format, args);
        }

    }
}
