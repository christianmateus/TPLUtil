// Global variables
using TPLUtil;

//if (args[1] == "extract-tpl")
//{
//    ExtractTextures();
//}
//else if (args[1] == "convert-tga")
//{
//    // Converting
//    ConverterTGA converter = new();
//    converter.ConvertToTGA(args[0]);
//}

ConverterBMP ConverterBMP = new();
ConverterBMP.TPLtoBMP();
ConverterBMP.BMPtoTPL();

void ExtractTextures()
{
    FileStream fs = new(args[0], FileMode.Open);
    BinaryReader buffer = new(fs);

    string roomID = Path.GetFileNameWithoutExtension(args[0]);

    // Get textures starting offset
    buffer.BaseStream.Seek(0x08, SeekOrigin.Begin);
    UInt32 texturesOffset = buffer.ReadUInt32() + 0x10;
    UInt32 filesize = (UInt32)fs.Length;

    Directory.CreateDirectory("Textures");

    // TPL Header size
    int headerLength = 0x30;
    buffer.BaseStream.Seek(texturesOffset + 0x04, SeekOrigin.Begin);
    byte texturesCount = buffer.ReadByte();
    UInt32[] offsets = new UInt32[texturesCount];

    // Loop to get all texture offsets
    for (uint i = 0; i < texturesCount; i++)
    {
        buffer.BaseStream.Seek(texturesOffset + headerLength, SeekOrigin.Begin);
        offsets[i] = buffer.ReadUInt32();
        headerLength += 0x30;
    }

    // Loop to generate main header, this one includes tpl count and has 0x10 length
    for (int tpl = 0; tpl < texturesCount; tpl++)
    {
        byte[] mainHeader = new byte[] { 0x00, 0x10, 0x00, 0x00, 01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        BinaryWriter bw = new(File.Open($"Textures/{roomID}_{tpl}.tpl", FileMode.Create));
        bw.Write(mainHeader);
        bw.Close();
    }

    // Loop to get and write all tpl headers
    headerLength = 0;
    for (int header = 0; header < texturesCount; header++)
    {
        buffer.BaseStream.Position = texturesOffset + 0x10 + headerLength;
        BinaryWriter bw = new(File.Open($"Textures/{roomID}_{header}.tpl", FileMode.Append));
        bw.Write(buffer.ReadBytes(0x30));
        headerLength += 0x30;
        bw.Close();
    }

    // Move stream pointer to the first texture and extracts all textures
    buffer.BaseStream.Position = texturesOffset + offsets[0];
    for (uint i = 0; i < texturesCount; i++)
    {
        BinaryReader br = new(File.Open($"Textures/{roomID}_{i}.tpl", FileMode.Open));
        br.BaseStream.Position = 0x14;
        byte bitDepth = br.ReadByte();

        br.BaseStream.Position = 0x30;
        UInt32 pixelsOffset = br.ReadUInt32();
        UInt32 palleteOffset = br.ReadUInt32();

        int palleteLength = 0;
        br.Close();

        switch (bitDepth)
        {
            case 8:
                palleteLength = 0x80;
                break;
            case 9:
                palleteLength = 0x400;
                break;
            default:
                break;
        }

        BinaryWriter bw = new(File.Open($"Textures/{roomID}_{i}.tpl", FileMode.Append));

        bw.Write(buffer.ReadBytes((int)((palleteOffset - pixelsOffset) + palleteLength)));
        bw.Close();
    }

    // Update header config
    for (int texture = 0; texture < texturesCount; texture++)
    {
        BinaryReader br = new(File.Open($"Textures/{roomID}_{texture}.tpl", FileMode.Open));

        // Gets bit depth value (8 for 16 colors, 9 for 256 colors)
        br.BaseStream.Position = 0x14;
        byte bitDepth = br.ReadByte();
        br.Close();

        // Updating offsets for pixels and pallete
        BinaryWriter bw = new(File.Open($"Textures/{roomID}_{texture}.tpl", FileMode.Open));
        UInt32 pixelsOffset = 0x40;
        UInt32 palleteOffset = 0;
        switch (bitDepth)
        {
            case 8:
                long a = bw.BaseStream.Length - 0x80; // 0x80 for 4-bit textures
                palleteOffset = (UInt32)a;
                break;
            case 9:
                long pixels = bw.BaseStream.Length - 0x400; // 0x400 for 8-bit textures
                palleteOffset = (UInt32)pixels;
                break;
            default:
                break;
        }

        bw.BaseStream.Position = 0x30;
        bw.Write(pixelsOffset);
        bw.BaseStream.Position = 0x34;
        bw.Write(palleteOffset);

        // Removing Mipmaps config
        bw.BaseStream.Position = 0x1A;
        bw.Write((byte)0);
        for (int i = 0; i < 16; i++)
        {
            bw.BaseStream.Position = 0x20 + i;
            bw.Write((byte)0);
        }
    }
    fs.Close();
    buffer.Close();
}