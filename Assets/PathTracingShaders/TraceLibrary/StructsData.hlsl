static const float EPSILON = 1e-8;

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
};
StructuredBuffer<MaterialData> _MaterialDatas;

struct Vertex
{
    float3 position;
    float3 normal;
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
    int materialId;     //材质ID
    bool interTransparent;  //是否是进入介质
};
