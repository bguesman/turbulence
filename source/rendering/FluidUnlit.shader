Shader "Turbulence/FluidUnlit"
{
    Properties
    {
        _Density ("Density", Range(0,1)) = 0.25
        _ScatteringColor ("Scattering Color", Color) = (1,1,1,1)
        _AbsorptionColor ("Absorption Color", Color) = (1,1,1,1)
        _DensityTex ("Density", 3D) = "white" {}
    }
    SubShader
    {
        Tags {"RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #pragma target 3.5

            #include "UnityCG.cginc"

            // HDRP lights
            // #include "UnityDeferredLibrary.cginc"
            // #include "UnityLightingCommon.cginc"
            #include "Assets/Turbulence/source/rendering/Geometry.hlsl"
            #include "Assets/Turbulence/source/rendering/lighting/lights.hlsl"
            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/ShaderVariablesLightLoop.hlsl"
            // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            // #include "../../Lighting/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float depth : TEXCOORD0;
                float4 positionCS: TEXCOORD1;
                float4 vPos: TEXCOORD2;
                UNITY_FOG_COORDS(1)
            };

            // Properties
            half _Density;
            fixed4 _ScatteringColor;
            fixed4 _AbsorptionColor;
            sampler3D _DensityTex;
            SamplerState sampler_DensityTex;

            uniform int _TEST_VARIABLE;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                o.positionCS = ComputeScreenPos(o.vertex);
                o.vPos = v.vertex;
                COMPUTE_EYEDEPTH(o.depth);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Remap density.
                float densityRemap = rcp(1.01 - _Density) * _Density;

                // Get ray extents in world space.
                float3 o = _WorldSpaceCameraPos;
                float3 d = -normalize(WorldSpaceViewDir(i.vPos));

                // Intersect AABB.
                float3 oOS = mul(unity_WorldToObject, float4(o, 1)).xyz;
                float3 dOS = mul(unity_WorldToObject, float4(d, 0)).xyz;
                float3 boxLowBound = float3(-0.5, -0.5, -0.5);
                float3 boxHighBound = float3(0.5, 0.5, 0.5);
                float2 intersection = Geometry::rayBoxDst(boxLowBound, boxHighBound, oOS, 1/dOS);

                float start = Geometry::pointInBox(boxLowBound, boxHighBound, oOS) ? 0 : intersection.x;
                float t = intersection.y;

                // Raymarch 3D Texture to compute transmittance and lighting.
                float3 opticalDepth = 0;
                float3 light = 0;
                const int kSamples = 32;
                float step = t * rcp((float) kSamples);
                for (int sample = 0; sample < kSamples; sample++)
                {
                    // Generate our sample point
                    float sampleT = start + step * (sample + 0.5);
                    float3 samplePoint = o + d * sampleT;

                    // Generate UV in box and sample density
                    float3 sampleUV = Geometry::uvAABB(samplePoint, unity_WorldToObject);
                    float density = densityRemap * tex3D(_DensityTex, sampleUV);

                    // Add to global optical depth estimate along ray
                    float3 localOpticalDepth = density * step * _AbsorptionColor;
                    opticalDepth += localOpticalDepth;

                    // Ambient light
                    light += exp(-opticalDepth) * (1 - exp(-localOpticalDepth)) * half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

                    // TODO: energy-conserving integration
                    for (int l = 0; l < _TurbulenceNumDirectionalLights; l++)
                    {
                        // Estimate self-shadowing
                        DirectionalLightMirror directionalLight = _TurbulenceDirectionalLights[l];
                        float3 L = -directionalLight.forward;
                        float3 sampleOS = mul(unity_WorldToObject, float4(samplePoint, 1)).xyz;
                        float3 L_OS = mul(unity_WorldToObject, float4(L, 0)).xyz;
                        
                        float2 shadowIntersection = Geometry::rayBoxDst(boxLowBound, boxHighBound, sampleOS, 1/L_OS);
                        
                        const int kShadowSamples = 4;
                        float shadowStep = shadowIntersection.y * rcp((float) kShadowSamples);
                        float shadowOpticalDepth = 0;
                        for (int shadowSample = 0; shadowSample < kShadowSamples; shadowSample++)
                        {
                            float shadowSampleT = shadowStep * (shadowSample + 0.5);
                            float3 shadowSamplePoint = samplePoint + L * shadowSampleT;
                            float3 shadowUV = Geometry::uvAABB(shadowSamplePoint, unity_WorldToObject);
                            shadowOpticalDepth += tex3D(_DensityTex, shadowUV);
                            // shadowOpticalDepth += _DensityTex.Sample(sampler_DensityTex, shadowUV);
                        }
                        
                        shadowOpticalDepth *= shadowStep * densityRemap * _AbsorptionColor;
                        float3 shadow = exp(-shadowOpticalDepth);

                        light += shadow * exp(-opticalDepth) * (1 - exp(-localOpticalDepth)) * (directionalLight.color);
                    }
                }

                // Use depth to compute transmittance.
                float3 T = exp(-opticalDepth);
                float alpha = 1 - min(min(T.x, T.y), T.z);
                light *= _ScatteringColor;

                fixed4 col = fixed4(light, alpha);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
