using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AbeAttributes
{
    public static class PopToConsoleRuntime
    {
        public static void OnInvoke(
            string typeName,
            string methodName)
        {
            Debug.Log(
                $"[PopToConsole] Invoke: " +
                $"{typeName}.{methodName}");
        }

        public static void OnGet(
            string typeName,
            string propertyName,
            object value)
        {
            Debug.Log(
                $"[PopToConsole] Get: " +
                $"{typeName}.{propertyName} = " +
                $"{value}");
        }

        public static void OnSet(
            string typeName,
            string propertyName,
            object value)
        {
            Debug.Log(
                $"[PopToConsole] Set: " +
                $"{typeName}.{propertyName} = " +
                $"{value}");
        }

        public static void OnManual(
            string typeName,
            string memberName)
        {
            Debug.Log(
                $"[PopToConsole] Manual: " +
                $"{typeName}.{memberName}");
        }

        public static void OnManual(
            string typeName,
            string memberName,
            object value)
        {
            Debug.Log(
                $"[PopToConsole] Manual: " +
                $"{typeName}.{memberName} = " +
                $"{value}");
        }

        public static void Break()
        {
            Debug.Log("Break Point Reached");
            Debug.Break();
        }
    }
}