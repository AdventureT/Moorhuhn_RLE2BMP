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

for (int i = 0; i < imgHeader.countOfSubImages; i++)
{
    var subImgHeader = new SubImageHeader(br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadInt32(), br.ReadUInt32());

    byte[] compressedBuffer = br.ReadBytes(subImgHeader.sizeOfSubImage);

    var bmp = BitmapCreator.CreateBitmap(compressedBuffer, palette, subImgHeader.width, subImgHeader.height);

    bmp.Save(args[0] + "_" + (i + 1) + ".bmp", ImageFormat.Bmp);
}

record ImageHeader(int width, int height, uint bpp, uint isCompressed, uint countOfSubImages);
record SubImageHeader(int width, int height, uint unk3, uint unk4, uint unk5, uint unk6, int sizeOfSubImage, uint zero);