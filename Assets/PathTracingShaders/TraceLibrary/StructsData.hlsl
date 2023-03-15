static const float PI = 3.14159265f;
static const float EPSILON = 1e-8;
float _Seed;

RWTexture2D<float4> Result;
Texture2D<float4> _SkyBoxTexture;
SamplerState sampler_SkyBoxTexture;
float4x4 _CameraToWorld;
float4x4 _CameraInverseProjection;
float2 _Pixel;
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
    float3 albedo;
    float3 specular;
    float smoothness;
    float3 emission;
    float opacity;     
    float refractivity;
};

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
    float3 albedo;      //命中点的反照率
    float3 specular;    //命中点的镜面反射
    float  smoothness;   //命中点粗糙度
    float3 emission;    //命中点自发光
    float opacity;      //不透明度
    float refractivity; //折射率
    bool interTransparent;  //是否是进入介质
};
