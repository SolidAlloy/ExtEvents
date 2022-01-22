namespace ExtEvents
{
    using System;

    /// <summary>
    /// An attribute added to a non-public method that you want to use in <see cref="ExtEvent"/>.
    /// By default, only public methods are shown in the method dropdown.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ExtEventListener : Attribute { }
}