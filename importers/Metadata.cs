using Godot;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

public partial class Metadata : Node
{
    string path;
    
    string magic;
    UInt32 version;
    
    FileStream fs;
    BinaryReader reader;

    static string[] TextureNativeExtensions = new string[] { ".bmp", ".tga" };
    static string[] SoundExtensions = new string[] { ".wav" };

    public string LocateInstall()
    {
        string steamInstallPath = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            steamInstallPath = (Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", null) as string).Replace("\\", "/");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            steamInstallPath = System.Environment.GetEnvironmentVariable("HOME") + "/Library/Application\\ Support/Steam";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            steamInstallPath = System.Environment.GetEnvironmentVariable("HOME") + "/.local/share/Steam";
        if (steamInstallPath == null)
        {
            return null;
        }
        return steamInstallPath + "/steamapps/common/Half-Life";
    }
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
        fs = File.Open(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
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
    
    public Asset ImportAsset(Node app)
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
                        fs.Seek(0, SeekOrigin.Begin);
                        asset.Import(fs, reader, app);
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
                asset.Import(fs, reader, app);
                asset.type = "Pack";
                asset.format = "IWAD";
                break;
            case "WAD3":
                asset = new WAD3();
                SetMetadata(asset);
                asset.Import(fs, reader, app);
                asset.type = "Pack";
                asset.format = "WAD3";
                break;
            case "IDST":
                switch (version)
                {
                    case 10:
                        asset = new MDL();
                        SetMetadata(asset);
                        asset.Import(fs, reader, app);
                        asset.type = "Model";
                        asset.format = "GoldSrc MDL";
                        break;
                }
                break;
            case "IDSP":
                switch (version)
                {
                    case 2:
                        asset = new IDSP();
                        SetMetadata(asset);
                        asset.Import(fs, reader, app);
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
