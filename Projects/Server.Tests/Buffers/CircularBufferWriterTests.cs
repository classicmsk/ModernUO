using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace Server.Tests.Buffers
{
    public class CircularBufferWriterTests
    {
        [Theory, InlineData("Test String", "us-ascii", -1, 1024, 1024, 0),
         InlineData("Test String", "utf-8", -1, 1024, 1024, 0), InlineData("Test String", "utf-16BE", -1, 1024, 1024, 0),
         InlineData("Test String", "utf-16", -1, 1024, 1024, 0), InlineData("Test String", "us-ascii", -1, 1024, 1024, 1030),
         InlineData("Test String", "utf-8", -1, 1024, 1024, 1030),
         InlineData("Test String", "utf-16BE", -1, 1024, 1024, 1030),
         InlineData("Test String", "utf-16", -1, 1024, 1024, 1030),
         InlineData("Test String", "us-ascii", -1, 1024, 1024, 1020),
         InlineData("Test String", "utf-8", -1, 1024, 1024, 1020),
         InlineData("Test String", "utf-16BE", -1, 1024, 1024, 1020),
         InlineData("Test String", "utf-16", -1, 1024, 1024, 1020), InlineData("Test String", "us-ascii", 8, 1024, 1024, 0),
         InlineData("Test String", "utf-16BE", 8, 1024, 1024, 0), InlineData("Test String", "utf-16", 8, 1024, 1024, 0),
         InlineData("Test String", "us-ascii", 8, 1024, 1024, 1030),
         InlineData("Test String", "utf-16BE", 8, 1024, 1024, 1030),
         InlineData("Test String", "utf-16", 8, 1024, 1024, 1030),
         InlineData("Test String", "us-ascii", 8, 1024, 1024, 1020),
         InlineData("Test String", "utf-16BE", 8, 1024, 1024, 1020),
         InlineData("Test String", "utf-16", 8, 1024, 1024, 1020), InlineData("Test String", "us-ascii", 20, 1024, 1024, 0),
         InlineData("Test String", "utf-16BE", 20, 1024, 1024, 0), InlineData("Test String", "utf-16", 20, 1024, 1024, 0),
         InlineData("Test String", "us-ascii", 20, 1024, 1024, 1030),
         InlineData("Test String", "utf-16BE", 20, 1024, 1024, 1030),
         InlineData("Test String", "utf-16", 20, 1024, 1024, 1030),
         InlineData("Test String", "us-ascii", 20, 1024, 1024, 1020),
         InlineData("Test String", "utf-16BE", 20, 1024, 1024, 1020),
         InlineData("Test String", "utf-16", 20, 1024, 1024, 1020)]
        // First only, beginning

        // Second only

        // Split

        // First only, beginning, fixed length smaller

        // Second only, fixed length smaller

        // Split, fixed length smaller

        // First only, beginning, fixed length bigger

        // Second only, fixed length bigger

        // Split, fixed length bigger
        public void TestWriteString(
            string value,
            string encodingStr,
            int fixedLength,
            int firstSize,
            int secondSize,
            int offset
        )
        {
            Span<byte> buffer = stackalloc byte[firstSize + secondSize];
            buffer.Clear();

            var encoding = EncodingHelpers.GetEncoding(encodingStr);
            var strLength = fixedLength > -1 ? Math.Min(value.Length, fixedLength) : value.Length;
            var chars = value.AsSpan(0, strLength);

            var writer = new CircularBufferWriter(buffer.Slice(0, firstSize), buffer.Slice(firstSize));
            writer.Seek(offset, SeekOrigin.Begin);
            writer.WriteString(chars, encoding);

            if (offset > 0)
            {
                Span<byte> testEmpty = stackalloc byte[offset];
                testEmpty.Clear();
                AssertThat.Equal(buffer.Slice(0, offset), testEmpty);
            }

            Span<byte> expectedStr = stackalloc byte[encoding.GetByteCount(chars)];
            encoding.GetBytes(chars, expectedStr.Slice(0));

            AssertThat.Equal(buffer.Slice(offset, expectedStr.Length), expectedStr);
            offset += expectedStr.Length;

            if (offset < buffer.Length)
            {
                Span<byte> testEmpty = stackalloc byte[buffer.Length - offset];
                testEmpty.Clear();
                AssertThat.Equal(buffer.Slice(offset), testEmpty);
            }
        }
    }
}
