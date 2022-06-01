namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using JetBrains.Annotations;
    using SolidUtilities;
    using UnityEngine;

    /// <summary>
    /// An event whose listeners can be configured through editor UI.
    /// </summary>
    [Serializable]
    public abstract class BaseExtEvent
    {
        [SerializeField] internal PersistentListener[] _persistentListeners;

        /// <summary>
        /// A list of the persistent listeners of this event. <see cref="PersistentListener"/> is a listener that is configured in editor UI.
        /// </summary>
        [PublicAPI]
        public IReadOnlyList<PersistentListener> PersistentListeners => _persistentListeners;

        [SerializeField] internal bool Expanded = true;

        protected abstract Type[] EventParamTypes { get; }

        internal abstract Delegate _dynamicListeners { get; }

        /// <summary>
        /// Prepares an event for invocation, so that it takes less time to invoke later.
        /// </summary>
        public void Initialize()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0; index < _persistentListeners.Length; index++)
            {
                _persistentListeners[index].Initialize();
            }
        }

        /// <summary>
        /// Adds a new persistent listener.
        /// </summary>
        /// <exception cref="MethodNotEligibleException">The method passed is not eligible for invoking by <see cref="ExtEvent"/> with these generic arguments.</exception>
        /// <exception cref="ArgumentException">The number of arguments passed does not match the number of parameters the method takes in.</exception>
        /// <exception cref="ArgumentTypeMismatchException">A type of the argument passed does not match the type of the parameter taken in by the method.</exception>
        /// <exception cref="ArgumentIndexException">The index of a dynamic argument is either out of range of the arguments passed in ExtEvent.Invoke() or the type of the parameter by this index in ExtEvent.Invoke() does not match the type of the argument.</exception>
        public void AddPersistentListener(PersistentListener persistentListener)
        {
            if (!MethodIsEligible(persistentListener.MethodInfo, EventParamTypes, true, true))
                throw new MethodNotEligibleException("The method of persistent listener is not eligible for adding to this event");

            CheckArguments(persistentListener.MethodInfo, ref persistentListener._persistentArguments, EventParamTypes);
            ArrayHelper.Add(ref _persistentListeners, persistentListener);
        }

        /// <summary>
        /// Removes a persistent listener at the index.
        /// </summary>
        /// <param name="index">The index of a persistent listener to remove.</param>
        [PublicAPI]
        public void RemovePersistentListenerAt(int index) => ArrayHelper.RemoveAt(ref _persistentListeners, index);

        /// <summary>
        /// Removes the specified persistent listener from the list of listeners.
        /// </summary>
        /// <param name="listener">The listener to remove.</param>
        /// <returns>Whether the listener was found in the list.</returns>
        [PublicAPI]
        public bool RemovePersistentListener(PersistentListener listener) => ArrayHelper.Remove(ref _persistentListeners, listener);

        #region Verify Persistent Argument

        private static void CheckArguments(MethodInfo method, ref PersistentArgument[] arguments, Type[] eventParamTypes)
        {
            var parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                arguments = Array.Empty<PersistentArgument>();
                return;
            }

            if (arguments.Length < parameters.Length)
                throw new ArgumentException($"The number of arguments passed is {arguments.Length} while the method needs {parameters.Length} arguments", nameof(arguments));

            if (arguments.Length > parameters.Length)
                Array.Resize(ref arguments, parameters.Length);

            for (int i = 0; i < parameters.Length; i++)
            {
                var argument = arguments[i];
                var parameter = parameters[i];
                CheckArgument(ref argument, parameter, i, eventParamTypes);
            }
        }

        private static void CheckArgument(ref PersistentArgument argument, ParameterInfo parameter, int argumentIndex, Type[] eventParamTypes)
        {
            if (argument._targetType.Type != parameter.ParameterType)
                throw new ArgumentTypeMismatchException($"The passed argument at index {argumentIndex} was assigned a wrong type {argument._targetType} that does not match the parameter type of the method: {parameter.ParameterType}.");

            var matchingParamIndices = GetMatchingParamIndices(eventParamTypes, argument._targetType);

            if (argument._isSerialized)
            {
                argument._canBeDynamic = matchingParamIndices.Count != 0;
            }
            else
            {
                if (argument._index < 0 || argument._index >= eventParamTypes.Length)
                    throw new ArgumentIndexException($"The argument index {argument._index} is out of bounds of the number of parameter types of the event: {eventParamTypes.Length}");

                if (!matchingParamIndices.Contains(argument._index))
                    throw new ArgumentIndexException($"The argument is dynamic and was assigned an index of {argument._index} but an event parameter at that index is of a different type: {eventParamTypes[argument._index]}");
            }
        }

        private static List<int> GetMatchingParamIndices(Type[] eventParamTypes, Type argumentType)
        {
            var foundIndices = new List<int>(1);

            for (int i = 0; i < eventParamTypes.Length; i++)
            {
                if (eventParamTypes[i] == argumentType)
                    foundIndices.Add(i);
            }

            return foundIndices;
        }

        #endregion

        #region MethodIsEligible

        /// <summary>
        /// Check if the method is eligible for adding as a persistent listener to <see cref="ExtEvent"/>.
        /// </summary>
        /// <param name="method">A method info that you want to check eligibility of.</param>
        /// <param name="eventParamTypes">The generic argument types of the event you want to add a method to.</param>
        /// <param name="allowInternal">Whether you want the event to allow internal methods.</param>
        /// <param name="allowPrivate">Whether you want the event to allow private and protected methods.</param>
        /// <returns>True if the method is eligible.</returns>
        public static bool MethodIsEligible(MethodInfo method, Type[] eventParamTypes, bool allowInternal, bool allowPrivate)
        {
            var methodParams = method.GetParameters();

            return IsEligibleByVisibility(method, allowInternal, allowPrivate)
                   && !method.Name.IsPropertyGetter()
                   && !IsMethodPure(method)
                   && IsEligibleByParameters(methodParams, eventParamTypes);
        }

        private static bool IsEligibleByVisibility(MethodInfo method, bool allowInternal, bool allowPrivate)
        {
            if (method.IsPublic)
                return true;

            if (method.IsAssembly && allowInternal)
                return true;

            if ((method.IsPrivate || method.IsFamily) && allowPrivate)
                return true;

            return method.HasAttribute<ExtEventListener>();
        }

        private static bool IsMethodPure(MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
                return false;

            return method.HasAttribute<PureAttribute>() ||
                   method.HasAttribute<System.Diagnostics.Contracts.PureAttribute>();
        }

        private static bool IsEligibleByParameters(ParameterInfo[] parameters, Type[] eventParamTypes)
        {
            return parameters.Length <= 4 && parameters.All(param => ParamCanBeUsed(param.ParameterType, eventParamTypes));
        }

        private static bool ParamCanBeUsed(Type paramType, Type[] eventParamTypes)
        {
            return paramType.IsUnitySerializable() || ArgumentTypeIsInList(paramType, eventParamTypes);
        }

        private static bool ArgumentTypeIsInList(Type argType, Type[] eventParamTypes)
        {
            return eventParamTypes.Any(eventParamType => eventParamType.IsAssignableFrom(argType) || Converter.ExistsForTypes(eventParamType, argType));
        }

        #endregion
    }

    public class MethodNotEligibleException : ArgumentException
    {
        public MethodNotEligibleException(string message) : base(message) { }
    }

    public class ArgumentTypeMismatchException : ArgumentException
    {
        public ArgumentTypeMismatchException(string message) : base(message) { }
    }

    public class ArgumentIndexException : ArgumentException
    {
        public ArgumentIndexException(string message) : base(message) { }
    }
}