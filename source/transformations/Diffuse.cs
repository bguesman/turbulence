using UnityEngine;

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

    public Diffuse(float viscosity, string name="Diffuse")
    {
        // Arguments
        this.viscosity = viscosity;
        this.name = name;

        // Compute shader data
        this.computeShader = Resources.Load<ComputeShader>(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);
    }

    public void Transform(IGrid input, IGrid output, float dt)
    {
        Bind(input, output, dt);
        TransformationUtilities.DispatchAcrossGrid(output, computeShader, handle);
    }

    private void Bind(IGrid input, IGrid output, float dt)
    {
        input.Bind(computeShader, handle, sTexture, sTextureResolution);
        output.Bind(computeShader, handle, sTarget, sTextureResolution);
        computeShader.SetFloat(sViscosity, viscosity);
        computeShader.SetFloat(sDt, dt);
    }
}

} // namespace Turbulence