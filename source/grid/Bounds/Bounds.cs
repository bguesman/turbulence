using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Turbulence
{

/**
 * @brief: a bounds object is a simple representation of a 3D
 * axis-aligned bounding box.
 * */
class Bounds
{
    Vector4 lower; // Lower bound
    Vector4 upper; // Higher bound

    public Bounds(Vector3 lower, Vector3 upper)
    {
        this.lower = new Vector4(lower.x, lower.y, lower.z, 0);
        this.upper = new Vector4(upper.x, upper.y, upper.z, 1);
    }

    public Bounds(Vector2 x, Vector2 y, Vector2 z) 
        : this(new Vector3(x.x, y.x, z.x), new Vector3(x.y, y.y, z.y))
    {
    }

    public Bounds(float xLow, float xHigh, float yLow, float yHigh, float zLow, float zHigh)
        : this(new Vector3(xLow, yLow, zLow), new Vector3(xHigh, yHigh, zHigh))
    {
    }

    public void Bind(ComputeShader computeShader, string shaderVariableLow, string shaderVariableHigh)
    {
        computeShader.SetVector(shaderVariableLow, this.lower);
        computeShader.SetVector(shaderVariableHigh, this.upper);
    }

    public void Bind(CommandBuffer commandBuffer, string shaderVariableLow, string shaderVariableHigh)
    {
        commandBuffer.SetGlobalVector(shaderVariableLow, this.lower);
        commandBuffer.SetGlobalVector(shaderVariableHigh, this.upper);
    }
}

} // namespace Turbulence