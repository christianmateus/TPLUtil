namespace TPLUtil
{
    public struct BMP
    {
        public UInt16 magic;
        public UInt32 filesize;
        public UInt32 unused;
        public UInt32 pixelsOffset;

        public UInt32 infoHeader;
        public UInt32 width;
        public UInt32 height;
        public UInt16 planes; // always 01
        public UInt16 bitDepth; // 1-2-4-8-16-24 bits per pixel
        public UInt32 compression; // 0 = uncompressed | 1 = 8bit RLE | 2 = 4bit RLE
        public UInt32 imageSize; // after compressing, if uncompressed use 0
        public UInt32 horResolution; // horizontal resolution: Pixels/meter
        public UInt32 verResolution; // vertical resolution: Pixels/meter
        public UInt32 colors; // colors quantity
        public UInt32 colorsImportant;

    }

    public struct TPL
    {
        public UInt32 magic;
        public UInt32 tplCount;
        public UInt32 startOffset;
        public UInt32 unused1;

        public UInt16 width;
        public UInt16 height;
        public UInt16 bitDepth;
        public UInt16 interlace;
        public UInt16 baseResolution;
        public UInt16 mipmapCount;
        public UInt16 multipliedResolution;
        public UInt16 unused2;

        public UInt32 mipmapOffset1;
        public UInt32 mipmapOffset2;
        public UInt32 unk1;
        public UInt32 unk2;

        public UInt32 pixelsOffset1;
        public UInt32 paletteOffset1;
        public byte unused3;
        public byte config1;
        public byte config2;
        public byte config3;
        public byte config4;
        public byte unused4;
        public byte unused5;
        public byte endTag;
    }

    public class ConverterBMP
    {
        public void TPLtoBMP()
        {
            BinaryReader br = new(File.Open("itm0c6.tpl", FileMode.Open));

            BMP bmp;
            bmp.magic = 0x4D42;
            bmp.filesize = 256882;
            bmp.unused = 0;
            bmp.pixelsOffset = 0; // 436 for 256 colors, 76 for 16 colors
            bmp.infoHeader = 0x28;

            br.BaseStream.Position = 0x10;
            bmp.width = br.ReadUInt16();
            bmp.height = br.ReadUInt16();
            bmp.planes = 1;
            bmp.bitDepth = br.ReadByte();
            bmp.compression = 0;
            bmp.imageSize = 0;
            bmp.horResolution = 2835; // This is the default value
            bmp.verResolution = 2835; // This is the default value
            bmp.colors = 0;
            bmp.colorsImportant = 0;

            // Detect bit depth
            switch (bmp.bitDepth)
            {
                case 8: bmp.bitDepth = 4; bmp.colors = 16; bmp.colorsImportant = 16; bmp.pixelsOffset = 0x76; break;
                case 9: bmp.bitDepth = 8; bmp.colors = 256; bmp.colorsImportant = 256; bmp.pixelsOffset = 0x436; break;
                default: break;
            }

            // Gets pixels and palette offsets
            br.BaseStream.Position = 0x30;
            UInt32 pixelsOffset = br.ReadUInt32();
            br.BaseStream.Position = 0x34;
            UInt32 paletteOffset = br.ReadUInt32();
            UInt32 totalPixels = paletteOffset - pixelsOffset;

            // Reads every byte from palette
            br.BaseStream.Position = paletteOffset;
            byte[] palette = new byte[bmp.colors * 4];
            byte padding = 0x20; // Used in 4-bit textures

            // Verifies color count and make necessary adjustments
            if (bmp.colors == 16)
            {
                for (int i = 0; i < palette.Length; i++)
                {
                    if (i < 0x20) { palette[i] = br.ReadByte(); }
                    if (i == 0x20) { br.BaseStream.Position = br.BaseStream.Position + padding; }
                    if (i >= 0x20) { palette[i] = br.ReadByte(); }
                }
            }
            else if (bmp.colors == 256)
            {
                for (int i = 0; i < palette.Length; i++)
                {
                    palette[i] = br.ReadByte();
                }
            }


            // Reads every pixel
            br.BaseStream.Position = 0x40;
            if (bmp.colors == 16)
            {

            }
            byte[] pixels = br.ReadBytes((int)totalPixels);

            // -------------------------------
            // WRITING DATA 
            // -------------------------------
            BinaryWriter bw = new(File.Open("bmpteste.bmp", FileMode.Create));

            bw.Write(bmp.magic);
            bw.Write(bmp.filesize);
            bw.Write(bmp.unused);
            bw.Write(bmp.pixelsOffset);
            bw.Write(bmp.infoHeader);
            bw.Write(bmp.width);
            bw.Write(bmp.height);
            bw.Write(bmp.planes);
            bw.Write(bmp.bitDepth);
            bw.Write(bmp.compression);
            bw.Write(bmp.imageSize);
            bw.Write(bmp.horResolution);
            bw.Write(bmp.verResolution);
            bw.Write(bmp.colors);
            bw.Write(bmp.colorsImportant);

            // Writing palette data
            for (int i = 0; i < palette.Length; i += 4)
            {
                bw.Write((byte)palette[i + 2]); // B
                bw.Write((byte)palette[i + 1]); // G
                bw.Write((byte)palette[i]); // R
                bw.Write((byte)palette[i + 3]); // A
            }

            for (int pixel = 0; pixel < pixels.Length; pixel++)
            {
                bw.Write(pixels[pixel]);
            }

            bw.BaseStream.Position = 0x2;
            bmp.filesize = (uint)bw.BaseStream.Length;
            bw.Write(bmp.filesize);

            bw.Close();
            br.Close();
        }

        public void BMPtoTPL()
        {
            BinaryReader br = new(File.Open("aaa8bit.bmp", FileMode.Open));
            TPL tpl;

            tpl.magic = 0x00001000;
            tpl.tplCount = 1;
            tpl.startOffset = 0x10;
            tpl.unused1 = 0;

            br.BaseStream.Position = 0x12;
            tpl.width = (ushort)br.ReadUInt32();
            tpl.height = (ushort)br.ReadUInt32();

            br.BaseStream.Position = 0x1C;
            ushort bmpBitDepth = br.ReadUInt16();
            if (bmpBitDepth == 4) { tpl.bitDepth = 8; }
            else if (bmpBitDepth == 8) { tpl.bitDepth = 9; }
            else { Console.WriteLine("Bit depth not supported, images must be 4-bit or 8-bit"); return; }

            tpl.interlace = 0;
            tpl.baseResolution = 256;
            tpl.mipmapCount = 0;
            tpl.multipliedResolution = (ushort)(tpl.width * 4);
            tpl.unused2 = 0;

            tpl.mipmapOffset1 = 0;
            tpl.mipmapOffset2 = 0;
            tpl.unk1 = 0;
            tpl.unk2 = 0;

            tpl.pixelsOffset1 = 0x40;
            tpl.paletteOffset1 = 0;
            tpl.unused3 = 0;
            tpl.config1 = 0; // This is either 00, 40 or 80
            tpl.config2 = 0;

            // Getting bit depth for tpl config
            if (tpl.bitDepth == 8)
            {
                tpl.config2 = 0x40; // This byte is usually 0x40 for 4-bit images
                tpl.paletteOffset1 = (uint)(tpl.width * tpl.height / 2 + 0x40);
            }
            else if (tpl.bitDepth == 9)
            {
                tpl.config2 = 0x30; // This byte is usually 0x30 for 8-bit images
                tpl.paletteOffset1 = (uint)(tpl.width * tpl.height + 0x40);
            }

            tpl.config3 = 0xDD; // This byte controls the way the texture is applied to the model, 0xDD solves for ITM and SMD
            tpl.config4 = 0x05; // Not sure what this does, common values are 5 and 6
            tpl.unused4 = 0;
            tpl.unused5 = 0;
            tpl.endTag = 0x40;

            // Get pixels bytes
            br.BaseStream.Position = 0x0A;
            UInt32 bmpPixelsOffset = br.ReadUInt32();
            br.BaseStream.Position = bmpPixelsOffset;
            byte[] bmpPixels = br.ReadBytes(tpl.width * tpl.height);

            // Get palette bytes
            br.BaseStream.Position = 0x2E;
            UInt32 bmpPaletteCount = br.ReadUInt32();
            br.BaseStream.Position = 0x36;
            byte[] bmpPalette = br.ReadBytes((int)bmpPaletteCount * 4);

            //-------------------------------
            // WRITING DATA
            //-------------------------------
            BinaryWriter bw = new(File.Open("tplteste.tpl", FileMode.Create));

            Console.WriteLine("Writing TPL header");
            bw.Write(tpl.magic);
            bw.Write(tpl.tplCount);
            bw.Write(tpl.startOffset);
            bw.Write(tpl.unused1);
            bw.Write(tpl.width);
            bw.Write(tpl.height);
            bw.Write(tpl.bitDepth);
            bw.Write(tpl.interlace);
            bw.Write(tpl.baseResolution);
            bw.Write(tpl.mipmapCount);
            bw.Write(tpl.multipliedResolution);
            bw.Write(tpl.unused2);
            bw.Write(tpl.mipmapOffset1);
            bw.Write(tpl.mipmapOffset2);
            bw.Write(tpl.unk1);
            bw.Write(tpl.unk2);
            bw.Write(tpl.pixelsOffset1);
            bw.Write(tpl.paletteOffset1);
            bw.Write(tpl.unused3);
            bw.Write(tpl.config1);
            bw.Write(tpl.config2);
            bw.Write(tpl.config3);
            bw.Write(tpl.config4);
            bw.Write(tpl.unused4);
            bw.Write(tpl.unused5);
            bw.Write(tpl.endTag);

            Console.WriteLine("Writing TPL pixels");
            // Writing pixels
            for (int i = 0; i < bmpPixels.Length; i++)
            {
                bw.Write(bmpPixels[i]);
            }

            Console.WriteLine("Writing TPL palette");
            // Writing palette
            if (tpl.bitDepth == 8)
            {
                Console.WriteLine("4 bit texture");
                for (int i = 0; i < bmpPalette.Length; i++)
                {
                    if (i == 0x20)
                    {
                        for (int padding = 0; padding < 0x08; padding++)
                        {
                            bw.Write((UInt32)0);
                        }
                    }
                    bw.Write(bmpPalette[i]);
                }
                for (int padding = 0; padding < 0x08; padding++)
                {
                    bw.Write((UInt32)0);
                }
            }
            else if (tpl.bitDepth == 9)
            {
                Console.WriteLine("8 bit texture");
                for (int i = 0; i < bmpPalette.Length; i += 4)
                {
                    bw.Write(bmpPalette[i + 2]); // B
                    bw.Write(bmpPalette[i + 1]); // G
                    bw.Write(bmpPalette[i]); // R
                    bw.Write(bmpPalette[i + 3]); // A
                }
            }
            bw.Close();
        }
    }

}
