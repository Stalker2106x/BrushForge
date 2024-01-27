using Godot;
using System;
using System.IO;

public partial class WAD3 : DataPack
{
    static Color TransparentColor = new Color(0.0f, 0.0f, 1.0f); // In WAD3s, blue is expect to be transparent.

    public Godot.Collections.Array<Entry> entries;
    public Godot.Collections.Array<Texture> textures;

    public partial class Entry : GodotObject
    {
        public UInt32 dataIndex;       // Offset to entry data in WAD
        public UInt32 dataSize;        // Size of the entry in WAD
        public UInt32 uDataSize;       // Uncompressed size (unused)
        public Byte type;        // Type of entry
        public bool compression; // 1 if compressed, else 0
        public string name;        // Name of entry

        public Entry(FileStream fs, BinaryReader reader)
        {
            dataIndex = reader.ReadUInt32();
            dataSize = reader.ReadUInt32();
            uDataSize = reader.ReadUInt32();
            type = reader.ReadByte();
            compression = reader.ReadByte() > 0;
            reader.ReadBytes(2); // Dummy
            name = ExtractString(reader.ReadBytes(16));
        }
    }

    public partial class Texture : GodotObject
    {
        public string name;
        public UInt32 width;
        public UInt32 height;
        public UInt32[] mipMapOffsets;
        public Byte[] dataMipMap0;
        public Byte[] dataMipMap1;
        public Byte[] dataMipMap2;
        public Byte[] dataMipMap3;
        public Color[] colorPalette;
        public bool _hasAlpha; // Field added for easier parsing

        public Texture(FileStream fs, BinaryReader reader, UInt32 lumpIndex)
        {
            fs.Seek(lumpIndex, SeekOrigin.Begin);
            name = ExtractString(reader.ReadBytes(16));
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            mipMapOffsets = new UInt32[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() };
            fs.Seek(lumpIndex + mipMapOffsets[0], SeekOrigin.Begin);
            dataMipMap0 = reader.ReadBytes((int)(width * height));
            fs.Seek(lumpIndex + mipMapOffsets[1], SeekOrigin.Begin);
            dataMipMap1 = reader.ReadBytes((int)((width / 2) * (height/2)));
            fs.Seek(lumpIndex + mipMapOffsets[2], SeekOrigin.Begin);
            dataMipMap2 = reader.ReadBytes((int)((width / 4) * (height / 4)));
            fs.Seek(lumpIndex + mipMapOffsets[3], SeekOrigin.Begin);
            dataMipMap3 = reader.ReadBytes((int)((width / 8) * (height / 8)));
            UInt16 colorCount = reader.ReadUInt16();
            colorPalette = new Color[colorCount];
            _hasAlpha = false;
            for (int i = 0; i < colorCount; i++)
            {
                colorPalette[i] = new Color(reader.ReadByte() / 255.0f, reader.ReadByte() / 255.0f, reader.ReadByte() / 255.0f);
                if (colorPalette[i] == TransparentColor) _hasAlpha = true;
            }
        }
    }

    override public void Import(FileStream fs, BinaryReader reader)
    {
        UInt32 dirSize = reader.ReadUInt32();
        UInt32 dirIndex = reader.ReadUInt32();
        // Parse Entries
        entries = new Godot.Collections.Array<Entry>();
        fs.Seek(dirIndex, SeekOrigin.Begin);
        for (int i = 0; i < dirSize; i++) {
            entries.Add(new Entry(fs, reader));
        }
        // Parse Textures
        textures = new Godot.Collections.Array<Texture>();
        foreach (Entry entry in entries)
        {
            if (entry.type != 67) continue;
            Texture tex = new Texture(fs, reader, entry.dataIndex);
            textures.Add(tex);
            // Insert in GDTextures
            Image img = Image.Create((int)tex.width, (int)tex.height, false, tex._hasAlpha ? Image.Format.Rgba8 : Image.Format.Rgb8);
            for (int i = 0; i < tex.dataMipMap0.Length; i++) {
                img.SetPixel((int)(i % tex.width), (int)(i / tex.width), tex.colorPalette[tex.dataMipMap0[i]]);
            }
            gdTextures[tex.name] = ImageTexture.CreateFromImage(img);
        }
    }
}
