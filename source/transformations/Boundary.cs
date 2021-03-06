using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: imposes a boundary condition on a grid.
 * */
class Boundary : ITransformation
{

    /**
     * @brief: enum for specifying type of boundary condition.
     * */
    public enum BoundaryCondition
    {
        eNoSlip = 0,
        eFreeSlip,
        eNeumann,
        eNumBoundaryConditions
    }

    // Name of this transformation
    BoundaryCondition condition;
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Boundary";
    string[] kKernels = {"NO_SLIP", "FREE_SLIP", "NEUMANN"};
    int[] handles = new int[(int) BoundaryCondition.eNumBoundaryConditions];

    // Shader variable names
    const string sTexture = "_Tex";
    const string sTextureResolution = "_Tex_resolution";

    // Profiling
    ProfilingSampler profilingSampler;

    public Boundary(BoundaryCondition condition, string name="Set Constant")
    {
        // Arguments
        this.condition = condition;
        this.name = name;

        // Compute shader data
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        for (int i = 0; i < (int) BoundaryCondition.eNumBoundaryConditions; i++)
        {
            this.handles[i] = computeShader.FindKernel(kKernels[i]);
        }

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid grid)
    {   
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            // Select compute handle for appropriate boundary condition
            int handle = handles[(int) condition];
            Bind(grid, handle);
            context.DispatchAcrossBoundary(grid, computeShader, handle);
        }
    }

    private void Bind(IGrid grid, int handle)
    {
        grid.Bind(computeShader, handle, sTexture, sTextureResolution);
    }
}

} // namespace Turbulence