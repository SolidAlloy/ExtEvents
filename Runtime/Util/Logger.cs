namespace ExtEvents
{
    using UnityEngine;

    internal static class Logger
    {
        public static void LogWarning(string message)
        {
            if (PackageSettings.ShowInvocationWarning)
                Debug.LogWarning(message);
        }
    }
}