namespace ExtEvents
{
    internal static class StringExtensions
    {
        public static bool IsPropertySetter(this string methodName)
        {
            return methodName.StartsWith("set_");
        }

        public static bool IsPropertyGetter(this string methodName)
        {
            return methodName.StartsWith("get_");
        }
    }
}