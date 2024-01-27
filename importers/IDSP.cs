using Godot;
using System;
using System.IO;

public partial class IDSP : Asset
{
    Header header;
    Color[] palette;
    Godot.Collections.Array<Frame> frames;
    public partial class Header : Node
    {
        public UInt32 spriteType;    // Sprite type: 0 = VP_PARALLEL_UPRIGHT, 1 = FACING_UPRIGHT, 2 = VP_PARALLEL, 3 = ORIENTED, 4 = VP_PARALLEL_ORIENTED
        public UInt32 textureFormat; // Texture format: 0 = SPR_NORMAL, 1 = SPR_ADDITIVE, 2 = SPR_INDEXALPHA, 3 = SPR_ALPHTEST
        public float boundingRadius; // Bounding radius: sqrt( (Max.width >> 1)*(Max.width >> 1) +(Max.height >> 1) *(Max.height >> 1))
        public UInt32 maxWidth;      // Maximum width of frame
        public UInt32 maxHeight;     // Maximum height of frame
        public UInt32 framesCount;   // Number of frames
        public float beamLength;     // Beam length
        public UInt32 syncType;      // Synchronization type (0 = synchronized, 1 = random)

        public Header(FileStream fs, BinaryReader reader)
        {
            spriteType = reader.ReadUInt32();
            textureFormat = reader.ReadUInt32();
            boundingRadius = reader.ReadSingle();
            maxWidth = reader.ReadUInt32();
            maxHeight = reader.ReadUInt32();
            framesCount = reader.ReadUInt32();
            beamLength = reader.ReadSingle();
            syncType = reader.ReadUInt32();
        }
    }

    public partial class Frame : Node
    {
        public UInt32 group;
        public Int32 originX;
        public Int32 originY;
        public UInt32 width;
        public UInt32 height;
        public Byte[] data;

        public Frame(FileStream fs, BinaryReader reader)
        {
            group = reader.ReadUInt32();
            originX = reader.ReadInt32();
            originY = reader.ReadInt32();
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            data = reader.ReadBytes((int)(width * height));
        }
    }
    override public void Import(FileStream fs, BinaryReader reader, Node app)
    {
        header = new Header(fs, reader);
        // Read palette
        UInt16 paletteSize = reader.ReadUInt16();
        palette = new Color[paletteSize];
        for (int i = 0; i < paletteSize; i++)
        {
            palette[i] = new Color(reader.ReadByte() / 255.0f, reader.ReadByte() / 255.0f, reader.ReadByte() / 255.0f);
        }
        frames = new Godot.Collections.Array<Frame>();
        for (int i = 0; i < header.framesCount; i++)
        {
            frames.Add(new Frame(fs, reader));
        }
        // Render textures
        for (int i = 0; i < header.framesCount; i++)
        {
            Image img = Image.Create((int)frames[i].width, (int)frames[i].height, false, Image.Format.Rgba8);
            for (int b = 0; b < frames[i].data.Length; b++)
            {
                img.SetPixel((int)(b % frames[i].width), (int)(b / frames[i].width), palette[frames[i].data[b]]);
            }
            gdTextures[Path.GetFileNameWithoutExtension(path) + i.ToString()] = ImageTexture.CreateFromImage(img);
        }
    }
}
