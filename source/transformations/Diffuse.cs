using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: Adds an amount to a grid.
 * */
class Diffuse : ITransformation
{
    public float viscosity;
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Diffuse";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sTexture = "_Tex";
    const string sTarget = "_Target";
    const string sTextureResolution = "_Tex_resolution";
    const string sViscosity = "_viscosity";
    const string sDt = "_dt";

    // Profiling
    ProfilingSampler profilingSampler;

    public Diffuse(float viscosity, string name="Diffuse")
    {
        // Arguments
        this.viscosity = viscosity;
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
            Bind(context, input, output);
            context.DispatchAcrossGrid(output, computeShader, handle);
        }
    }

    private void Bind(TransformationContext context, IGrid input, IGrid output)
    {
        input.Bind(computeShader, handle, sTexture, sTextureResolution);
        output.Bind(computeShader, handle, sTarget, sTextureResolution);
        computeShader.SetFloat(sViscosity, viscosity);
        computeShader.SetFloat(sDt, context.dt);
    }
}

} // namespace Turbulence