namespace TPLUtil
{
    public class ConverterTGA
    {
        public void TPLtoTGA(string args)
        {
            // Load tpl file
            BinaryReader br = new(File.Open(args, FileMode.Open));
            br.BaseStream.Position = 0x10;
            UInt16 width = br.ReadUInt16();
            UInt16 height = br.ReadUInt16();
            byte bitDepth = br.ReadByte();

            // Gets offsets where data starts
            br.BaseStream.Position = 0x30;
            UInt32 pixelsOffset = br.ReadUInt32();
            UInt32 palleteOffset = br.ReadUInt32();
            UInt32 totalPixels = palleteOffset - pixelsOffset;

            // Creates a .tga header with an array of 0x12 length
            ushort[] tgaHeader = new ushort[] { 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, width, height, 0x20 };

            // Creates .tga file
            BinaryWriter bw = new(File.Open("test.tga", FileMode.Create));

            // Writes every byte of tgaHeader array in the .tga file
            for (int value = 0; value < tgaHeader.Length; value++)
            {
                bw.Write((UInt16)tgaHeader[value]);
            }

            // Verifies bit depth
            if (bitDepth == 8)
            {
                // Loop through each pixel
                for (int x = 0; x < totalPixels; x++)
                {
                    // LOWER 4 BITS
                    br.BaseStream.Position = pixelsOffset + x;
                    int nibbleLow = br.ReadByte() >> 4;
                    byte padding = 0x20;

                    // Check if it will need padding (used in 4-bit .tpl files)
                    if (nibbleLow < 8)
                    { br.BaseStream.Position = palleteOffset + (nibbleLow * 4); }
                    else
                    { br.BaseStream.Position = palleteOffset + padding + (nibbleLow * 4); }

                    bw.Write(br.ReadByte());
                    bw.Write(br.ReadByte());
                    bw.Write(br.ReadByte());
                    bw.Write((byte)0xFF);

                    // HIGHER 4 BITS
                    br.BaseStream.Position = pixelsOffset + x;
                    int nibbleHigh = br.ReadByte() & 0x0F;

                    // Check if it will need padding (used in 4-bit .tpl files)
                    if (nibbleHigh < 8)
                    { br.BaseStream.Position = palleteOffset + (nibbleHigh * 4); }
                    else
                    { br.BaseStream.Position = palleteOffset + padding + (nibbleHigh * 4); }

                    bw.Write(br.ReadByte()); // R
                    bw.Write(br.ReadByte()); // G
                    bw.Write(br.ReadByte()); // B
                    bw.Write((byte)0xFF);    // A
                }
            }
            else if (bitDepth == 9)
            {
                // Loop through each pixel
                for (int x = 0; x < totalPixels; x++)
                {
                    br.BaseStream.Position = pixelsOffset + x;
                    int colorIndex = br.ReadByte();
                    br.BaseStream.Position = palleteOffset + (colorIndex * 4);

                    bw.Write(br.ReadByte()); // R
                    bw.Write(br.ReadByte()); // G
                    bw.Write(br.ReadByte()); // B
                    bw.Write((byte)0xFF);    // A
                }
            }
            bw.Close();
        }
    }
}
