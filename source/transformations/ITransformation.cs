using UnityEngine;

namespace Turbulence
{

/**
 * @brief: an ITransformation is an abstract representation of an
 * operator that acts on a collection of grids.
 * */
interface ITransformation
{

}

/**
 * Static utility functions for making transform application easier.
 * */
static class TransformationUtilities
{
    public static void DispatchAcrossGrid(IGrid grid, ComputeShader computeShader, int handle)
    {
        uint groupSizeX = 1, groupSizeY = 1, groupSizeZ = 1;
        computeShader.GetKernelThreadGroupSizes(handle, out groupSizeX, out groupSizeY, out groupSizeZ);
        computeShader.Dispatch(handle, 
            (int) Mathf.Ceil(grid.Resolution().x / groupSizeX),
            (int) Mathf.Ceil(grid.Resolution().y / groupSizeY),
            (int) Mathf.Ceil(grid.Resolution().z / groupSizeZ));
    }
}

} // namespace Turbulence