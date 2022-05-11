using UnityEngine;
using UnityEngine.Rendering;

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
 * @brief: class detailing transformation globals passed to all
 * transform functions.
 * */
class TransformationContext
{
    public CommandBuffer cmd { get; }
    public float dt { get; }
    
    public TransformationContext(float dt, string commandBufferName="Turbulence")
    {
        this.cmd = CommandBufferPool.Get(commandBufferName);
        this.dt = dt;
        this.cmd.Clear();
    }

    public void ExecuteCommands()
    {
        Graphics.ExecuteCommandBuffer(this.cmd);   
    }

    public void DispatchAcrossGrid(IGrid grid, ComputeShader computeShader, int handle)
    {
        uint groupSizeX = 1, groupSizeY = 1, groupSizeZ = 1;
        computeShader.GetKernelThreadGroupSizes(handle, out groupSizeX, out groupSizeY, out groupSizeZ);
        cmd.DispatchCompute(computeShader, handle, 
            (int) Mathf.Max(1, Mathf.Ceil(grid.Resolution().x / groupSizeX)),
            (int) Mathf.Max(1, Mathf.Ceil(grid.Resolution().y / groupSizeY)),
            (int) Mathf.Max(1, Mathf.Ceil(grid.Resolution().z / groupSizeZ)));
    }

    public void DispatchAcrossBoundary(IGrid grid, ComputeShader computeShader, int handle)
    {
        // Get compute shader thread pool sizes
        uint groupSizeX = 1, groupSizeY = 1, groupSizeZ = 1;
        computeShader.GetKernelThreadGroupSizes(handle, out groupSizeX, out groupSizeY, out groupSizeZ);

        // Dispatch as 6 "slices" in the z-dimension, each of max resolution
        // in x, y, and z directions. We'll distribution the slices as follows:
        //  1: x low boundary
        //  2: x high boundary
        //  3: y low boundary
        //  etc...
        int maxRes = (int) Mathf.Max(1, Mathf.Max(Mathf.Max(grid.Resolution().x, grid.Resolution().y), grid.Resolution().z));
        int groupsX = (int) Mathf.Max(1, Mathf.Ceil(maxRes / groupSizeX));
        int groupsY = (int) Mathf.Max(1, Mathf.Ceil(maxRes / groupSizeY));
        int groupsZ = (int) Mathf.Max(1, Mathf.Ceil(6 / groupSizeZ));
     
        cmd.DispatchCompute(computeShader, handle, groupsX, groupsY, groupsZ);
    }

    ~TransformationContext()
    {
        CommandBufferPool.Release(this.cmd);
    }
}

/**
 * Static utility functions for making transform application easier.
 * */
static class TransformationUtilities
{
    public static ComputeShader LoadComputeShader(string name)
    {
        return UnityEngine.Object.Instantiate<ComputeShader>(Resources.Load<ComputeShader>(name));
    }
}

} // namespace Turbulence