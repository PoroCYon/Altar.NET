using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Altar
{
    public unsafe class BinBufferStreamResource : BinBufferResource
    {
        Stream stream;

        public bool DisposeStream
        {
            get;
            set;
        }

        public override int Position
        {
            get
            {
                return (int)stream.Position;
            }
            set
            {
                stream.Position = value;
            }
        }

        public override bool IsEmpty   => stream.Length == 0L;
        public override int Size       => (int)stream.Length;
        public override int BufferSize => (int)stream.Length;

        public BinBufferStreamResource(int initialCapacity)
        {
            stream = new MemoryStream(initialCapacity);
        }
        public BinBufferStreamResource(byte[] buffer)
        {
            stream = new MemoryStream(buffer);
        }
        public BinBufferStreamResource(Stream s, bool copy = false, bool dispose = true)
        {
            if (copy)
            {
                stream = new MemoryStream((int)s.Length);
                s.CopyTo(stream, (int)s.Length);
            }
            else
            {
                stream = s;

                DisposeStream = dispose;
            }
        }

        public override void Clear(bool wipeData = false)
        {
            if (stream.CanSeek)
                stream.Position = 0L;

            if (wipeData)
                stream.SetLength(0L);
        }

        public override void Write(Union v, int size)
        {
            var up = &v;

            for (uint i = 0; i < size; i++)
                stream.WriteByte(Union.ByteAt(up, i));
        }

        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
        }
        public override void Write(byte[] data, int startIndex, int count)
        {
            stream.Write(data, startIndex, count);
        }

        public override void Write(IntPtr data, int size)
        {
            var buf = new byte[size];
            ILHacks.Cpblk((void*)data, buf, 0, size);
            stream.Write(buf, 0, size);
        }

        public override Union ReadUnion(int size)
        {
            var u = new Union();
            var up = &u;

            for (uint i = 0; i < size; i++)
            {
                var v = stream.ReadByte();

                if (v == -1)
                    throw new EndOfStreamException();

                Union.SetByte(up, i, (byte)v);
            }

            return *up;
        }

        public override byte ReadByte()
        {
            var r = stream.ReadByte();

            if (r == -1)
                throw new EndOfStreamException();

            return (byte)r;
        }
        public override int Read(byte[] data, int startIndex, int count) => stream.Read(data, startIndex, count);

        public override Stream AsStream() => stream;
        public override byte[] AsByteArray()
        {
            if (stream is MemoryStream)
                return ((MemoryStream)stream).ToArray();

            //if (stream is UnmanagedMemoryStream)
            //{
            //    byte[] uret = new byte[stream.Length];
            //    Marshal.Copy(new IntPtr(((UnmanagedMemoryStream)stream).PositionPointer), uret, 0, (int)stream.Length);
            //    return uret;
            //}

            byte[] ret = new byte[stream.Length];

            long p = stream.Position;
            stream.Seek(0L, SeekOrigin.Begin);

            stream.Read(ret, 0, ret.Length);

            stream.Seek(p, SeekOrigin.Begin);

            return ret;
        }

        protected override void Dispose(bool disposing)
        {
            if (!DisposeStream)
                return;

            Clear();
            stream.Dispose();
            stream = null;
        }

        public static BinBufferStreamResource FromFile(string path, bool canWrite = true) => new BinBufferStreamResource(canWrite ? File.OpenWrite(path) : File.OpenRead(path));
    }
}
