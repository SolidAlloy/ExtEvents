namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEngine.Scripting;

    public partial class Converter
    {
        public static readonly Dictionary<(Type from, Type to), Type> BuiltInConverters = new Dictionary<(Type from, Type to), Type>()
        {
            { (typeof(sbyte), typeof(short)), typeof(sbyte_short_Converter) },
            { (typeof(sbyte), typeof(int)), typeof(sbyte_int_Converter) },
            { (typeof(sbyte), typeof(long)), typeof(sbyte_long_Converter) },
            { (typeof(sbyte), typeof(float)), typeof(sbyte_float_Converter) },
            { (typeof(sbyte), typeof(double)), typeof(sbyte_double_Converter) },
            { (typeof(sbyte), typeof(decimal)), typeof(sbyte_decimal_Converter) },
            { (typeof(byte), typeof(short)), typeof(byte_short_Converter) },
            { (typeof(byte), typeof(ushort)), typeof(byte_ushort_Converter) },
            { (typeof(byte), typeof(int)), typeof(byte_int_Converter) },
            { (typeof(byte), typeof(uint)), typeof(byte_uint_Converter) },
            { (typeof(byte), typeof(long)), typeof(byte_long_Converter) },
            { (typeof(byte), typeof(ulong)), typeof(byte_ulong_Converter) },
            { (typeof(byte), typeof(float)), typeof(byte_float_Converter) },
            { (typeof(byte), typeof(double)), typeof(byte_double_Converter) },
            { (typeof(byte), typeof(decimal)), typeof(byte_decimal_Converter) },
            { (typeof(short), typeof(int)), typeof(short_int_Converter) },
            { (typeof(short), typeof(long)), typeof(short_long_Converter) },
            { (typeof(short), typeof(float)), typeof(short_float_Converter) },
            { (typeof(short), typeof(double)), typeof(short_double_Converter) },
            { (typeof(short), typeof(decimal)), typeof(short_decimal_Converter) },
            { (typeof(ushort), typeof(int)), typeof(ushort_int_Converter) },
            { (typeof(ushort), typeof(uint)), typeof(ushort_uint_Converter) },
            { (typeof(ushort), typeof(long)), typeof(ushort_long_Converter) },
            { (typeof(ushort), typeof(ulong)), typeof(ushort_ulong_Converter) },
            { (typeof(ushort), typeof(float)), typeof(ushort_float_Converter) },
            { (typeof(ushort), typeof(double)), typeof(ushort_double_Converter) },
            { (typeof(ushort), typeof(decimal)), typeof(ushort_decimal_Converter) },
            { (typeof(int), typeof(long)), typeof(int_long_Converter) },
            { (typeof(int), typeof(float)), typeof(int_float_Converter) },
            { (typeof(int), typeof(double)), typeof(int_double_Converter) },
            { (typeof(int), typeof(decimal)), typeof(int_decimal_Converter) },
            { (typeof(uint), typeof(long)), typeof(uint_long_Converter) },
            { (typeof(uint), typeof(ulong)), typeof(uint_ulong_Converter) },
            { (typeof(uint), typeof(float)), typeof(uint_float_Converter) },
            { (typeof(uint), typeof(double)), typeof(uint_double_Converter) },
            { (typeof(uint), typeof(decimal)), typeof(uint_decimal_Converter) },
            { (typeof(long), typeof(float)), typeof(long_float_Converter) },
            { (typeof(long), typeof(double)), typeof(long_double_Converter) },
            { (typeof(long), typeof(decimal)), typeof(long_decimal_Converter) },
            { (typeof(ulong), typeof(float)), typeof(ulong_float_Converter) },
            { (typeof(ulong), typeof(double)), typeof(ulong_double_Converter) },
            { (typeof(ulong), typeof(decimal)), typeof(ulong_decimal_Converter) },
            { (typeof(float), typeof(double)), typeof(float_double_Converter) },
        };

        public static readonly Dictionary<(Type from, Type to), Type> ConverterTypes = new Dictionary<(Type from, Type to), Type>(BuiltInConverters);
    }

    [Preserve]
    internal class sbyte_short_Converter : Converter
    {
        private short _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class sbyte_int_Converter : Converter
    {
        private int _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class sbyte_long_Converter : Converter
    {
        private long _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class sbyte_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class sbyte_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class sbyte_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_short_Converter : Converter
    {
        private short _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_ushort_Converter : Converter
    {
        private ushort _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_int_Converter : Converter
    {
        private int _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_uint_Converter : Converter
    {
        private uint _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_long_Converter : Converter
    {
        private long _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_ulong_Converter : Converter
    {
        private ulong _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class byte_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class short_int_Converter : Converter
    {
        private int _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class short_long_Converter : Converter
    {
        private long _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class short_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class short_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class short_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ushort_int_Converter : Converter
    {
        private int _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ushort_uint_Converter : Converter
    {
        private uint _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ushort_long_Converter : Converter
    {
        private long _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ushort_ulong_Converter : Converter
    {
        private ulong _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ushort_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ushort_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ushort_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class int_long_Converter : Converter
    {
        private long _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class int_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class int_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class int_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class uint_long_Converter : Converter
    {
        private long _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class uint_ulong_Converter : Converter
    {
        private ulong _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class uint_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class uint_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class uint_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class long_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<long>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class long_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<long>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class long_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<long>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ulong_float_Converter : Converter
    {
        private float _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ulong>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ulong_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ulong>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class ulong_decimal_Converter : Converter
    {
        private decimal _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<ulong>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }

    [Preserve]
    internal class float_double_Converter : Converter
    {
        private double _arg;

        public override unsafe void* Convert(void* sourceTypePointer)
        {
            _arg = Unsafe.Read<float>(sourceTypePointer);
            return Unsafe.AsPointer(ref _arg);
        }
    }
}
