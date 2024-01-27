using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Reflection.PortableExecutable;
using static GoldSrcBSP;

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

    virtual public Node3D BuildGDLevel(string levelId, string shading, Godot.Collections.Array<Asset> files)
    {
        return new Node3D();
    }
    
    // Utils
    public const float UNIT_SCALE = 1.0f / 32.0f;
    static public Vector3 ConvertVectorMDL(Vector3 rawVector)
    {
        return new Vector3(rawVector.X, rawVector.Y, rawVector.Z);
    }
    static public Vector3 ConvertVectorMDL2(Vector3 rawVector, bool rescale = true)
    {
        return new Vector3(rawVector.X, rawVector.Y, rawVector.Z) * (rescale ? UNIT_SCALE : 1.0f);
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

    // We have to populate surfacetool per triangle (3 vertex for each),
    // (1)  In triangle strips (/\/\/\), we iterate on current summit + the next two verts, because they are also part of the triangle.
    // (-1) In triangle fans (_\|/_), we iterate on first summit + the next two verts, because all triangles have the first summit of
    //      the whole array in common.
    // In both cases, we end with an offset of 2 to compensate.
    // We store the index of current summit in triangle fan vertex array in the "trivertIndex" variable
    static public int GetSummitVertIndex(int trianglePackType, int packIndex, int summit)
    {
        int vertIndex = -1;
        if (trianglePackType == 1)
        {
            switch (summit)
            {
                case 0:
                    vertIndex = packIndex + 0;
                    break;
                case 1:
                    vertIndex = packIndex % 2 == 1 ? packIndex + 2 : packIndex + 1;
                    break;
                case 2:
                    vertIndex = packIndex % 2 == 1 ? packIndex + 1 : packIndex + 2;
                    break;
            }
        }
        else
        {
            vertIndex = (summit == 0 ? 0 : packIndex + summit);
        }
        return vertIndex;
    }

}
