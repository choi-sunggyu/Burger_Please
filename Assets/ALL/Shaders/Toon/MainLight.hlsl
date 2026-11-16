
#ifndef CUSTOM_TOON_MAIN_LIGHT_INCLUDED
#define CUSTOM_TOON_MAIN_LIGHT_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

void MainLight_float(
    out float3 Direction,
    out float3 Color)
{
#ifdef SHADERGRAPH_PREVIEW
    Direction = normalize(float3(0.5, 1.0, 0.5));
    Color = float3(1.0, 1.0, 1.0);
#else
    Light mainLight = GetMainLight();

    // No Directional Light: URP reports a black light along +Z, which would shade every surface facing the camera.
    Direction = any(mainLight.color) ? mainLight.direction : normalize(float3(0.3, 0.8, -0.5));
    Color = mainLight.color;
#endif
}

#endif
