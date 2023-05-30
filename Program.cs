// Huge thanks to DKDave from Xentax for helping out with this format

using System.Drawing;
using System.Drawing.Imaging;

BinaryReader br = new BinaryReader(File.OpenRead(args[0]));

br.BaseStream.Seek(0xC, SeekOrigin.Begin); // Skip Header

var palette = new Color[256];

for (int i = 0; i < 256; i++) // Iterate through palette 256 color
{
    var r = br.ReadByte(); var g = br.ReadByte(); var b = br.ReadByte(); var a = br.ReadByte();
    palette[i] = Color.FromArgb(a, b, g, r);
}

br.BaseStream.Seek(0x80C, SeekOrigin.Begin); // Go to ImageHeader
var imgHeader = new ImageHeader(br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32());


SubImageHeader subImgHeader = null;

for (int i = 0; i < imgHeader.countOfSubImages; i++)
{
    List<byte> pixelData = new();
    subImgHeader = new SubImageHeader(br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadInt32(), br.ReadUInt32());
    pixelData.AddRange(br.ReadBytes(subImgHeader.sizeOfSubImage));
    Console.WriteLine(br.BaseStream.Position);
    var decompBuffer = DecompressRLE(pixelData.ToArray(), subImgHeader.sizeOfSubImage, (int)(subImgHeader.width * subImgHeader.height));

    var bmp = new Bitmap(subImgHeader.width, subImgHeader.height, PixelFormat.Format32bppArgb);

    for (int y = 0; y < subImgHeader.height; y++)
    {
        for (int x = 0; x < subImgHeader.width; x++)
        {
            var val = decompBuffer[x + (y * subImgHeader.width)];
            var col = palette[val];
            bmp.SetPixel(x,y,col);
        }
    }

    bmp.Save(args[0] + "_" + (i + 1) + ".bmp", ImageFormat.Bmp);
}

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
