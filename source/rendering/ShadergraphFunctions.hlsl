#ifndef TURBULENCE_SHADERGRAPH_FUNCTIONS_HLSL
#define TURBULENCE_SHADERGRAPH_FUNCTIONS_HLSL

#include "Geometry.hlsl"

struct IntersectBoxInput
{
    Geometry::Ray ray;
    Geometry::Bounds bounds;
};
struct IntersectBoxOutput
{
    float hit;
    float dist;
};
void IntersectBox(in IntersectBoxInput i, out IntersectBoxOutput o)
{
    float2 hitAndDist = Geometry::RayBoxDst(i.ray, i.bounds);
    o.hit = hitAndDist.x;
    o.dist = hitAndDist.y;
}
void IntersectBox_float(in float3 rayOrigin, in float3 rayDirection, 
    in float3 boundsLow, in float3 boundsHigh, out float hit, out float dist)
{
    // Set up input
    IntersectBoxInput i;
    IntersectBoxOutput o;
    i.ray.origin = rayOrigin;
    i.ray.direction = rayDirection;
    i.bounds.low = boundsLow;
    i.bounds.high = boundsHigh;

    // Call the function
    IntersectBox(i, o);

    // Map back outputs
    hit = o.hit;
    dist = o.dist;
}
void IntersectBox_half(in half3 rayOrigin, in half3 rayDirection, 
    in half3 boundsLow, in half3 boundsHigh, out half hit, out half dist)
{
    // Set up input
    IntersectBoxInput i;
    IntersectBoxOutput o;
    i.ray.origin = rayOrigin;
    i.ray.direction = rayDirection;
    i.bounds.low = boundsLow;
    i.bounds.high = boundsHigh;

    // Call the function
    IntersectBox(i, o);

    // Map back outputs
    hit = o.hit;
    dist = o.dist;
}


TEXTURE3D(_DensityTexture);
struct RaymarchInput
{
    Geometry::Ray ray;
    float hit;
    float dist;
    float4x4 worldToObject;
    UnityTexture3D densityTex;
};
struct RaymarchOutput
{
    float3 color;
    float3 alpha;
};
void Raymarch(in RaymarchInput i, out RaymarchOutput o)
{
    // Initialize to default result.
    o.alpha = 1;
    o.color = 0;

    float3 opticalDepth = 0;
    float3 lighting = 1;
    const int kSamples = 32;
    float step = i.dist * rcp((float) kSamples);
    for (int sample = 0; sample < kSamples; sample++)
    {
        // Generate our sample point
        float sampleT = i.hit + step * (sample + 0.5);
        float3 samplePoint = i.ray.origin + i.ray.direction * sampleT;

        // Generate UV in box and sample density
        float3 sampleUV = Geometry::UnitUVAABB(samplePoint, i.worldToObject);
        float density = 10 * SAMPLE_TEXTURE3D_LOD(i.densityTex.tex, s_linear_clamp_sampler, sampleUV, 0).x;// i.densityTex.tex.Sample(s_linear_clamp_sampler, sampleUV);

        // Add to global optical depth estimate along ray
        float3 localOpticalDepth = density * step;// * _AbsorptionColor;
        opticalDepth += localOpticalDepth;
    }

    // Use depth to compute transmittance.
    float3 T = exp(-opticalDepth);
    o.alpha = 1 - T;
    o.color = lighting;
}
void Raymarch_float(in float3 rayOrigin, in float3 rayDirection,
    in float hit, in float dist, in float4x4 worldToObject,
    in UnityTexture3D densityTex,
    out float3 color,
    out float3 alpha)
{
    // Prep inputs/outputs
    RaymarchInput i;
    RaymarchOutput o;
    i.ray.origin = rayOrigin;
    i.ray.direction = rayDirection;
    i.hit = hit;
    i.dist = dist;
    i.worldToObject = worldToObject;
    i.densityTex = densityTex;

    // Call function
    Raymarch(i, o);

    // Transfer back output
    color = o.color;
    alpha = o.alpha;
}
void Raymarch_half(in half3 rayOrigin, in half3 rayDirection,
    in half hit, in half dist, in half4x4 worldToObject,
    in UnityTexture3D densityTex, //
    out float3 color,
    out float3 alpha)
{
    // Prep inputs/outputs
    RaymarchInput i;
    RaymarchOutput o;
    i.ray.origin = rayOrigin;
    i.ray.direction = rayDirection;
    i.hit = hit;
    i.dist = dist;
    i.worldToObject = worldToObject;
    i.densityTex = densityTex;

    // Call function
    Raymarch(i, o);

    // Transfer back output
    color = o.color;
    alpha = o.alpha;
}




#endif // TURBULENCE_SHADERGRAPH_FUNCTIONS_HLSL