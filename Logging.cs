using System;
using System.Diagnostics;
using System.Reflection;
using BepInEx.Logging;

namespace WalkSimulator
{
    public static class Logging
    {
        private static ManualLogSource logger;

        public static int DebuggerLines = 20;

        public static void Init()
        {
            logger = Logger.CreateLogSource("WalkSimulator");
        }

        public static void Exception(Exception e)
        {
            var method = new StackTrace().GetFrame(1).GetMethod();
            logger.LogWarning($"({method.ReflectedType.Name}.{method.Name}()) {e.Message} {e.StackTrace}");
        }

        public static void Fatal(params object[] content)
        {
            var method = new StackTrace().GetFrame(1).GetMethod();
            logger.LogFatal($"({method.ReflectedType.Name}.{method.Name}()) {string.Join(" ", content)}");
        }

        public static void Warning(params object[] content)
        {
            var method = new StackTrace().GetFrame(1).GetMethod();
            logger.LogWarning($"({method.ReflectedType.Name}.{method.Name}()) {string.Join(" ", content)}");
        }

        public static void Info(params object[] content)
        {
            var method = new StackTrace().GetFrame(1).GetMethod();
            logger.LogInfo($"({method.ReflectedType.Name}.{method.Name}()) {string.Join(" ", content)}");
        }

        public static void Debug(params object[] content)
        {
            var method = new StackTrace().GetFrame(1).GetMethod();
            logger.LogDebug($"({method.ReflectedType.Name}.{method.Name}()) {string.Join("  ", content)}");
        }

        public static void Debugger(params object[] content)
        {
            Debug(content);
        }

        public static string PrependTextToLog(string log, string text)
        {
            log = text + "\n" + log;
            var lines = log.Split('\n');
            if (lines.Length > DebuggerLines)
            {
                log = string.Join("\n", lines, 0, DebuggerLines);
            }
            return log;
        }
    }
}
