using UnityEngine;

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

    public Divergence(string name="Divergence")
    {
        // Arguments
        this.name = name;

        // Compute shader data
        this.computeShader = Resources.Load<ComputeShader>(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);
    }

    public void Transform(IGrid input, IGrid output)
    {
        Bind(input, output);
        TransformationUtilities.DispatchAcrossGrid(output, computeShader, handle);
    }

    private void Bind(IGrid input, IGrid output)
    {
        input.Bind(computeShader, handle, sTexture, sTextureResolution);
        output.Bind(computeShader, handle, sTarget, sTextureResolution);
    }
}

} // namespace Turbulence