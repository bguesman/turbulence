using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: Adds vorticity confinement force to grid.
 * */
class VorticityConfinement : ITransformation
{
    public float vorticity;
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "VorticityConfinement";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sCurl = "_Curl";
    const string sTarget = "_Target";
    const string sTextureResolution = "_Tex_resolution";
    const string sVorticity = "_vorticity";
    const string sDt = "_dt";

    // curl computation step
    Curl curlStep;

    // Profiling
    ProfilingSampler profilingSampler;
    
    public VorticityConfinement(float vorticity, string name="VorticityConfinement")
    {
        // Arguments
        this.vorticity = vorticity;
        this.name = name;

        // Compute shader data
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);

        // Curl computation
        curlStep = new Curl("Vorticity Confinement Curl");

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid velocity, IGrid curl)
    {   
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            // Compute curl
            curlStep.Transform(context, velocity, curl);

            // Use it to compute vorticity confinement force
            Bind(context, velocity, curl);
            context.DispatchAcrossGrid(velocity, computeShader, handle);
        }
    }

    private void Bind(TransformationContext context, IGrid velocity, IGrid curl)
    {
        curl.Bind(computeShader, handle, sCurl, sTextureResolution);
        velocity.Bind(computeShader, handle, sTarget, sTextureResolution);
        computeShader.SetFloat(sVorticity, vorticity);
        computeShader.SetFloat(sDt, context.dt);
    }
}

} // namespace Turbulence