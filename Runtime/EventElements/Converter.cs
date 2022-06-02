#if (UNITY_EDITOR || UNITY_STANDALONE) && !ENABLE_IL2CPP
#define CAN_EMIT
#endif

namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEditor;
    using UnityEngine;

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

        public static IEnumerable<((Type from, Type to) fromToTypes, Type customConverter)> GetCustomConverters()
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
                throw new ExecutionEngineException("Attempting to convert types that don't have a converter generated ahead of time (AOT).");
#pragma warning restore CS0618
#endif
            }

            converter = (Converter) Activator.CreateInstance(converterType);
            _createdConverters.Add(types, converter);
            return converter;
        }

        public abstract unsafe void* Convert(void* sourceTypePointer);
    }

    public abstract class Converter<TFrom, TTo> : Converter
    {
        public override unsafe void* Convert(void* sourceTypePointer)
        {
            TTo arg = Convert(Unsafe.Read<TFrom>(sourceTypePointer));
            return Unsafe.AsPointer(ref arg);
        }

        protected abstract TTo Convert(TFrom from);
    }

    // public class ExampleConverter : Converter<float, int>
    // {
    //     protected override int Convert(float from)
    //     {
    //         return (int) from;
    //     }
    // }
}