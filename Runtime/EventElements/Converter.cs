namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public abstract class Converter
    {
        public static readonly Dictionary<(Type from, Type to), Type> ConverterTypes =
            new Dictionary<(Type from, Type to), Type>()
            {
            };

        private static readonly Dictionary<(Type from, Type to), Converter> _createdConverters =
            new Dictionary<(Type from, Type to), Converter>();

        internal static Converter GetForTypes(Type from, Type to)
        {
            var types = (from, to);

            if (_createdConverters.TryGetValue(types, out var converter))
                return converter;

            if (!ConverterTypes.TryGetValue(types, out var converterType))
            {
                throw new NotImplementedException(); // implement emit in editor, throw error in AOT builds.
            }

            converter = (Converter) Activator.CreateInstance(converterType);
            _createdConverters.Add(types, converter);
            return converter;
        }

        internal abstract unsafe void* Convert(void* sourceTypePointer);
    }

    public abstract class Converter<TFrom, TTo> : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            TTo arg = Convert(Unsafe.Read<TFrom>(sourceTypePointer));
            return Unsafe.AsPointer(ref arg);
        }

        protected abstract TTo Convert(TFrom from);
    }

    public class ExampleConverter : Converter<float, int>
    {
        static ExampleConverter()
        {
            // TODO: discover a way to streamline this without user having to type in the types or ideally any method
            Converter.ConverterTypes.Add((typeof(float), typeof(int)), typeof(ExampleConverter));
        }

        protected override int Convert(float from)
        {
            return (int) from;
        }
    }
}