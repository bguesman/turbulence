#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

namespace GridUtilities
{

struct Neighborhood
{
    uint3 right;
    uint3 left;
    uint3 up;
    uint3 down;
    uint3 forward;
    uint3 backward;
};

Neighborhood GetNeighborhood(uint3 id, uint3 resolution)
{
    int3 idInt = (int3) id;
    Neighborhood n;
    n.right = clamp(idInt + int3(1, 0, 0), 0, resolution - 1);
    n.left = clamp(idInt + int3(-1, 0, 0), 0, resolution - 1);
    n.up = clamp(idInt + int3(0, 1, 0), 0, resolution - 1);
    n.down = clamp(idInt + int3(0, -1, 0), 0, resolution - 1);
    n.forward = clamp(idInt + int3(0, 0, 1), 0, resolution - 1);
    n.backward = clamp(idInt + int3(0, 0, -1), 0, resolution - 1);
    return n;
}

uint3 BoundaryThreadIDToCell(uint3 id, float3 resolution)
{
    if (id.z < 2)
    {
        // X boundary
        return uint3(id.z == 0 ? 0 : (uint) resolution.x - 1, id.x, id.y);
    }
    else if (id.z < 4)
    {
        // Y boundary
        return uint3(id.x, id.z == 2 ? 0 : (uint) resolution.y - 1, id.y);
    }
    else
    {
        // Z boundary
        return uint3(id.x, id.y, id.z == 4 ? 0 : (uint) resolution.z - 1);
    }
}

bool OnBoundary3D(uint3 cell, float3 resolution)
{
    return any(cell == (uint3) 0) || any(cell == (uint3) (resolution.xyz - 1));
}

bool OnBoundary1D(uint cell, float resolution)
{
    return cell == 0 || cell == ((uint) resolution) - 1;
}

}