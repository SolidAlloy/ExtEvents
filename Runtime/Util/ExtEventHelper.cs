namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using JetBrains.Annotations;
    using SolidUtilities;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

    public static class ExtEventHelper
    {
        #region MethodIsEligible

        public static bool MethodIsEligible(MethodInfo method, Type[] eventParamTypes, bool allowInternal, bool allowPrivate)
        {
            return IsEligibleByVisibility(method, allowInternal, allowPrivate) 
                   && !method.Name.IsPropertyGetter()
                   && !IsMethodPure(method) 
                   && method.GetParameters().All(param => ParamCanBeUsed(param.ParameterType, eventParamTypes));
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

        private static bool ParamCanBeUsed(Type paramType, Type[] eventParamTypes)
        {
            return paramType.IsUnitySerializable() || ArgumentTypeIsInList(paramType, eventParamTypes);
        }
        
        private static bool ArgumentTypeIsInList(Type argType, Type[] eventParamTypes)
        {
            return eventParamTypes.Any(eventParamType => eventParamType.IsAssignableFrom(argType));
        }

        #endregion

        #region CreatePersistentListener

        public static PersistentListener CreatePersistentListener([NotNull] MethodInfo method, [CanBeNull] Object target, Type[] eventParamTypes, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
        {
            if (!MethodIsEligible(method, eventParamTypes, true, true))
                throw new MethodNotEligibleException("The method is not eligible for adding to this event", nameof(method));

            bool isStatic = method.IsStatic;
            
            if (!isStatic && target is null)
                throw new TargetNullException($"The provided method is instance but the passed target is null", nameof(target));

            string methodName = method.Name;
            CheckArguments(method, ref arguments, eventParamTypes);
            return new PersistentListener(methodName, isStatic, target, callState, method.DeclaringType, arguments);
        }

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
            if (argument._type.Type != parameter.ParameterType)
                throw new ArgumentTypeMismatchException($"The passed argument at index {argumentIndex} was assigned a wrong type {argument._type} that does not match the parameter type of the method: {parameter.ParameterType}.");

            var matchingParamIndices = GetMatchingParamIndices(eventParamTypes, argument._type);


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
    }
    
    public class MethodNotEligibleException : ArgumentException
    {
        public MethodNotEligibleException(string message, string paramName) : base(message, paramName) { }
    }

    public class TargetNullException : ArgumentException
    {
        public TargetNullException(string message, string paramName) : base(message, paramName) { }
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