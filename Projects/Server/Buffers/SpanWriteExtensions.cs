/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpanWriteExtensions.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using Server;

namespace System.Buffers
{
    public static class SpanWriteExtensions
    {
        // Extensions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ushort value) => BinaryPrimitives.WriteUInt16BigEndian(span, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, short value) => BinaryPrimitives.WriteInt16BigEndian(span, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, uint value) => BinaryPrimitives.WriteUInt32BigEndian(span, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, int value) => BinaryPrimitives.WriteInt32BigEndian(span, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, byte value) => span[0] = value;

        // Ref Extensions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, ReadOnlySpan<byte> data)
        {
            data.CopyTo(span.Slice(pos, data.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, bool value)
        {
            span[pos++] = value ? (byte)1 : (byte)0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, byte value) => span[pos++] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(pos, 2), value);
            pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, short value)
        {
            BinaryPrimitives.WriteInt16BigEndian(span.Slice(pos, 2), value);
            pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(pos, 4), value);
            pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLE(this Span<byte> span, ref int pos, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(pos, 4), value);
            pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, int value)
        {
            BinaryPrimitives.WriteInt32BigEndian(span.Slice(pos, 4), value);
            pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLE(this Span<byte> span, ref int pos, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(pos, 4), value);
            pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, Serial value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(pos, 4), value);
            pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, ulong value)
        {
            BinaryPrimitives.WriteUInt64BigEndian(span.Slice(pos, 8), value);
            pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, long value)
        {
            BinaryPrimitives.WriteInt64BigEndian(span.Slice(pos, 8), value);
            pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLE(this Span<byte> span, ref int pos, ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(pos, 8), value);
            pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLE(this Span<byte> span, ref int pos, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(span.Slice(pos, 8), value);
            pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAscii(this Span<byte> span, ref int pos, string value)
        {
            var length = value.Length;
            pos += Encoding.ASCII.GetBytes(value.AsSpan(0, length), span.Slice(pos, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAscii(this Span<byte> span, ref int pos, string value, int max)
        {
            var length = Math.Min(value.Length, max);

            pos += Encoding.ASCII.GetBytes(value.AsSpan(0, length), span.Slice(pos, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsciiNull(this Span<byte> span, ref int pos, string value)
        {
            var length = value.Length;
            pos += Encoding.ASCII.GetBytes(value.AsSpan(0, length), span.Slice(pos, length));
#if NO_LOCAL_INIT
            span[pos] = 0; // Null terminator
#endif
            pos++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsciiNull(this Span<byte> span, ref int pos, string value, int max)
        {
            var length = Math.Min(value.Length, max - 1);

            pos += Encoding.ASCII.GetBytes(value.AsSpan(0, length), span.Slice(pos, length));
#if NO_LOCAL_INIT
            span[pos] = 0; // Null terminator
#endif
            pos++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAsciiFixed(this Span<byte> span, ref int pos, string value, int amount)
        {
            var length = Math.Min(value.Length, amount);
#if NO_LOCAL_INIT
            int bytesWritten = Encoding.ASCII.GetBytes(value.AsSpan(0, length), span.Slice(pos, length));

            if (bytesWritten < amount)
                span.Slice(pos + bytesWritten, amount - bytesWritten).Clear();
#else
            Encoding.ASCII.GetBytes(value.AsSpan(0, length), span.Slice(pos, length));
#endif

            pos += amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBigUni(this Span<byte> span, ref int pos, string value)
        {
            pos += Encoding.BigEndianUnicode.GetBytes(value, span.Slice(pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLittleUni(this Span<byte> span, ref int pos, string value)
        {
            pos += Encoding.Unicode.GetBytes(value, span.Slice(pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBigUniNull(this Span<byte> span, ref int pos, string value)
        {
            pos += Encoding.BigEndianUnicode.GetBytes(value, span.Slice(pos));
#if NO_LOCAL_INIT
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(pos, 2), 0); // Null terminator
#endif
            pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLittleUniNull(this Span<byte> span, ref int pos, string value)
        {
            pos += Encoding.Unicode.GetBytes(value, span.Slice(pos));
#if NO_LOCAL_INIT
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(pos, 2), 0); // Null terminator
#endif
            pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> span, ref int pos, Point3D p)
        {
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(pos, 2), (ushort)p.X);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(pos + 2, 2), (ushort)p.Y);
            span[pos + 4] = (byte)p.Z;
            pos += 5;
        }
    }
}
