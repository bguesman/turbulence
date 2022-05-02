using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

enum GridDatatype
{
    eScalar = 0,
    eVector2,
    eVector3
}

/**
 * @brief: an IGrid is a discrete data structure storing
 * 3D fluid data.
 * */
interface IGrid
{
    /**
     * @return: grid resolution.
     * */
    Vector3Int Resolution();

    /**
     * @return: grid datatype.
     * */
    GridDatatype Datatype();

    /**
     * @brief: binds grid to specified shader variable in
     * given compute instance, as well as grid resolution.
     * */
    void Bind(ComputeShader computeShader, int kernel, string shaderVariable,
        string resolutionShaderVariable);

    /**
     * @brief: binds grid to specified shader variable globally, as well
     * as grid resolution.
     * */
    void Bind(CommandBuffer commandBuffer, string shaderVariable, string resolutionShaderVariable);
}

} // namespace Turbulence