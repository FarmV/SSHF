using System;
using System.Buffers.Binary;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace SSHF.Infrastructure
{
    internal static class DibToBitmapConverter
    {
        public const string Dib = "DeviceIndependentBitmap";
        internal static MemoryStream ConvertToPng(MemoryStream ms)
        {
            if (ms == null) throw new ArgumentNullException(nameof(ms));
            ReadOnlySpan<byte> bytes = ms.ToArray();

            int headerSize = BinaryPrimitives.ReadInt32LittleEndian(bytes);
            // Only supporting 40-byte DIB from clipboard
            if (headerSize != 40) throw new ArgumentException("Unsupported DIB header size");

            ReadOnlySpan<byte> header = bytes[..headerSize];
            int dataOffset = headerSize;
            int width = BinaryPrimitives.ReadInt32LittleEndian(header[4..]);
            int height = BinaryPrimitives.ReadInt32LittleEndian(header[8..]);
            short planes = BinaryPrimitives.ReadInt16LittleEndian(header[12..]);
            short bitCount = BinaryPrimitives.ReadInt16LittleEndian(header[14..]);

            //Compression: 0 = RGB; 3 = BITFIELDS.
            int compression = BinaryPrimitives.ReadInt32LittleEndian(header[16..]);

            // Not dealing with non-standard formats.
            if (planes != 1 || compression != 0 && compression != 3) throw new ArgumentException("Unsupported DIB compression type");

            System.Drawing.Imaging.PixelFormat fmt = bitCount switch
            {
                32 => System.Drawing.Imaging.PixelFormat.Format32bppRgb,
                24 => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                16 => System.Drawing.Imaging.PixelFormat.Format16bppRgb555,
                _ => throw new ArgumentException("Unsupported DIB pixel format")
            };

            if (compression == 3) dataOffset += 12;
            if (bytes.Length < dataOffset) throw new ArgumentException("Wrong DIB image data length");

            byte[] image = bytes[dataOffset..].ToArray();
            if (compression == 3)
            {
                uint redMask = BinaryPrimitives.ReadUInt32LittleEndian(bytes[headerSize..]);
                uint greenMask = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(headerSize + 4)..]);
                uint blueMask = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(headerSize + 8)..]);

                if (bitCount == 32 && redMask == 0xFF0000 && greenMask == 0x00FF00 && blueMask == 0x0000FF)
                {
                    for (int pix = 3; pix < image.Length; pix += 4)
                    {
                        if (image[pix] != 0)
                        {
                            fmt = System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
                            break;
                        }
                    }
                }
                else throw new ArgumentException("Unsupported DIB pixel bitmask format");
            }
            using Bitmap bmp = CreateBitmap(image, width, height, fmt);
            bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
            MemoryStream result = new MemoryStream();
            bmp.Save(result, ImageFormat.Png);
            return result;
        }
        private static Bitmap CreateBitmap(byte[] bytes, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            Bitmap bmp = new Bitmap(width, height, pixelFormat);
            BitmapData bmpData = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(bytes, 0, bmpData.Scan0, height * bmpData.Stride);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }
}


