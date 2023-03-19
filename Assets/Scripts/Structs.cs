using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MaterialData
{
    public Vector4 Color;
    public Vector3 Emission;
    public float Metallic;
    public float Smoothness;
    public float IOR;
    public float RenderMode;
    public int AlbedoIdx;
    public int EmisIdx;
    public int MetallicIdx;
    public int NormalIdx;
    public int RoughIdx;

    public static int TypeSize = sizeof(float)*11+sizeof(int)*5;
}

public struct MeshObject
{
    public Matrix4x4 localToWorldMatrix;
    public Matrix4x4 worldToLocalMatrix;
    public int indices_offset;
    public int indices_count;
    public Vector3 AABBmax;
    public Vector3 AABBmin;
    public int MaterialID;

    public static int TypeSize = sizeof(int) * 3 + sizeof(float) * 38;
}

//工具函数
class PathTracingFunction
{
    public static Vector4 ColorToVector4(Color color)
    {
        return new Vector4(color.r, color.g, color.b, color.a);
    }

    public static Vector3 ColorToVector3(Color color)
    {
        return new Vector3(color.r, color.g, color.b);
    }
}