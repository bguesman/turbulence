using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: One step in a jacobi matrix iteration problem.
 * */
class JacobiStep : ITransformation
{
    // Name of this transformation
    string name;
    public float alpha, beta;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "JacobiStep";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sA = "_A";
    const string sB = "_B";
    const string sTarget = "_Target";
    const string sTargetResolution = "_Target_resolution";
    const string sAlpha = "_alpha";
    const string sBeta = "_beta";

    // Profiling
    ProfilingSampler profilingSampler;

    public JacobiStep(float alpha, float beta, string name="Jacobi Step")
    {
        // Arguments
        this.name = name;
        this.alpha = alpha;
        this.beta = beta;

        // Compute shader data
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid A, IGrid B, IGrid Output)
    {
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            Bind(A, B, Output);
            context.DispatchAcrossGrid(Output, computeShader, handle);
        }
    }

    private void Bind(IGrid A, IGrid B, IGrid Output)
    {
        A.Bind(computeShader, handle, sA, sTargetResolution);
        B.Bind(computeShader, handle, sB, sTargetResolution);
        Output.Bind(computeShader, handle, sTarget, sTargetResolution);
        computeShader.SetFloat(sAlpha, alpha);
        computeShader.SetFloat(sBeta, beta);
    }
}

} // namespace Turbulence