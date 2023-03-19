#ifndef TRACER_FUNCTIONS
#define TRACER_FUNCTIONS
#include "StructsData.hlsl"

float rand()
{
    float result = frac(sin(_Seed / 100.0f * dot(_Pixel, float2(12.9898f, 78.233f))) * 43758.5453f);
    _Seed += 1.0f;
    return result;
}

float3x3 GetTangentSpace(float3 normal)
{
    float3 helper = float3(1,0,0);
    if(abs(normal.x) > 0.99f)
        helper = float3(0,0,1);

    //生产切线和副切线
    float3 tangent = normalize(cross(normal,helper));
    float3 binormal = normalize(cross(normal,tangent));
    return float3x3 (tangent,binormal,normal);
}

float3 SampleHemisphere(float3 normal,float alpha = 0)
{
    //根据两个角度确定向量,其中alpha是对采样的变换
    float cosTheta = pow( rand(),1.0f / (alpha + 1.0f));
    float sinTheta = sqrt(max(0.0f, 1.0f - cosTheta * cosTheta));
    float phi = 2 * PI * rand();
    float3 tangentSpaceDir = float3(cos(phi) * sinTheta, sin(phi) * sinTheta,cosTheta);

    //坐标转换
    return mul(tangentSpaceDir,GetTangentSpace(normal));
}

HitMaterial CreateHitMaterial(MaterialData s_mat,float2 uv = 0.0)
{
    HitMaterial d_mat;
    d_mat.albedo = s_mat.color.rgb;
    d_mat.alpha = s_mat.color.a;
    d_mat.metallic = s_mat.metallic;
    d_mat.roughness =1 - s_mat.smoothness;
    d_mat.emission = s_mat.emission;
    d_mat.ior = s_mat.ior;
    d_mat.mode = s_mat.mode;
    if(s_mat.albedoIdx >= 0)
    {
        float4 color = _AlbedoTextures.SampleLevel(sampler_AlbedoTextures, float3(uv, s_mat.albedoIdx), 0.0);
        d_mat.albedo = d_mat.albedo * color.rgb;
        d_mat.alpha = d_mat.alpha * color.a;
    }
    if (s_mat.metalIdx >= 0)
    {
        float4 metallicRoughness = _MetallicTextures.SampleLevel(sampler_MetallicTextures, float3(uv, s_mat.metalIdx), 0.0);
        d_mat.metallic = metallicRoughness.r;
        d_mat.roughness = metallicRoughness.a;
    }
    if(s_mat.roughIdx >= 0)
    {
        d_mat.roughness = _RoughnessTextures.SampleLevel(sampler_RoughnessTextures, float3(uv, s_mat.roughIdx), 0.0).x;
        d_mat.roughness = 1.0 - d_mat.roughness;
    }
    if (s_mat.emisIdx >= 0)
    {
        // fetch emission value
        d_mat.emission = d_mat.emission * _EmissionTextures.SampleLevel(sampler_EmissionTextures, float3(uv, s_mat.emisIdx), 0.0).xyz;
    }
    return d_mat;
}

float sdot(float3 x, float3 y, float f = 1.0f)
{
    return saturate(dot(x, y) * f);
}

float energy(float3 color)
{
    //平均颜色
    return dot(color,1.0f / 3.0f);
}

float SmoothnessToPhongAlpha(float s)
{
    return pow(1000.0f,s * s);
}

#endif