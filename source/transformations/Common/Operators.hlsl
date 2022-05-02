#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

namespace Operators
{

float3 Laplacian(float3 l, float3 r, float3 u, float3 d, float3 f, float3 b, float3 c)
{
    return l + r + d + u + b + f - 6 * c;
}

}