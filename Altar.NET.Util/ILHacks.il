
//!    ___________________________________________________
//!  /                                                    \
//! | do NOT rename, the name is hardcoded in the .csproj |
//! \ __________________________________________________ /
//!

.assembly extern mscorlib
{
    .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
    .ver 4:0:0:0
}
.assembly Altar.NET.Util
{
    .custom instance void [mscorlib]System.Reflection.AssemblyCompanyAttribute                     ::.ctor(string) = { string('PoroCYon')                                  }
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32 ) = { int32(8)                                            }
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute  ::.ctor(      ) = { property bool 'WrapNonExceptionThrows' = bool(true) }
    .custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute                       ::.ctor(string) = { string('Altar.NET.Util')                            }
    .custom instance void [mscorlib]System.Reflection.AssemblyDescriptionAttribute                 ::.ctor(string) = { string('Utility methods for Altar.NET')             }
    .custom instance void [mscorlib]System.Reflection.AssemblyConfigurationAttribute               ::.ctor(string) = { string('')                                          }

    .custom instance void [mscorlib]System.Reflection.AssemblyProductAttribute        ::.ctor(string) = { string('Altar.NET')                                                                                      }
    .custom instance void [mscorlib]System.Reflection.AssemblyCopyrightAttribute      ::.ctor(string) = { string('Copyright © PoroCYon 2016')                                                                      }
    .custom instance void [mscorlib]System.Reflection.AssemblyTrademarkAttribute      ::.ctor(string) = { string('')                                                                                               }
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool  ) = { bool(false)                                                                                              }
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute      ::.ctor(string) = { string('a5a66bee-a24b-40d7-8f72-bb3ac53a8e67')                                                           }
    .custom instance void [mscorlib]System.Reflection.AssemblyFileVersionAttribute    ::.ctor(string) = { string('1.0.0.0')                                                                                        }
    .custom instance void [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = { string('.NETFramework,Version=v4.0') property string 'FrameworkDisplayName' = string('.NET Framework 4') }

    .permissionset reqmin = { [mscorlib]System.Security.Permissions.SecurityPermissionAttribute = { property bool 'SkipVerification' = bool(true) } }

    .hash algorithm 0x00008004
    .ver 1:0:0:0
}

.module Altar.NET.Util.dll
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = { }

.imagebase      0x10000000
.file alignment 0x00000200
.stackreserve   0x00100000
.subsystem      0x0003
.corflags       0x00000001

.class public abstract sealed auto ansi Altar.ILHacks
{
    .method public static hidebysig
        int32 SizeOf<valuetype .ctor ([mscorlib]System.ValueType) T>()
            cil managed
        {
            .maxstack 1

            sizeof !!T
            ret
        }

    .method public static hidebysig
        uint8[] ToByteArray<valuetype .ctor ([mscorlib]System.ValueType) T>(
            !!T& v
        ) cil managed
    {
        .locals init (
            [0] uint8[]
        )

        .maxstack 3

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

    .method public static hidebysig
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

    .method public static hidebysig
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

    .method public static hidebysig
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

    .method public static hidebysig
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

    .method public static hidebysig
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

    .method public static hidebysig
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

    .method public static hidebysig
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

    .method public static hidebysig
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
    .method public static hidebysig
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

    // useful overloads

    .method public hidebysig static
        uint8[] ToByteArray<valuetype .ctor ([mscorlib]System.ValueType) T>(
            !!T v
        ) cil managed
    {
        .maxstack 1

        ldarga.s   v
        call uint8[] Altar.ILHacks::ToByteArray<!!0>(!!0&)
        ret
    }

    .method public hidebysig static
        void Cpblk<valuetype .ctor ([mscorlib]System.ValueType) T>(
            !!T& source,
            native int target
        ) cil managed
    {
        .maxstack 2

        ldarg.0
        ldarg.1
        // not needed to cast to a void*

        call void Altar.ILHacks::Cpblk<!!0>(!!0&, void*)

        ret
    }

    .method public hidebysig static
        void Cpblk<T>(
            !!T[] source,
            native int target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack 4

        ldarg.0
        ldarg.1
        // <cast>

        ldarg.2
        ldarg.3
        call void Altar.ILHacks::Cpblk<!!0>(!!0[], void*, int32, int32)

        ret
    }

    .method public hidebysig static
        void Cpblk<T>(
            native int source,
            !!T[] target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack 4

        ldarg.0
        // <cast>
        ldarg.1
        ldarg.2
        ldarg.3
        call void Altar.ILHacks::Cpblk<!!0>(void*, !!0[], int32, int32)

        ret
    }

    .method public hidebysig static
        void Cpblk<T1,T2>(
            !!T1[] source,
            !!T2 target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack  8

        ldarg.0
        ldarga.s target
        ldarg.2
        ldarg.3
        call void Altar.ILHacks::Cpblk<!!0,!!1>(!!0[], !!1&, int32, int32)

        ret
    }

    .method public hidebysig static
        void Cpblk<T1,T2>(
            !!T1 source,
            !!T2[] target,
            int32 index,
            int32 size
        ) cil managed
    {
        .maxstack  8

        ldarga.s source
        ldarg.1
        ldarg.2
        ldarg.3
        call void Altar.ILHacks::Cpblk<!!0,!!1>(!!0&, !!1[], int32, int32)

        ret
    }

    .method public hidebysig static
        void Cpblk<valuetype .ctor ([mscorlib]System.ValueType) T>(
            native int source,
            !!T& target
        ) cil managed
    {
        .maxstack  2

        ldarg.0
        // <cast>
        ldarg.1
        call void Altar.ILHacks::Cpblk<!!0>(void*, !!0&)

        ret
    }

    .method public hidebysig static
        void Cpblk<T1,T2>(
            !!T1[] source,
            !!T2[] target
        ) cil managed
    {
        .maxstack  3

        ldarg.0
        ldarg.1
        ldarg.0
        ldlen
        conv.i4
        call void Altar.ILHacks::Cpblk<!!0,!!1>(!!0[], !!1[], int32)

        ret
    }
}
