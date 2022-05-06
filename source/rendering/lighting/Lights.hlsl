#ifndef TURBULENCE_LIGHTING_INCLUDED
#define TURBULENCE_LIGHTING_INCLUDED    

#include "Lights.cs.hlsl"

StructuredBuffer<DirectionalLightMirror> _TurbulenceDirectionalLights;
int _TurbulenceNumDirectionalLights;

#endif // TURBULENCE_LIGHTING_INCLUDED