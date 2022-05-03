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
            (int) Mathf.Ceil(grid.Resolution().x / groupSizeX),
            (int) Mathf.Ceil(grid.Resolution().y / groupSizeY),
            (int) Mathf.Ceil(grid.Resolution().z / groupSizeZ));
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