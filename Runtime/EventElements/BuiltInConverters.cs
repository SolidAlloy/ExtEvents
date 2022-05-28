namespace ExtEvents
{
    using System.Runtime.CompilerServices;

    internal class sbyte_short_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            short arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class sbyte_int_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            int arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class sbyte_long_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            long arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class sbyte_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class sbyte_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class sbyte_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class sbyte_nint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nint arg = Unsafe.Read<sbyte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_short_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            short arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_ushort_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            ushort arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_int_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            int arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_uint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            uint arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_long_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            long arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_ulong_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            ulong arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_nint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nint arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class byte_nuint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nuint arg = Unsafe.Read<byte>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class short_int_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            int arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class short_long_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            long arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class short_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class short_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class short_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class short_nint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nint arg = Unsafe.Read<short>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_int_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            int arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_uint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            uint arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_long_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            long arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_ulong_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            ulong arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_nint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nint arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ushort_nuint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nuint arg = Unsafe.Read<ushort>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class int_long_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            long arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class int_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class int_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class int_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class int_nint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nint arg = Unsafe.Read<int>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class uint_long_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            long arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class uint_ulong_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            ulong arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class uint_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class uint_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class uint_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class uint_nuint_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            nuint arg = Unsafe.Read<uint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class long_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<long>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class long_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<long>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class long_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<long>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ulong_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<ulong>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ulong_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<ulong>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class ulong_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<ulong>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class float_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<float>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nint_long_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            long arg = Unsafe.Read<nint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nint_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<nint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nint_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<nint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nint_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<nint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nuint_ulong_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            ulong arg = Unsafe.Read<nuint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nuint_float_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            float arg = Unsafe.Read<nuint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nuint_double_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            double arg = Unsafe.Read<nuint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }

    internal class nuint_decimal_Converter : Converter
    {
        internal override unsafe void* Convert(void* sourceTypePointer)
        {
            decimal arg = Unsafe.Read<nuint>(sourceTypePointer);
            return Unsafe.AsPointer(ref arg);
        }
    }
}
