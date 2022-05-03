#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

namespace Operators
{

float3 Laplacian(float3 r, float3 l, float3 u, float3 d, float3 f, float3 b, float3 c)
{
    return l + r + d + u + b + f - 6 * c;
}

float Divergence(float3 r, float3 l, float3 u, float3 d, float3 f, float3 b)
{
    return 0.5 * (r.x - l.x + u.y - d.y + f.z - b.z);
}

float3 Curl(float3 r, float3 l, float3 u, float3 d, float3 f, float3 b)
{
    return 0.5 * float3((u.z - d.z) - (f.y - b.y), (f.x - b.x) - (r.z - l.z), (r.y - l.y) - (u.x - d.x));
}

float3 Gradient(float r, float l, float u, float d, float f, float b)
{
    return 0.5 * float3(r - l, u - d, f - b);
}

}