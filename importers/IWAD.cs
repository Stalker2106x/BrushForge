using Godot;
using System;
using System.Data.Common;
using System.IO;
using System.Reflection.PortableExecutable;

public partial class IWAD : DataPack
{
    public Godot.Collections.Array<Entry> entries;
    public Color[] palette;
    public Godot.Collections.Array<Texture> textures;

    public partial class Entry : GodotObject
    {
        public UInt32 dataIndex;
        public UInt32 dataSize;
        public string name;
        
        public Entry(FileStream fs, BinaryReader reader)
        {
            dataIndex = reader.ReadUInt32();
            dataSize = reader.ReadUInt32();
            name = ExtractString(reader.ReadBytes(8));
        }
    }
    
    public partial class Texture : GodotObject
    {
        public UInt16 width; // Texture width
        public UInt16 height; // Texture height
        public UInt16 xOffset; // Texture x offset
        public UInt16 yOffset; // Texture y offset
        public UInt32[] columnIndexes;
        public UInt32 columns;
        public Godot.Collections.Array<Chunk>[] data; // 2D Array of texture data
        
        public partial class Chunk : GodotObject
        {
            public Byte row;
            public Byte height;
            public Byte[] paletteIndexes;

            static public Chunk Parse(FileStream fs, BinaryReader reader)
            {
                Chunk chunk = new Chunk();
                chunk.row = reader.ReadByte();    // This is the row where the next chunk starts (for transparency)
                if (chunk.row == 255) return null;           // Reached end of column
                chunk.height = reader.ReadByte(); // Height of the next chunk
                if (chunk.height == 255) return null;        // Reached end of column
                chunk.paletteIndexes = new Byte[chunk.height];
                reader.ReadByte();               // Dummy
                for (int i = 0; i < chunk.height; i++)
                {
                    chunk.paletteIndexes[i] = reader.ReadByte(); // Apparently this needs to be -1'ed
                }
                reader.ReadByte(); // Dummy
                return chunk;
            }
        }

        public Texture(FileStream fs, BinaryReader reader, UInt32 lumpIndex)
        {
            fs.Seek(lumpIndex, SeekOrigin.Begin);
            width = reader.ReadUInt16();
            height = reader.ReadUInt16();
            xOffset = reader.ReadUInt16();
            yOffset = reader.ReadUInt16();
            columnIndexes = new UInt32[width];
            for (int c = 0; c < width; c++) {
                columnIndexes[c] = reader.ReadUInt32();
            }
            // Actually parse texture
            int column = 0;
            data = new Godot.Collections.Array<Chunk>[width];

            fs.Seek(lumpIndex + columnIndexes[column], SeekOrigin.Begin);
            data[column] = new Godot.Collections.Array<Chunk>();
            while (column < width && fs.Position < fs.Length)
            {
                Chunk chunk = Chunk.Parse(fs, reader);
                if (chunk != null) {
                    data[column].Add(chunk);
                } else {
                    column++;
                    if (column < width)
                    {
                        fs.Seek(lumpIndex + columnIndexes[column], SeekOrigin.Begin);
                        data[column] = new Godot.Collections.Array<Chunk>();
                    }
                }
            }
        }
    }
    
    override public void Import(FileStream fs, BinaryReader reader)
    {
        UInt32 dirCount = reader.ReadUInt32();
        UInt32 dirIndex = reader.ReadUInt32();
        // Parse Entries
        entries = new Godot.Collections.Array<Entry>();
        fs.Seek(dirIndex, SeekOrigin.Begin);
        for (int i = 0; i < dirCount; i++) {
            entries.Add(new Entry(fs, reader));
        }
        // Iterate and decode
        string section = null;
        foreach (Entry entry in entries)
        {
            if (entry.name.StartsWith("PLAYPAL")) {
                section = null;
                fs.Seek(entry.dataIndex, SeekOrigin.Begin);
                palette = new Color[256];
                for (int i = 0; i < 256; i++)
                {
                    palette[i] = new Color(reader.ReadByte() / 256.0f, reader.ReadByte() / 256.0f, reader.ReadByte() / 256.0f);
                }
            } else if (entry.name.StartsWith("MAP")) {
                section = entry.name;
                // Parse map
            } else if (entry.name.StartsWith("S_START")) {
                section = "texture";
                textures = new Godot.Collections.Array<Texture>();
            } else if (entry.name.StartsWith("S_END")) {
                section = null;
            } else if (section == "texture") {
                Texture tex = new Texture(fs, reader, entry.dataIndex);
                textures.Add(tex);
                // Inject in GDTextures
                Image img = Image.Create(tex.width, tex.height, false, Image.Format.Rgba8);
                for (int column = 0; column < tex.width; column++)
                {
                    foreach (Texture.Chunk chunk in tex.data[column])
                    {
                        for (int rRow = 0; rRow < chunk.height; rRow++)
                        {
                            img.SetPixel(column, chunk.row + rRow, palette[chunk.paletteIndexes[rRow]]);
                        }
                    }
                }
                gdTextures[entry.name] = ImageTexture.CreateFromImage(img);
            }
        }
    }
}
