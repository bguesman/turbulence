using UnityEngine;

namespace Turbulence
{

/**
 * @brief: sets entire grid to a constant.
 * */
class Advect : ITransformation
{
    // Name of this transformation
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Advect";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    // V is the advector quantity
    const string sV = "_V";
    const string sVResolution = "_V_resolution";
    // D is the advected quantity
    const string sDIn = "_DIn";
    const string sDOut = "_DOut";
    const string sDResolution = "_D_resolution";
    const string sDt = "_dt";

    public Advect(string name="Advect")
    {
        // Arguments
        this.name = name;

        // Compute shader data
        this.computeShader = Resources.Load<ComputeShader>(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);
    }

    /**
     * @brief: advects grid D by grid V.
     * */
    public void Transform(IGrid DIn, IGrid DOut, IGrid V, float dt)
    {
        Bind(DIn, DOut, V, dt);
        // Dispatch across D, since we are writing to D.
        TransformationUtilities.DispatchAcrossGrid(DOut, computeShader, handle);
    }

    private void Bind(IGrid DIn, IGrid DOut, IGrid V, float dt)
    {
        DIn.Bind(computeShader, handle, sDIn, sDResolution);
        DOut.Bind(computeShader, handle, sDOut, sDResolution);
        V.Bind(computeShader, handle, sV, sVResolution);
        computeShader.SetFloat(sDt, dt);
    }
}

} // namespace Turbulence