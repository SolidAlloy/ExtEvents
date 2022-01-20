namespace ExtEvents
{
    using System;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ExtEventListener : Attribute { }
}