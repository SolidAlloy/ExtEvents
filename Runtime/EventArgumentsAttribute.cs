namespace ExtEvents
{
    using System;
    using JetBrains.Annotations;

    /// <summary>
    /// An optional attribute for <see cref="ExtEvent"/> that provides the names of the arguments it represents when
    /// invoked, so that those names are shown in the editor UI. It allows users to better understand which arguments
    /// to assign to a listener when adding a listener through editor UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false), BaseTypeRequired(typeof(BaseExtEvent))]
    public class EventArgumentsAttribute : Attribute
    {
        /// <summary>
        /// The names of the arguments passed to ExtEvent.Invoke().
        /// </summary>
        public readonly string[] ArgumentNames;

        /// <summary>
        /// Creates an instance of <see cref="EventArgumentsAttribute"/>.
        /// </summary>
        /// <param name="argumentNames">The names of the arguments passed to ExtEvent.Invoke().</param>
        public EventArgumentsAttribute(params string[] argumentNames)
        {
            ArgumentNames = argumentNames;
        }
    }
}