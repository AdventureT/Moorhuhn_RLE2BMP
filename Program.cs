// Huge thanks to DKDave from Xentax for helping out with this format

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

BinaryReader br = new BinaryReader(File.OpenRead(args[0]));

br.BaseStream.Seek(0xC, SeekOrigin.Begin); // Skip Header

var palette = new Color[256];

for (int i = 0; i < 256; i++) // Iterate through palette 256 color
{
    var r = br.ReadByte(); var g = br.ReadByte(); var b = br.ReadByte(); var a = br.ReadByte();
    palette[i] = Color.FromArgb(a, b, g, r); // Read it as ABGR cause Windows Bitmaps are funky
}

br.BaseStream.Seek(0x80C, SeekOrigin.Begin);
var imgHeader = new ImageHeader(br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32());

List<byte> compressedBuffer = new();

for (int i = 0; i < imgHeader.countOfSubImages; i++)
{
    var subImgHeader = new SubImageHeader(br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadInt32(), br.ReadUInt32());

    compressedBuffer.AddRange(br.ReadBytes(subImgHeader.sizeOfSubImage));

    var decompBuffer = DecompressRLE(compressedBuffer.ToArray(), subImgHeader.sizeOfSubImage, (int)(subImgHeader.width * subImgHeader.height));

    compressedBuffer.Clear();

    var rawData = ToARGB32(subImgHeader.width, subImgHeader.height, decompBuffer, palette);

    var bmp = new Bitmap(subImgHeader.width, subImgHeader.height, PixelFormat.Format32bppArgb);

    // Copy 32bppArgb data to BitmapData
    BitmapData sourceData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
    Marshal.Copy(rawData, 0, sourceData.Scan0, rawData.Length);
    bmp.UnlockBits(sourceData);

    // Save image
    bmp.Save(args[0] + "_" + (i + 1) + ".bmp", ImageFormat.Bmp);
}

// Convert Format8bppIndexed to Format32bppArgb
static int[] ToARGB32(int width, int height, byte[] imgData, Color[] palette)
{
    var rgb = new int[width * height];
    
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            var val = imgData[x + (y * width)];
            var col = palette[val];
            rgb[x + (y * width)] = col.ToArgb();
        }
    }
    return rgb;
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
            var repeatByte = data[inOff++];
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

record ImageHeader(int width, int height, uint bpp, uint isCompressed, uint countOfSubImages);
record SubImageHeader(int width, int height, uint unk3, uint unk4, uint unk5, uint unk6, int sizeOfSubImage, uint zero);