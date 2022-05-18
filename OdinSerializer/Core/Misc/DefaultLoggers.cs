namespace ExtEvents.OdinSerializer
{
    using UnityEngine;

    /// <summary>
    /// Defines default loggers for serialization and deserialization. This class and all of its loggers are thread safe.
    /// </summary>
    public static class DefaultLoggers
    {
        private static readonly object LOCK = new object();
        private static volatile ILogger unityLogger;

        /// <summary>
        /// The default logger - usually this is <see cref="UnityLogger"/>.
        /// </summary>
        public static ILogger DefaultLogger => UnityLogger;

        /// <summary>
        /// Logs messages using Unity's <see cref="UnityEngine.Debug"/> class.
        /// </summary>
        public static ILogger UnityLogger
        {
            get
            {
                if (unityLogger == null)
                {
                    lock (LOCK)
                    {
                        if (unityLogger == null)
                        {
                            unityLogger = new CustomLogger(Debug.LogWarning, Debug.LogError, Debug.LogException);
                        }
                    }
                }

                return unityLogger;
            }
        }
    }
}