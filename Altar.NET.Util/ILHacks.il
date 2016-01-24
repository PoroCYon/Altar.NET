.class abstract sealed Altar.ILHacks
{
    .method public static
        int32 SizeOf<valuetype .ctor ([mscorlib]System.ValueType) T>()
            cil managed
        {
            .maxstack 1

            sizeof !!T
            ret
        }

    .method public static
        uint8[] ToByteArray<valuetype .ctor ([mscorlib]System.ValueType) T>(
            !!T& v
        ) cil managed
    {
        .locals init (
            [0] uint8[]
        )

        .maxstack 3

        // arr = new byte[sizeof(T)];
        sizeof !!T
        newarr uint8
        dup
        stloc.0
        ldc.i4.0
        ldelema uint8

        ldarg.0

        sizeof !!T

        cpblk

        ldloc.0
        ret
    }

    .method public static
        void Cpblk<valuetype .ctor ([mscorlib]System.ValueType) T>(
            !!T&  source,
            void* target
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.0

        sizeof !!T

        cpblk

        ret
    }

    .method public static
        void Cpblk<valuetype .ctor ([mscorlib]System.ValueType) T>(
            void* source,
            !!T&  target
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.0

        sizeof !!T

        cpblk

        ret
    }

    .method public static
        void Cpblk<T>(
            !!T[] source,
            void* target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.0
        ldarg.2
        ldelema !!T

        ldarg.3

        cpblk

        ret
    }

    .method public static
        void Cpblk<T>(
            void* source,
            !!T[] target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.2
        ldelema !!T
        ldarg.0

        ldarg.3

        cpblk

        ret
    }

    .method public static
        void Cpblk<T1, T2>(
            !!T1[] source,
            !!T2&  target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.0
        ldarg.2
        ldelema !!T1

        ldarg.3

        cpblk

        ret
    }

    .method public static
        void Cpblk<T1, T2>(
            !!T1&  source,
            !!T2[] target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.2
        ldelema !!T2
        ldarg.0

        ldarg.3

        cpblk

        ret
    }

    .method public static
        void Cpblk<T1, T2>(
            !!T1[] source,
            !!T2[] target,
            int32 length
        ) cil managed
    {
        .maxstack 3

        ldarg.0
        ldlen
        ldarg.2
        blt.s NORMAL

        ldstr "length"
        newobj instance void [mscorlib]System.ArgumentOutOfRangeException::.ctor(string)
        throw

    NORMAL:
        ldarg.1
        ldc.i4.0
        ldelema !!T2

        ldarg.0
        ldc.i4.0
        ldelema !!T1

        sizeof !!T1
        ldarg.2
        mul

        cpblk

        ret
    }

    .method public static
        void Cpblk(
            native int source,
            native int target,
            int32 size
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.0
        ldarg.2

        cpblk

        ret
    }
    .method public static
        void Cpblk(
            void* source,
            void* target,
            int32 size
        ) cil managed
    {
        .maxstack 3

        ldarg.1
        ldarg.0
        ldarg.2

        cpblk

        ret
    }
}
