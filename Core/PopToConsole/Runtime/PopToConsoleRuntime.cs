using System.Diagnostics;
using UnityEngine;

namespace AbeAttributes
{
    public static class PopToConsoleRuntime
    {
        public static void OnInvoke(
            string typeName,
            string methodName)
        {
            UnityEngine.Debug.Log(
                $"[PopToConsole] Invoke: " +
                $"{typeName}.{methodName}");

            LogCallStack();
        }

        public static void OnManual(
            string typeName,
            string methodName)
        {
            UnityEngine.Debug.Log(
                $"[PopToConsole] Manual: " +
                $"{typeName}.{methodName}");
        }

        private static void LogCallStack()
        {
            StackTrace stackTrace =
                new StackTrace(
                    2,
                    true);

            UnityEngine.Debug.Log(
                "[PopToConsole] Call Stack:\n" +
                stackTrace);
        }
    }
}
