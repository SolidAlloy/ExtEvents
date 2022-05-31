namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using TypeReferences;
    using UnityEngine;
    using UnityEngine.Events;
    using Object = UnityEngine.Object;

    /// <summary>
    /// A persistent listener of an <see cref="ExtEvent"/> that contains a method to invoke and a number of serialized arguments.
    /// Can be configured in ExtEvent's inspector.
    /// </summary>
    [Serializable]
    public partial class PersistentListener
    {
        [SerializeField] internal PersistentArgument[] _persistentArguments;

        /// <summary>
        /// A list of persistent arguments the listener has. Each argument can be either dynamic (passed when the event is invoked) or serialized (set in the editor UI before-hand).
        /// </summary>
        [PublicAPI]
        public IReadOnlyList<PersistentArgument> PersistentArguments => _persistentArguments;

        [SerializeField] internal Object _target;

        /// <summary>
        /// The target object of a listener which method is invoked. For static listeners, it is null.
        /// </summary>
        [PublicAPI]
        public Object Target => _target;

        [SerializeField] internal bool _isStatic;

        /// <summary>
        /// Whether the listener invokes a static or instance method.
        /// </summary>
        [PublicAPI]
        public bool IsStatic => _isStatic;

        /// <summary>
        /// Whether the listener is invoked when the play mode is not entered, or is turned off permanently.
        /// </summary>
        [SerializeField] public UnityEventCallState CallState = UnityEventCallState.RuntimeOnly;

        [SerializeField, TypeOptions(ShowAllTypes = true, AllowInternal = true, ShowNoneElement = false)]
        internal TypeReference _staticType;

        /// <summary>
        /// The declaring type of a static method when the listener is static.
        /// </summary>
        [PublicAPI]
        public Type StaticType => _staticType;

        [NonSerialized] internal bool _initializationComplete;
        [NonSerialized] private bool _initializationSuccessful;
        private unsafe void*[] _arguments;
        private BaseInvokableCall _invokableCall;

        private PersistentListener() { }

        internal PersistentListener([NotNull] MethodInfo method, [CanBeNull] Object target, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
        {
            if (method is null)
                // ReSharper disable once NotResolvedInText
                throw new ArgumentNullException("The method provided is null", nameof(method));

            bool isStatic = method.IsStatic;

            _methodName = method.Name;
            _isStatic = isStatic;
            _target = target;
            CallState = callState;
            _staticType = method.DeclaringType;
            _persistentArguments = arguments;
        }

        /// <summary>
        /// Create a <see cref="PersistentListener"/> instance from a static method.
        /// </summary>
        /// <param name="methodDelegate">
        /// A method delegate that refers to a static method.
        /// The method must not contain a parameter that is not serialized and is not one of the generic argument types of the event.
        /// </param>
        /// <param name="callState">When to call the callback.</param>
        /// <param name="arguments">
        /// A list of the arguments that will be passed to the method. The number of arguments must match the number of
        /// parameters taken in by the method. An argument can have a pre-determined serialized value or be dynamic which
        /// means it will be passed when the event is invoked. When the argument is dynamic, an index is specified that
        /// reflects the index of an argument in the ExtEvent.Invoke() method.
        /// </param>
        /// <exception cref="ArgumentException">The method provided is instance.</exception>
        /// <exception cref="ArgumentNullException">The method provided is null.</exception>
        /// <returns>A new instance of <see cref="PersistentListener"/>.</returns>
        public static PersistentListener FromStatic<T>([NotNull] T methodDelegate, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
            where T : Delegate
        {
            return FromStatic(methodDelegate.Method, callState, arguments);
        }

        /// <summary>
        /// Create a <see cref="PersistentListener"/> instance from a static method.
        /// </summary>
        /// <param name="method">
        /// A static method info that acts as a callback to an event.
        /// The method must not contain a parameter that is not serialized and is not one of the generic argument types of the event.
        /// </param>
        /// <param name="callState">When to call the callback.</param>
        /// <param name="arguments">
        /// A list of the arguments that will be passed to the method. The number of arguments must match the number of
        /// parameters taken in by the method. An argument can have a pre-determined serialized value or be dynamic which
        /// means it will be passed when the event is invoked. When the argument is dynamic, an index is specified that
        /// reflects the index of an argument in the ExtEvent.Invoke() method.
        /// </param>
        /// <exception cref="ArgumentException">The method provided is instance.</exception>
        /// <exception cref="ArgumentNullException">The method provided is null.</exception>
        /// <returns>A new instance of <see cref="PersistentListener"/>.</returns>
        public static PersistentListener FromStatic([NotNull] MethodInfo method, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
        {
            if (!method.IsStatic)
                throw new ArgumentException("Expected a static method but got an instance one.", nameof(method));

            return new PersistentListener(method, null, callState, arguments);
        }

        /// <summary>
        /// Create a <see cref="PersistentListener"/> instance from an instance method.
        /// </summary>
        /// <param name="methodDelegate">
        /// A method delegate that refers to a instance method.
        /// The method must not contain a parameter that is not serialized and is not one of the generic argument types of the event.
        /// </param>
        /// <param name="target">An object that contains the passed method and on which the method will be called.</param>
        /// <param name="callState">When to call the callback.</param>
        /// <param name="arguments">
        /// A list of the arguments that will be passed to the method. The number of arguments must match the number of
        /// parameters taken in by the method. An argument can have a pre-determined serialized value or be dynamic which
        /// means it will be passed when the event is invoked. When the argument is dynamic, an index is specified that
        /// reflects the index of an argument in the ExtEvent.Invoke() method.
        /// </param>
        /// <exception cref="ArgumentException">The method provided is static.</exception>
        /// <exception cref="ArgumentNullException">The target or the method provided is null.</exception>
        /// <returns>A new instance of <see cref="PersistentListener"/>.</returns>
        public static PersistentListener FromInstance<T>([NotNull] T methodDelegate, [NotNull] Object target, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
            where T : Delegate
        {
            return FromInstance(methodDelegate.Method, target, callState, arguments);
        }

        /// <summary>
        /// Create a <see cref="PersistentListener"/> instance from an instance method.
        /// </summary>
        /// <param name="method">
        /// An instance method info that acts as a callback to an event.
        /// The method must not contain a parameter that is not serialized and is not one of the generic argument types of the event.
        /// </param>
        /// <param name="target">An object that contains the passed method and on which the method will be called.</param>
        /// <param name="callState">When to call the callback.</param>
        /// <param name="arguments">
        /// A list of the arguments that will be passed to the method. The number of arguments must match the number of
        /// parameters taken in by the method. An argument can have a pre-determined serialized value or be dynamic which
        /// means it will be passed when the event is invoked. When the argument is dynamic, an index is specified that
        /// reflects the index of an argument in the ExtEvent.Invoke() method.
        /// </param>
        /// <exception cref="ArgumentException">The method provided is static.</exception>
        /// <exception cref="ArgumentNullException">The target or the method provided is null.</exception>
        /// <returns>A new instance of <see cref="PersistentListener"/>.</returns>
        public static PersistentListener FromInstance([NotNull] MethodInfo method, [NotNull] Object target, UnityEventCallState callState = UnityEventCallState.RuntimeOnly, [CanBeNull] params PersistentArgument[] arguments)
        {
            if (method.IsStatic)
                throw new ArgumentException("Expected an instance method but got a static one.", nameof(method));

            if (target is null)
                throw new ArgumentNullException($"The provided method is instance but the passed {nameof(target)} is null");

            return new PersistentListener(method, target, callState, arguments);
        }

        internal unsafe void Invoke([CanBeNull] void*[] args)
        {
            // If no function is chosen, exit without any warnings.
            if (CallState == UnityEventCallState.Off || string.IsNullOrEmpty(_methodName))
                 return;

#if UNITY_EDITOR
            if (CallState == UnityEventCallState.RuntimeOnly && !Application.isPlaying)
                return;
#endif

            if (_initializationComplete)
            {
                if (_initializationSuccessful)
                    InvokeImpl(args);

                return;
            }

            _initializationSuccessful = Initialize();

            if (_initializationSuccessful)
                InvokeImpl(args);
        }

        internal static BindingFlags GetFlags(bool isStatic) => BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance | BindingFlags.Static);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void InvokeImpl(void*[] args)
        {
            FillWithDynamicArgs(args);
            _invokableCall.Invoke(_arguments);
        }

        /// <summary>
        /// Prepare the listener for the invocation.
        /// </summary>
        /// <returns>Whether the initialization is successful.</returns>
        public bool Initialize()
        {
            var declaringType = GetDeclaringType();

            if (declaringType == null)
            {
                _initializationComplete = true;
                return false;
            }

            var argumentTypes = GetArgumentTypes();

            if (argumentTypes == null)
            {
                _initializationComplete = true;
                return false;
            }

            var target = _isStatic ? null : _target;
            _invokableCall = GetInvokableCall(declaringType, argumentTypes, target);

            if (_invokableCall == null)
            {
                LogMethodInfoWarning();
                _initializationComplete = true;
                return false;
            }

            InitializeArguments();

            _initializationComplete = true;
            return true;
        }

        private Type GetDeclaringType()
        {
            if (_isStatic)
            {
                if (_staticType.Type == null)
                    Logger.LogWarning($"Tried to invoke a listener to an event but the declaring type is missing: {_staticType.TypeNameAndAssembly}");

                return _staticType.Type;
            }

            if (_target is null)
                Logger.LogWarning("Tried to invoke a listener to an event but the target is missing");

            return _target?.GetType();
        }

        private void LogMethodInfoWarning()
        {
#if UNITY_EDITOR
            if (!PackageSettings.ShowInvocationWarning)
                return;
#endif

            string typeName = _isStatic ? _staticType.TypeNameAndAssembly : _target.GetType().Name;
            bool isProperty = _methodName.IsPropertySetter();
            string memberName = isProperty ? "property" : "method";
            string methodName = isProperty ? $"{_methodName.Substring(4)} setter" : _methodName;
            Logger.LogWarning($"Tried to invoke a listener to an event but the {memberName} {typeName}.{methodName} is missing.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void FillWithDynamicArgs(void*[] args)
        {
            if (args == null || _arguments == null)
                return;

            for (int i = 0; i < _arguments.Length; i++)
            {
                var persistentArg = _persistentArguments[i];

                if (!persistentArg._isSerialized)
                    _arguments[i] = persistentArg.ProcessDynamicArgument(args[persistentArg._index]);
            }
        }

        // Setting _arguments inside the method instead of returning it because IL2CPP incorrectly translates a method that returns void*[] into a void method.
        private unsafe void InitializeArguments()
        {
            if (_persistentArguments.Length == 0)
                return;

            _arguments = new void*[_persistentArguments.Length];

            for (int i = 0; i < _persistentArguments.Length; i++)
            {
                var serializedArg = _persistentArguments[i];

                if (serializedArg._isSerialized)
                {
                    _arguments[i] = serializedArg.SerializedValuePointer;
                }
                else
                {
                    serializedArg.InitDynamic();
                    _arguments[i] = null;
                }
            }
        }

        private IEnumerable<string> GetNullArgumentTypeNames()
        {
            foreach (var argument in _persistentArguments)
            {
                if (argument._targetType.Type == null)
                    yield return argument._targetType.TypeNameAndAssembly;
            }
        }

        private Type[] GetArgumentTypes()
        {
            var types = new Type[_persistentArguments.Length];

            for (int i = 0; i < _persistentArguments.Length; i++)
            {
                types[i] = _persistentArguments[i]._targetType.Type;

                if (types[i] == null)
                {
                    if (PackageSettings.ShowInvocationWarning)
                        Logger.LogWarning($"Tried to invoke a listener to an event but some of the argument types are missing: {string.Join(", ", GetNullArgumentTypeNames().Select(TypeReference.GetTypeNameFromNameAndAssembly))}.");

                    return null;
                }
            }

            return types;
        }
    }
}