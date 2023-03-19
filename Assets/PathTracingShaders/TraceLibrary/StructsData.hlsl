#ifndef TRACER_STRUCTSDATA
#define TRACER_STRUCTSDATA

static const float EPSILON = 1e-8;
static const float PI = 3.14159265f;
float _Seed;
float2 _Pixel;

RWTexture2D<float4> Result;
Texture2D<float4> _SkyBoxTexture;
SamplerState sampler_SkyBoxTexture;
float4x4 _CameraToWorld;
float4x4 _CameraInverseProjection;
float _HDRIntensity;
float4 _DirectionalLight;
float2 _PixelOffset;
float2 _HDROffset;

struct Sphere
{
    float3 position;
    float radius;
    float3 albedo;
    float3 specular;
    float smoothness;
    float3 emission;
};
StructuredBuffer<Sphere> _Spheres;

struct MeshObject
{
    float4x4 localToWorldMatrix;
    float4x4 worldToLocalMatrix;
    int indices_offset;
    int indices_count;
    float3 AABBmax;
    float3 AABBmin;
    int MaterialID;
};

struct MaterialData
{
    float4 color;
    float3 emission;
    float metallic;
    float smoothness;
    float ior;
    float mode;
    int albedoIdx;
    int emisIdx;
    int metalIdx;
    int normIdx;
    int roughIdx;
};
StructuredBuffer<MaterialData> _MaterialDatas;

struct HitMaterial
{
    float3 albedo;
    float3 emission;
    int mode;
    float roughness;
    float metallic;
    float alpha;
    float ior;
};

Texture2DArray<float4> _AlbedoTextures;
SamplerState sampler_AlbedoTextures;
Texture2DArray<float4> _NormalTextures;
SamplerState sampler_NormalTextures;
Texture2DArray<float4> _RoughnessTextures;
SamplerState sampler_RoughnessTextures;
Texture2DArray<float4> _MetallicTextures;
SamplerState sampler_MetallicTextures;
Texture2DArray<float4> _EmissionTextures;
SamplerState sampler_EmissionTextures;

struct Vertex
{
    float3 position;
    float3 normal;
    float2 uv;
};

StructuredBuffer<MeshObject> _MeshObjects;
StructuredBuffer<Vertex> _Vertices;
StructuredBuffer<int> _Indices;

struct AABBBox
{
    float3 Pmin;
    float3 Pmax;
};

struct Ray
{
    float3 origin;      //起始点
    float3 direction;   //目标点
    float3 energy;      //当前能量
};

struct RayHit
{
    float3 position;    //命中点
    float distance;     //到光线出发点距离
    float3 normal;      //法线
    HitMaterial mat;     //材质
    bool interTransparent;  //是否是进入介质
};

#endif