using UnityEngine;

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

    public VorticityConfinement(float vorticity, string name="VorticityConfinement")
    {
        // Arguments
        this.vorticity = vorticity;
        this.name = name;

        // Compute shader data
        this.computeShader = Resources.Load<ComputeShader>(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);

        // Curl computation
        curlStep = new Curl("Vorticity Confinement Curl");
    }

    public void Transform(IGrid velocity, IGrid curl, float dt)
    {
        // Compute curl
        curlStep.Transform(velocity, curl);

        // Use it to compute vorticity confinement force
        Bind(velocity, curl, dt);
        TransformationUtilities.DispatchAcrossGrid(velocity, computeShader, handle);
    }

    private void Bind(IGrid velocity, IGrid curl, float dt)
    {
        curl.Bind(computeShader, handle, sCurl, sTextureResolution);
        velocity.Bind(computeShader, handle, sTarget, sTextureResolution);
        computeShader.SetFloat(sVorticity, vorticity);
        computeShader.SetFloat(sDt, dt);
    }
}

} // namespace Turbulence