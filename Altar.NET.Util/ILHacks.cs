using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Altar
{
    public unsafe static class ILHacks
    {
        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern int SizeOf<T>() where T : struct;

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern byte[] ToByteArray<T>(ref T v) where T : struct;
        public static byte[] ToByteArray<T>(T v) where T : struct => ToByteArray(ref v);

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk<T>(ref T source, void* target) where T : struct;
        public static void Cpblk<T>(ref T source, IntPtr target)
            where T : struct
        {
            Cpblk(ref source, (void*)target);
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk<T>(T[] source, void* target, int index, int size);
        public static void Cpblk<T>(T[] source, IntPtr target, int index, int size)
        {
            Cpblk(source, (void*)target, index, size);
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk<T>(void* source, T[] target, int index, int size);
        public static void Cpblk<T>(IntPtr source, T[] target, int index, int size)
        {
            Cpblk((void*)source, target, index, size);
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk<T1, T2>(T1[] source, ref T2 target, int index, int size);
        public static void Cpblk<T1, T2>(T1[] source, T2 target, int index, int size)
        {
            Cpblk(source, ref target, index, size);
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk<T1, T2>(ref T1 source, T2[] target, int index, int size);
        public static void Cpblk<T1, T2>(T1 source, T2[] target, int index, int size)
        {
            Cpblk(ref source, target, index, size);
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk<T>(void* source, ref T target) where T : struct;
        public static void Cpblk<T>(IntPtr source, ref T target)
            where T : struct
        {
            Cpblk((void*)source, ref target);
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk<T1, T2>(T1[] source, T2[] target, int length);
        public static void Cpblk<T1, T2>(T1[] source, T2[] target)
        {
            Cpblk(source, target, source.Length);
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk(IntPtr source, IntPtr target, int size);
        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern void Cpblk(void* source, void* target, int size);
    }
}
