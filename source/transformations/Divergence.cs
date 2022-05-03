using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: Computes divergence of a grid.
 * */
class Divergence : ITransformation
{
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Divergence";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sTexture = "_Tex";
    const string sTarget = "_Target";
    const string sTextureResolution = "_Tex_resolution";

    // Profiling
    ProfilingSampler profilingSampler;

    public Divergence(string name="Divergence")
    {
        // Arguments
        this.name = name;

        // Compute shader data
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid input, IGrid output)
    {
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            Bind(input, output);
            context.DispatchAcrossGrid(output, computeShader, handle);
        }
    }

    private void Bind(IGrid input, IGrid output)
    {
        input.Bind(computeShader, handle, sTexture, sTextureResolution);
        output.Bind(computeShader, handle, sTarget, sTextureResolution);
    }
}

} // namespace Turbulence