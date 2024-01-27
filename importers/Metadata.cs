using Godot;
using System;
using System.IO;

public partial class Metadata : Node
{
    string path;
    
    string magic;
    UInt32 version;
    
    FileStream fs;
    BinaryReader reader;

    static string[] TextureNativeExtensions = new string[] { ".bmp", ".tga" };
    static string[] SoundExtensions = new string[] { ".wav" };

    public void Discover(string filePath)
    {
        path = filePath;
        magic = null;
        foreach (string nativeExt in TextureNativeExtensions)
        {
            if (path.EndsWith(nativeExt))
            {
                magic = "BUILTINTEXTURE";  // Texture ype supported natively
                return;
            }
        }
        foreach (string ext in SoundExtensions)
        {
            if (path.EndsWith(ext))
            {
                magic = "WAV";
                return;
            }
        }
        // Parse complex file
        fs = File.OpenRead(path);
        if (fs.Length < 4) return; // Minimum header is 4
        reader = new BinaryReader(fs);
        version = reader.ReadUInt32();
        magic = ""; // null magic is invalid
        if (version > 255)
        {
            if (fs.Length < 8) return; // Minimum long header is 8
            // Probably the version was a magic, rewind and read again reverse
            fs.Seek(0, SeekOrigin.Begin);
            version = 0;
            magic = System.Text.Encoding.Default.GetString((reader.ReadBytes(4))).Trim('\0');
            if (!magic.Contains("WAD"))
            {
                // We parse version only for non-WAD files, because they don't have versions
                version = reader.ReadUInt32();
            }
        }
    }

    public void SetMetadata(Asset asset)
    {
        asset.path = path;
        asset.magic = magic;
        asset.version = version;
    }
    
    public Asset ImportAsset()
    {
        if (magic == null) return null;
        Asset asset = null;
        switch (magic)
        {
            case "BUILTINTEXTURE":
                asset = new Asset();
                SetMetadata(asset);
                Image img = new Image();
                img.Load(path);
                asset.gdTextures.Add(Path.GetFileNameWithoutExtension(path).ToUpper(), ImageTexture.CreateFromImage(img));
                asset.type = "Texture";
                asset.format = "Builtin";
                break;
            case "WAV":
                asset = new Asset();
                SetMetadata(asset);
                WAV wav = new WAV();
                if (wav.Load(path) == false) return null;
                asset.gdSounds.Add(Path.GetFileNameWithoutExtension(path).ToUpper(), wav);
                asset.type = "Sound";
                asset.format = "Builtin";
                break;
            case "":
                switch (version)
                {
                    case 30:
                        asset = new GoldSrcBSP();
                        SetMetadata(asset);
                        asset.Import(fs, reader);
                        asset.type = "Pack";
                        asset.format = "GoldSrc BSP";
                        break;
                    case 16:
                        return null; // Looks like a *.nod
                }
                break;
            case "IWAD":
                asset = new IWAD();
                SetMetadata(asset);
                asset.Import(fs, reader);
                asset.type = "Pack";
                asset.format = "IWAD";
                break;
            case "WAD3":
                asset = new WAD3();
                SetMetadata(asset);
                asset.Import(fs, reader);
                asset.type = "Pack";
                asset.format = "WAD3";
                break;
            case "IDSP":
                switch (version)
                {
                    case 2:
                        asset = new IDSP();
                        SetMetadata(asset);
                        asset.Import(fs, reader);
                        asset.type = "Texture";
                        asset.format = "GoldSrc SPR";
                        break;
                }
                break;
            default:
                return null;
        }
        return asset;
    }
}
