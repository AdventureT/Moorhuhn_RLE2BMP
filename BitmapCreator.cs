using System;
using System.Drawing;
using System.Drawing.Imaging;

public class BitmapCreator
{
    public static Bitmap CreateBitmap(byte[] compressedImageData, Color[] palette, int width, int height)
    {
        if (palette.Length != 256)
        {
            throw new ArgumentException("Invalid palette length");
        }

        byte[] imageData = DecompressRLE(compressedImageData, compressedImageData.Length, width * height);

        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

        ColorPalette colorPalette = bitmap.Palette;
        for (int i = 0; i < palette.Length; i++)
        {
            colorPalette.Entries[i] = palette[i];
        }
        bitmap.Palette = colorPalette;

        BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                                ImageLockMode.WriteOnly,
                                                PixelFormat.Format8bppIndexed);

        int stride = bitmapData.Stride;
        IntPtr scan0 = bitmapData.Scan0;

        unsafe
        {
            fixed (byte* imageDataPtr = imageData)
            {
                byte* imagePtr = imageDataPtr;
                byte* bitmapPtr = (byte*)scan0;

                for (int y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(imagePtr, bitmapPtr, width, width);
                    imagePtr += width;
                    bitmapPtr += stride;
                }
            }
        }

        bitmap.UnlockBits(bitmapData);

        return bitmap;
    }

    // Decompress RLE Buffer
    static byte[] DecompressRLE(byte[] data, int compressedSize, int decompressedSize)
    {
        var inOff = 0;
        var outOff = 0;
        var decompressedBuffer = new byte[decompressedSize];
        while (inOff < compressedSize)
        {
            var controlByte = data[inOff++];
            int count;

            if (controlByte >= 128)
            {
                count = 256 - controlByte;

                // Copy Bytes no RLE needed
                Buffer.BlockCopy(data, inOff, decompressedBuffer, outOff, count);

                inOff += count;
            }
            else
            {
                count = controlByte;
                byte repeatByte = data[inOff++];
                // Repeat Bytes with count: RLE
                for (int i = 0; i < count; i++)
                {
                    decompressedBuffer[outOff + i] = repeatByte;
                }
            }

            outOff += count;
        }

        return decompressedBuffer;

    }
}