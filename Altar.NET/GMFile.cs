using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Altar.NET
{
    public static class GMFile
    {
        public unsafe static GMFileContent GetFile(byte[] data)
        {
            var ret = new GMFileContent();

            var hdr_bp = new Pointer(data);
            byte* hdr_b = hdr_bp.BPtr;

            var basePtr = (SectionHeader*)hdr_b;

            ret.Base = basePtr;

            if (ret.Base->Identity != SectionHeaders.Form)
                throw new InvalidDataException("No 'FORM' header.");

            SectionHeader*
                hdr = basePtr + 1,
                hdrEnd = (SectionHeader*)((IntPtr)basePtr + (int)ret.Base->Size);

            int headersMet = 0;

            while (hdr < hdrEnd)
            {
                switch (hdr->Identity)
                {
                    case SectionHeaders.General:
                        ret.Gen = (SectionGeneral*)hdr;
                        break;
                    case SectionHeaders.Options:
                        ret.Options = (SectionOptions*)hdr;
                        break;
                    case SectionHeaders.Extensions:
                        ret.Extensions = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Sounds:
                        ret.Sounds = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Sprites:
                        ret.Sprites = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Backgrounds:
                        ret.Backgrounds = (SectionCountOffset*)hdr;
                        break;
                    case SectionHeaders.Paths:
                        ret.Paths = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Scripts:
                        ret.Scripts = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Shaders:
                        ret.Shaders = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Fonts:
                        ret.Fonts = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Timelines:
                        ret.Timelines = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Objects:
                        ret.Objects = (SectionCountOffset*)hdr;
                        break;
                    case SectionHeaders.Rooms:
                        ret.Rooms = (SectionCountOffset*)hdr;
                        break;
                    case SectionHeaders.DataFiles:
                        ret.DataFiles = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.TexInfo:
                        ret.TexInfo = (SectionUnknown*)hdr;
                        break;
                    case SectionHeaders.Code:
                        ret.Code = (SectionCountOffset*)hdr;
                        break;
                    case SectionHeaders.Variables:
                        ret.Variables = (SectionRefDefs*)hdr;
                        break;
                    case SectionHeaders.Functions:
                        ret.Functions = (SectionRefDefs*)hdr;
                        break;
                    case SectionHeaders.Strings:
                        ret.Strings = (SectionCountOffset*)hdr;
                        break;
                    case SectionHeaders.Textures:
                        ret.Textures = (SectionCountOffset*)hdr;
                        break;
                    case SectionHeaders.Audio:
                        ret.Audio = (SectionCountOffset*)hdr;
                        break;
                    case SectionHeaders.AudioGroup:
                        ret.AudioGroup = (SectionUnknown*)hdr;
                        break;
                }

                ret.HeaderOffsets[headersMet++] = (uint)((byte*)hdr - (byte*)basePtr);
                hdr = unchecked((SectionHeader*)((IntPtr)hdr + (int)hdr->Size) + 1);
            }

            ret.RawData = hdr_bp;

            return ret;
        }

      //public static unsafe void* PtrFromOffset(GMFileContent file,  int offset) => (void*)((byte*)file.RawData.VPtr + offset);
        public static unsafe void* PtrFromOffset(GMFileContent file, uint offset) => (void*)((byte*)file.RawData.VPtr + offset);
        public static unsafe SectionHeaders HeaderOf(GMFileContent file, uint offset)
        {
            var sorted = file.HeaderOffsets.OrderBy(i => i).ToArray();

            if (sorted.Length == 1)
                return *(SectionHeaders*)PtrFromOffset(file, sorted[0]);

            for (int i = 0; i < sorted.Length - 1 && sorted[i + 1] != 0; i++)
                if (offset == sorted[i] || offset > sorted[i] && (offset < sorted[i + 1] || sorted[i + 1] == 0))
                    return *(SectionHeaders*)PtrFromOffset(file, sorted[i]);

            return SectionHeaders.Form;
        }
    }
}
