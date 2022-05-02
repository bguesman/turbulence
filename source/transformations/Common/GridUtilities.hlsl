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

}