using Godot;
using System;
using System.IO;

public partial class Asset : Node
{
    [Export]
    public string type;
    [Export]
    public string format;
    [Export]
    public string path;

    [Export]
    public UInt32 version;
    [Export]
    public string magic;

    public Godot.Collections.Dictionary<string, Texture2D> gdTextures;
    public Godot.Collections.Dictionary<string, AudioStream> gdSounds;
    public Node3D gdModel;

    public Asset()
    {
        gdTextures = new Godot.Collections.Dictionary<string, Texture2D>();
        gdSounds = new Godot.Collections.Dictionary<string, AudioStream>();
    }

    virtual public void Import(FileStream fs, BinaryReader reader, Node app)
    {
    }
    
    public string GetFileName()
    {
        return Path.GetFileName(path);
    }
}
