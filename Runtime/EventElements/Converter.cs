#if (UNITY_EDITOR || UNITY_STANDALONE) && !ENABLE_IL2CPP
#define CAN_EMIT
#endif

namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A class that converts a value of one type to another type. The exact from and to types are defined by derived classes.
    /// </summary>
    // Not using RequireDerivedAttribute here because it doesn't work and Unity strips the inheritors' default constructor anyway.
    // Instead, we have to rely on good old Preserve in built-in converters and automatically add custom converters to link.xml (See BuildPreprocessor)
    public abstract partial class Converter
    {
#if UNITY_EDITOR
        static Converter()
        {
            // Find all inheritors of the Converter<TFrom, TTo> class and add them to ConverterTypes.
            foreach ((var fromToTypes, Type customConverterType) in GetCustomConverters())
            {
                if (ConverterTypes.TryGetValue(fromToTypes, out var converterType))
                {
                    Debug.LogWarning($"Two custom converters for the same pair of types: {converterType} and {customConverterType}");
                    continue;
                }

                ConverterTypes.Add(fromToTypes, customConverterType);
            }
        }

        internal static IEnumerable<((Type from, Type to) fromToTypes, Type customConverter)> GetCustomConverters()
        {
            var types = TypeCache.GetTypesDerivedFrom<Converter>();

            foreach (Type type in types)
            {
                if (type.IsGenericType || type.IsAbstract)
                    continue;

                var baseType = type.BaseType;

                // ReSharper disable once PossibleNullReferenceException
                if (!baseType.IsGenericType)
                    continue;

                var genericArgs = baseType.GetGenericArguments();

                if (genericArgs.Length != 2)
                    continue;

                var fromToTypes = (genericArgs[0], genericArgs[1]);

                yield return (fromToTypes, type);
            }
        }
#endif

        private static readonly Dictionary<(Type from, Type to), Converter> _createdConverters =
            new Dictionary<(Type from, Type to), Converter>();

        /// <summary>
        /// Whether a converter from type <paramref name="from"/> to type <paramref name="to"/> exists.
        /// This may be a converter for numerical conversions between built-in types,
        /// a converter that uses implicit conversion operator of the <paramref name="from"/> type, or a custom converter.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool ExistsForTypes(Type from, Type to)
        {
            var types = (from, to);

            if (ConverterTypes.TryGetValue(types, out _))
                return true;

            return ImplicitConversionsCache.HaveImplicitConversion(from, to);
        }

        public static Converter GetForTypes(Type from, Type to)
        {
            var types = (from, to);

            if (_createdConverters.TryGetValue(types, out var converter))
                return converter;

            if (!ConverterTypes.TryGetValue(types, out var converterType))
            {
    #if CAN_EMIT
                var implicitOperator = ImplicitConversionsCache.GetImplicitOperatorForTypes(from, to);

                if (implicitOperator == null)
                {
                    Debug.LogError($"Tried to implicitly convert type {from} to {to} but no implicit operator was found.");
                    return null;
                }

                converterType = ConverterEmitter.EmitConverter(from, to, implicitOperator);
                ConverterTypes.Add(types, converterType);
#else
#pragma warning disable CS0618
                throw new ExecutionEngineException($"Attempting to convert types {{{from}, {to}}} that don't have a converter generated ahead of time (AOT).");
#pragma warning restore CS0618
#endif
            }

            converter = (Converter) Activator.CreateInstance(converterType);
            _createdConverters.Add(types, converter);
            return converter;
        }

        public abstract unsafe void* Convert(void* sourceTypePointer);
    }

#pragma warning disable 436
    /// <summary>
    /// A base converter for custom converters defined between types <typeparamref name="TFrom"/> and <typeparamref name="TTo"/>.
    /// Inherit from this type and define rules of the conversion in the Convert method. The type will be automatically used by ExtEvents.
    /// </summary>
    /// <typeparam name="TFrom"></typeparam>
    /// <typeparam name="TTo"></typeparam>
    // Using a copy of UsedImplicitly declared below because Unity's version of JetBrains.Annotations is too old and doesn't include WithInheritors.
    [UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
    public abstract class Converter<TFrom, TTo> : Converter
    {
        private TTo _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Convert(Unsafe.Read<TFrom>(sourceTypePointer));
            return Unsafe.AsPointer(ref _arg);
        }

        protected abstract TTo Convert(TFrom from);
    }
}

namespace JetBrains.Annotations
{
    using System;
    using System.Diagnostics;

    [AttributeUsage(AttributeTargets.All)]
    [Conditional("JETBRAINS_ANNOTATIONS")]
    // Setting it to internal so that there are no ambiguous errors in code that uses ExtEvents. Rider is smart enough to process this attribute anyway.
    internal sealed class UsedImplicitlyAttribute : Attribute
    {
        public UsedImplicitlyAttribute()
            : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) { }

        public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags)
            : this(useKindFlags, ImplicitUseTargetFlags.Default) { }

        public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags)
            : this(ImplicitUseKindFlags.Default, targetFlags) { }

        public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
        {
            UseKindFlags = useKindFlags;
            TargetFlags = targetFlags;
        }

        public ImplicitUseKindFlags UseKindFlags { get; }

        public ImplicitUseTargetFlags TargetFlags { get; }
    }

    [Flags]
    internal enum ImplicitUseTargetFlags
    {
        Default = 1,
        Itself = Default, // 0x00000001
        Members = 2,
        WithInheritors = 4,
        WithMembers = Members | Itself, // 0x00000003
    }
}
#pragma warning restore 436