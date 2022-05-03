using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: Projects pressure out of velocity.
 * */
class Project : ITransformation
{
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Project";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sTexture = "_Tex";
    const string sTarget = "_Target";
    const string sTextureResolution = "_Tex_resolution";

    // Profiling
    ProfilingSampler profilingSampler;

    public Project(string name="Project")
    {
        // Arguments
        this.name = name;

        // Compute shader data
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid pressure, IGrid velocity)
    {   
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            Bind(pressure, velocity);
            context.DispatchAcrossGrid(velocity, computeShader, handle);
        }
    }

    private void Bind(IGrid pressure, IGrid velocity)
    {
        pressure.Bind(computeShader, handle, sTexture, sTextureResolution);
        velocity.Bind(computeShader, handle, sTarget, sTextureResolution);
    }
}

} // namespace Turbulence