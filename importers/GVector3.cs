using Godot;
using Godot.Collections;
using System;
using System.IO;

public partial class GVector3 : GodotObject
{
    float x;
    float y;
    float z;

    // Build from GDCoords
    public GVector3(float gdx, float gdy, float gdz)
    {
        x = -gdy;
        y = gdz;
        z = -gdx;
    }
    public GVector3(Vector3 gdVec)
    {
        x = -gdVec.Y;
        y = gdVec.Z;
        z = -gdVec.X;
    }

    // Build from QCoords
    public GVector3(BinaryReader reader)
    {
        x = reader.ReadSingle();
        y = reader.ReadSingle();
        z = reader.ReadSingle();
    }

    public Vector3 GetGDVector3()
    {
        return new Vector3(-y, z, -x);
    }

    // Output
    public Byte[] GetBSPData()
    {
        return new Byte[3 * 4];
    }
}
