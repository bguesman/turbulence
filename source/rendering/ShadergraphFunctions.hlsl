#ifndef TURBULENCE_SHADERGRAPH_FUNCTIONS_HLSL
#define TURBULENCE_SHADERGRAPH_FUNCTIONS_HLSL

#include "Geometry.hlsl"
#include "lighting/Lights.hlsl"

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

struct FluidMaterial
{
    UnityTexture3D densityTex;
    float density;
    float3 extinctionColor;
    float3 scatteringColor;
    float ambientDimmer;
    int primarySamples;
    int shadowSamples;
};
float3 transmittance(Geometry::Ray ray, float4x4 worldToObject, FluidMaterial material)
{
    Geometry::Ray rayOS = ray.transform(worldToObject);
    float2 shadowIntersection = Geometry::RayBoxDst(rayOS, Geometry::Bounds::Unit());
    
    float shadowStep = shadowIntersection.y * rcp((float) material.shadowSamples);
    float shadowOpticalDepth = 0;
    for (int shadowSample = 0; shadowSample < material.shadowSamples; shadowSample++)
    {
        float3 shadowSamplePoint = ray.origin + ray.direction * shadowStep * (shadowSample + 0.5);
        float3 shadowUV = Geometry::UnitUVAABB(shadowSamplePoint, worldToObject);
        shadowOpticalDepth += SAMPLE_TEXTURE3D_LOD(material.densityTex.tex, s_linear_clamp_sampler, shadowUV, 4).x;
    }

    shadowOpticalDepth *= shadowStep * material.density * material.extinctionColor;
    return exp(-shadowOpticalDepth);
}

float3 fibonacciHemisphere(int i, int n) {
    float i_mid = i + 0.5;
    float cos_phi = 1 - i/float(n);
    float sin_phi = sqrt(1 - cos_phi * cos_phi);
    float theta = 2 * PI * i * rcp(1.6180339887498948482);
    float cos_theta = cos(theta);
    float sin_theta = sqrt(1 - cos_theta * cos_theta);
    return float3(cos_theta * sin_phi, cos_phi, sin_theta * sin_phi);
}
float3 ambientOcclusion(float3 p, float4x4 worldToObject, FluidMaterial material)
{   
    float3 averageTransmittance = 0;
    int kSamples = 3;
    for (int i = 0; i < kSamples; i++)
    {
        Geometry::Ray r = {p, fibonacciHemisphere(i, kSamples)};
        averageTransmittance += transmittance(r, worldToObject, material);
    }
    return averageTransmittance * rcp((float) kSamples);
}

struct RaymarchInput
{
    Geometry::Ray ray;
    float hit;
    float dist;
    float4x4 worldToObject;
    FluidMaterial material;
    float exposure;
    float jitter;
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
    float3 lighting = 0;
    float step = i.dist * rcp((float) i.material.primarySamples);
    for (int sample = 0; sample < i.material.primarySamples; sample++)
    {
        // Generate our sample point
        float sampleT = i.hit + step * (sample + i.jitter);
        float3 samplePoint = i.ray.origin + i.ray.direction * sampleT;

        // Generate UV in box and sample density
        float3 sampleUV = Geometry::UnitUVAABB(samplePoint, i.worldToObject);
        float density = i.material.density * SAMPLE_TEXTURE3D_LOD(i.material.densityTex.tex, s_linear_clamp_sampler, sampleUV, 0).x;// i.densityTex.tex.Sample(s_linear_clamp_sampler, sampleUV);

        // Add to global optical depth estimate along ray
        float3 localOpticalDepth = density * step * i.material.extinctionColor;

        // Accumulate lighting
        float3 lightingIntegration = exp(-opticalDepth) * (1 - exp(-localOpticalDepth));
        lighting += ambientOcclusion(samplePoint, i.worldToObject, i.material) * lightingIntegration * i.exposure * i.material.ambientDimmer * float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
        for (int l = 0; l < _TurbulenceNumDirectionalLights; l++)
        {
            DirectionalLightMirror directionalLight = _TurbulenceDirectionalLights[l];
            Geometry::Ray shadowRay = {samplePoint, -directionalLight.forward};
            float3 shadow = transmittance(shadowRay, i.worldToObject, i.material);
            lighting += shadow * lightingIntegration * directionalLight.color;
        }

        opticalDepth += localOpticalDepth;
    }

    // Use depth to compute transmittance, and subtract from 1 to get alpha.
    float3 scatteringAlbedo = i.material.scatteringColor * max(1e-6, rcp(i.material.extinctionColor));
    o.alpha = 1 - exp(-opticalDepth);
    o.color = scatteringAlbedo * lighting;
}
void Raymarch_float(
    // ray params
    in float3 rayOrigin, 
    in float3 rayDirection,
    in float hit, 
    in float dist, 
    in float4x4 worldToObject,
    // material params
    in UnityTexture3D densityTex,
    in float density,
    in float3 extinctionColor,
    in float3 scatteringColor,
    in float ambientDimmer,
    in float primarySamples,
    in float shadowSamples,
    // lighting context
    in float exposure,
    // random noise
    in float jitter,
    // output
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
    i.material.densityTex = densityTex;
    i.material.density = density;
    i.material.extinctionColor = extinctionColor;
    i.material.scatteringColor = scatteringColor;
    i.material.ambientDimmer = ambientDimmer;
    i.material.primarySamples = primarySamples;
    i.material.shadowSamples = shadowSamples;
    i.exposure = exposure;
    i.jitter = jitter;

    // Call function
    Raymarch(i, o);

    // Transfer back output
    color = o.color;
    alpha = o.alpha;
}
void Raymarch_half(
    // ray params
    in half3 rayOrigin, 
    in half3 rayDirection,
    in half hit, 
    in half dist, 
    in half4x4 worldToObject,
    // material params
    in UnityTexture3D densityTex,
    in half density,
    in half3 extinctionColor,
    in half3 scatteringColor,
    in float ambientDimmer,
    in half primarySamples,
    in half shadowSamples,
    // lighting context
    in half exposure,
    // random noise
    in float jitter,
    // output
    out half3 color,
    out half3 alpha)
{
    // Prep inputs/outputs
    RaymarchInput i;
    RaymarchOutput o;
    i.ray.origin = rayOrigin;
    i.ray.direction = rayDirection;
    i.hit = hit;
    i.dist = dist;
    i.worldToObject = worldToObject;
    i.material.densityTex = densityTex;
    i.material.density = density;
    i.material.extinctionColor = extinctionColor;
    i.material.scatteringColor = scatteringColor;
    i.material.ambientDimmer = ambientDimmer;
    i.material.primarySamples = primarySamples;
    i.material.shadowSamples = shadowSamples;
    i.exposure = exposure;
    i.jitter = jitter;

    // Call function
    Raymarch(i, o);

    // Transfer back output
    color = o.color;
    alpha = o.alpha;
}




#endif // TURBULENCE_SHADERGRAPH_FUNCTIONS_HLSL