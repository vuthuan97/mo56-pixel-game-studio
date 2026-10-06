using System.IO.Compression;
using PixelGameStudio.Core.Primitives;

namespace PixelGameStudio.Rendering;

/// <summary>
/// Minimal PNG codec (encode/decode) so the renderer can read and write PNG
/// without any UI framework dependency. Decoder supports color types
/// 0/2/3/4/6 with bit depths 1/2/4/8/16 (16-bit takes the high byte) and all
/// five scanline filters; Adam7 interlacing is rejected.
/// </summary>
public static class PngCodec
{
    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static byte[] Encode(PixelBuffer buffer)
    {
        using var ms = new MemoryStream();
        Encode(buffer, ms);
        return ms.ToArray();
    }

    public static void Encode(PixelBuffer buffer, Stream output)
    {
        output.Write(Signature);
        Span<byte> ihdr = stackalloc byte[13];
        WriteInt32BigEndian(ihdr, 0, buffer.Width);
        WriteInt32BigEndian(ihdr, 4, buffer.Height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 6;  // color type RGBA
        ihdr[10] = 0; // deflate
        ihdr[11] = 0; // adaptive filtering (per scanline we use 0)
        ihdr[12] = 0; // no interlace
        WriteChunk(output, "IHDR"u8, ihdr);

        // Raw scanlines: filter byte 0 + RGBA row.
        int stride = (buffer.Width * 4) + 1;
        var raw = new byte[stride * buffer.Height];
        for (int y = 0; y < buffer.Height; y++)
        {
            int rowStart = y * stride;
            raw[rowStart] = 0; // filter None
            for (int x = 0; x < buffer.Width; x++)
            {
                Rgba32 p = buffer[x, y];
                int o = rowStart + 1 + (x * 4);
                raw[o] = p.R;
                raw[o + 1] = p.G;
                raw[o + 2] = p.B;
                raw[o + 3] = p.A;
            }
        }

        byte[] idat;
        using (var compressed = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal))
            {
                zlib.Write(raw, 0, raw.Length);
            }

            idat = compressed.ToArray();
        }

        WriteChunk(output, "IDAT"u8, idat);
        WriteChunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    /// <summary>Decodes a PNG; takes ownership of and disposes the input stream.</summary>
    public static PixelBuffer Decode(Stream input)
    {
        using (input)
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            return Decode(ms.ToArray());
        }
    }

    public static PixelBuffer Decode(byte[] data)
    {
        if (data.Length < Signature.Length || !data.AsSpan(0, Signature.Length).SequenceEqual(Signature))
        {
            throw new InvalidDataException("Not a PNG file.");
        }

        int width = 0, height = 0;
        int bitDepth = 0, colorType = 0;
        byte[]? palette = null, transparency = null;
        var idat = new List<byte[]>();
        int position = Signature.Length;

        while (position + 8 <= data.Length)
        {
            int length = ReadInt32BigEndian(data, position);
            string type = System.Text.Encoding.ASCII.GetString(data, position + 4, 4);
            int dataStart = position + 8;
            if (dataStart + length + 4 > data.Length)
            {
                throw new InvalidDataException("Truncated PNG chunk.");
            }

            byte[] chunk = data.AsSpan(dataStart, length).ToArray();
            switch (type)
            {
                case "IHDR":
                    width = ReadInt32BigEndian(chunk, 0);
                    height = ReadInt32BigEndian(chunk, 4);
                    bitDepth = chunk[8];
                    colorType = chunk[9];
                    if (chunk[12] != 0)
                    {
                        throw new NotSupportedException("Adam7 interlaced PNG is not supported.");
                    }

                    break;
                case "PLTE":
                    palette = chunk;
                    break;
                case "tRNS":
                    transparency = chunk;
                    break;
                case "IDAT":
                    idat.Add(chunk);
                    break;
                case "IEND":
                    position = data.Length;
                    continue;
            }

            position = dataStart + length + 4;
        }

        if (width <= 0 || height <= 0 || idat.Count == 0)
        {
            throw new InvalidDataException("PNG is missing IHDR or IDAT.");
        }

        byte[] compressedData = idat.SelectMany(c => c).ToArray();
        byte[] raw;
        using (var src = new MemoryStream(compressedData))
        using (var zlib = new ZLibStream(src, CompressionMode.Decompress))
        using (var dst = new MemoryStream())
        {
            zlib.CopyTo(dst);
            raw = dst.ToArray();
        }

        return UnfilterToBuffer(raw, width, height, bitDepth, colorType, palette, transparency);
    }

    private static PixelBuffer UnfilterToBuffer(
        byte[] raw, int width, int height, int bitDepth, int colorType,
        byte[]? palette, byte[]? transparency)
    {
        int channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new NotSupportedException($"PNG color type {colorType} is not supported."),
        };

        if (colorType == 3 && palette is null)
        {
            throw new InvalidDataException("Palette PNG without PLTE chunk.");
        }

        int bitsPerPixel = channels * bitDepth;
        int filterBpp = Math.Max(1, bitsPerPixel / 8);
        int stride = ((width * bitsPerPixel) + 7) / 8;
        int expected = (stride + 1) * height;
        if (raw.Length < expected)
        {
            throw new InvalidDataException("PNG image data is truncated.");
        }

        var result = new PixelBuffer(width, height);
        var previous = new byte[stride];
        var current = new byte[stride];

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (stride + 1);
            byte filter = raw[rowStart];
            Array.Copy(raw, rowStart + 1, current, 0, stride);
            UnfilterRow(filter, current, previous, filterBpp);
            for (int x = 0; x < width; x++)
            {
                result[x, y] = SamplePixel(current, x, bitDepth, colorType, channels, palette, transparency);
            }

            (previous, current) = (current, previous);
        }

        return result;
    }

    private static void UnfilterRow(byte filter, byte[] current, byte[] previous, int bpp)
    {
        switch (filter)
        {
            case 0:
                break;
            case 1: // Sub
                for (int i = bpp; i < current.Length; i++)
                {
                    current[i] = (byte)(current[i] + current[i - bpp]);
                }

                break;
            case 2: // Up
                for (int i = 0; i < current.Length; i++)
                {
                    current[i] = (byte)(current[i] + previous[i]);
                }

                break;
            case 3: // Average
                for (int i = 0; i < current.Length; i++)
                {
                    int left = i >= bpp ? current[i - bpp] : 0;
                    current[i] = (byte)(current[i] + ((left + previous[i]) >> 1));
                }

                break;
            case 4: // Paeth
                for (int i = 0; i < current.Length; i++)
                {
                    int left = i >= bpp ? current[i - bpp] : 0;
                    int up = previous[i];
                    int upLeft = i >= bpp ? previous[i - bpp] : 0;
                    int p = (left + up) - upLeft;
                    int pa = Math.Abs(p - left);
                    int pb = Math.Abs(p - up);
                    int pc = Math.Abs(p - upLeft);
                    int predictor = pa <= pb && pa <= pc ? left : pb <= pc ? up : upLeft;
                    current[i] = (byte)(current[i] + predictor);
                }

                break;
            default:
                throw new InvalidDataException($"Unknown PNG filter {filter}.");
        }
    }

    private static Rgba32 SamplePixel(
        byte[] row, int x, int bitDepth, int colorType, int channels,
        byte[]? palette, byte[]? transparency)
    {
        switch (colorType)
        {
            case 6:
                {
                    if (bitDepth == 8)
                    {
                        int o = x * 4;
                        return new Rgba32(row[o], row[o + 1], row[o + 2], row[o + 3]);
                    }

                    int o16 = x * 8;
                    return new Rgba32(row[o16], row[o16 + 2], row[o16 + 4], row[o16 + 6]);
                }

            case 2:
                {
                    if (bitDepth == 8)
                    {
                        int o = x * 3;
                        return new Rgba32(row[o], row[o + 1], row[o + 2], 255);
                    }

                    int o16 = x * 6;
                    return new Rgba32(row[o16], row[o16 + 2], row[o16 + 4], 255);
                }

            case 0:
                {
                    int value = ReadChannel(row, x, bitDepth, 0);
                    if (bitDepth == 16)
                    {
                        value = row[x * 2];
                    }

                    byte key = (byte)value;
                    byte alpha = (byte)(transparency is { Length: >= 2 } && value == (transparency[0] << 8 | transparency[1]) ? 0 : 255);
                    return new Rgba32(key, key, key, alpha);
                }

            case 4:
                {
                    int cp = bitDepth == 16 ? 2 : 1;
                    int g = ReadChannel(row, x, bitDepth, 0);
                    int a = ReadChannel(row, x, bitDepth, 1);
                    if (bitDepth == 16)
                    {
                        g = row[x * 4];
                        a = row[(x * 4) + 2];
                        cp = 2;
                    }

                    _ = cp;
                    return new Rgba32((byte)g, (byte)g, (byte)g, (byte)a);
                }

            case 3:
                {
                    int index = ReadChannel(row, x, bitDepth, 0);
                    int po = index * 3;
                    byte alpha = 255;
                    if (transparency is not null && index < transparency.Length)
                    {
                        alpha = transparency[index];
                    }

                    return new Rgba32(palette![po], palette[po + 1], palette[po + 2], alpha);
                }

            default:
                throw new NotSupportedException($"PNG color type {colorType} is not supported.");
        }
    }

    private static int ReadChannel(byte[] row, int x, int bitDepth, int channel)
    {
        return bitDepth switch
        {
            8 => row[(x * 1) + channel],
            16 => row[(x * 2 * 1) + (channel * 2)], // high byte
            1 => (row[x >> 3] >> (7 - (x & 7))) & 1,
            2 => (row[x >> 2] >> (6 - (2 * (x & 3)))) & 3,
            4 => (row[x >> 1] >> (4 - (4 * (x & 1)))) & 15,
            _ => throw new NotSupportedException($"Bit depth {bitDepth} is not supported."),
        };
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> header = stackalloc byte[8];
        WriteInt32BigEndian(header, 0, data.Length);
        type.CopyTo(header[4..]);
        output.Write(header);

        uint crc = Crc32(type, data);
        output.Write(data);
        Span<byte> crcBytes = stackalloc byte[4];
        WriteInt32BigEndian(crcBytes, 0, (int)crc);
        output.Write(crcBytes);
    }

    private static uint Crc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in type)
        {
            crc = UpdateCrc(crc, b);
        }

        foreach (byte b in data)
        {
            crc = UpdateCrc(crc, b);
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static uint UpdateCrc(uint crc, byte b)
    {
        crc ^= b;
        for (int i = 0; i < 8; i++)
        {
            bool low = (crc & 1) != 0;
            crc >>= 1;
            if (low)
            {
                crc ^= 0xEDB88320;
            }
        }

        return crc;
    }

    private static void WriteInt32BigEndian(Span<byte> target, int offset, int value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static int ReadInt32BigEndian(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
}
