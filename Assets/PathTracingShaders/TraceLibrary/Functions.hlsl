static const float PI = 3.14159265f;
float _Seed;
float2 _Pixel;

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