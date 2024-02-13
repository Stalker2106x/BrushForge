using Godot;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;

namespace BrushForge;

public static class Extensions
{
    public const float UNIT_SCALE = 1.0f / 32.0f;

    /* v4 */
    public static Vector3 ToGodotVector3(this NVector4 v)
    {
        return new Vector3(-v.Y, v.Z, -v.X);
    }

    /* v3 default */
    public static Vector3 ToGodotVector3(this NVector3 v)
    {
        return new Vector3(-v.Y, v.Z, -v.X);
    }

    public static Vector3 ToGodotVector3(this Vector3 v)
    {
        return new Vector3(-v.Y, v.Z, -v.X);
    }

    public static NVector3 ToQuakeVector3(this Vector3 v)
    {
        return new NVector3(-v.Z, -v.X, v.Y);
    }

    /* v3 scaled */
    public static Vector3 ToSGodotVector3(this NVector3 v)
    {
        return new Vector3(-v.Y, v.Z, -v.X) * UNIT_SCALE;
    }

    public static Vector3 ToSGodotVector3(this Vector3 v)
    {
        return new Vector3(-v.Y, v.Z, -v.X) * UNIT_SCALE;
    }

    public static NVector3 ToSQuakeVector3(this Vector3 v)
    {
        return new NVector3(-v.Z, -v.X, v.Y) / UNIT_SCALE;
    }
}