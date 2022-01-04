namespace ExtEvents
{
    using System;
    using JetBrains.Annotations;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false), BaseTypeRequired(typeof(BaseExtEvent))]
    public class EventArgumentsAttribute : Attribute
    {
        public readonly string[] ArgumentNames;

        public EventArgumentsAttribute(params string[] argumentNames)
        {
            ArgumentNames = argumentNames;
        }
    }
}