using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: sets entire grid to a constant.
 * */
class Fill : ITransformation
{
    // Public settings
    public Vector3 constant;
    public Bounds bounds;

    // Name of this transformation
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Fill";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sTexture = "_Tex";
    const string sTextureResolution = "_Tex_resolution";
    const string sConstant = "_constant";
    const string sBoundsLow = "_boundLow";
    const string sBoundsHigh = "_boundHigh";

    // Profiling
    ProfilingSampler profilingSampler;

    public Fill(float constant, Bounds bounds=null, string name="Set Constant") 
        : this(new Vector3(constant, 0, 0), bounds, name)
    {
    }

    public Fill(Vector2 constant, Bounds bounds=null, string name="Set Constant") 
        : this(new Vector3(constant.x, constant.y, 0), bounds, name)
    {
    }

    public Fill(Vector3 constant, Bounds bounds=null, string name="Set Constant")
    {
        // Arguments
        this.constant = constant;
        this.bounds = bounds == null ? new Bounds(0, 1, 0, 1, 0, 1) : bounds;
        this.name = name;

        // Compute shader data
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid grid)
    {
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            Bind(grid);
            context.DispatchAcrossGrid(grid, computeShader, handle);
        }
    }

    private void Bind(IGrid grid)
    {
        grid.Bind(computeShader, handle, sTexture, sTextureResolution);
        bounds.Bind(computeShader, sBoundsLow, sBoundsHigh);
        computeShader.SetVector(sConstant, new Vector4(constant.x, constant.y, constant.z, 0));
    }
}

} // namespace Turbulence