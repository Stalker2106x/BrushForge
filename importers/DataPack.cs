using Godot;
using System;
using System.IO;
using System.Reflection.PortableExecutable;

public partial class DataPack : Asset
{
    public DataPack()
    {
    }
    
    virtual public Godot.Collections.Array<Variant> GetLevels()
    {
        return new Godot.Collections.Array<Variant>();
    }

    virtual public Godot.Collections.Array<Variant> GetEntities()
    {
        return new Godot.Collections.Array<Variant>();
    }

    virtual public Node3D BuildGDLevel(string levelId, Godot.Collections.Dictionary<string, Variant> renderSettings, Godot.Collections.Array<Asset> files)
    {
        return new Node3D();
    }
    
    // Utils
    public const float UNIT_SCALE = 1.0f / 32.0f;
    static public Vector3 ConvertVector(Vector3 rawVector, bool rescale = true)
    {
        return new Vector3(-rawVector.Y, rawVector.Z, -rawVector.X) * (rescale ? UNIT_SCALE : 1.0f);
    }

    static public string ExtractString(Byte[] buffer)
    {
        string str = "";
        for (int i = 0; i < buffer.Length; i++)
        {
            char c = (char)(buffer[i] >= 97 && buffer[i] <= 122 ? buffer[i] - 32 : buffer[i]);
            if (c == '\0') break;
            str += c;
        }
        return str;
    }
}
