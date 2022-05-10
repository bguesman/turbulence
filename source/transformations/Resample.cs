using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: Copies one grid into another of differing resolution.
 * */
class Resample : ITransformation
{
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Resample";
    const string kKernelGeneric = "GENERIC";
    const string kDownsample2XKernel = "DOWNSAMPLE_2X";
    int handleGeneric, handleDownsample2X;

    // Shader variable names
    const string sTexture = "_Tex";
    const string sTarget = "_Target";
    const string sTextureResolution = "_Tex_resolution";
    const string sTargetResolution = "_Target_resolution";

    // Profiling
    ProfilingSampler profilingSampler;

    public Resample(string name="Resample")
    {
        // Arguments
        this.name = name;

        // Compute shader data
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        this.handleGeneric = computeShader.FindKernel(kKernelGeneric);
        this.handleDownsample2X = computeShader.FindKernel(kDownsample2XKernel);

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid input, IGrid output)
    {
        // Choose kernel
        int handle = SelectKernel(input, output);

        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            Bind(input, output, handle);
            context.DispatchAcrossGrid(output, computeShader, handle);
        }
    }

    private int SelectKernel(IGrid input, IGrid output)
    {
        bool div2Int = (input.Resolution().x / output.Resolution().x) == 2;
        bool rem2Int = (input.Resolution().x % output.Resolution().x) == 0;
        return (div2Int && rem2Int) ? handleDownsample2X : handleGeneric;
    }

    private void Bind(IGrid input, IGrid output, int handle)
    {
        input.Bind(computeShader, handle, sTexture, sTextureResolution);
        output.Bind(computeShader, handle, sTarget, sTargetResolution);
    }
}

} // namespace Turbulence