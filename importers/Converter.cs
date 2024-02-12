using Godot;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;

namespace BrushForge;

public static class Extensions
{
    public static Vector3 ToGodotVector3(this NVector3 v)
    {
        return new Vector3(-v.Y, v.Z, -v.X);
    }

    public static Vector3 ToGodotVector3(this NVector4 v)
    {
        return new Vector3(-v.Y, v.Z, -v.X);
    }

    public static NVector3 ToQuakeVector3(this Vector3 v)
    {
        return new NVector3(-v.Z, -v.X, v.Y);
    }
}