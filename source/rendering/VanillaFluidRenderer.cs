using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Turbulence
{

class VanillaFluidRenderer : CustomPass
{
    // Ideally at some point all fluid sims will be collected and rendered
    // together, but for now just render one.
    public FluidSimulation simulation;

    // Compute shader
    ComputeShader computeShader;
    const string kComputeShaderName = "VanillaFluidRenderer";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    // V is the advector quantity
    const string sFramebuffer = "_Framebuffer";
    const string sDensity = "_Density";
    const string sTransform = "_transform";

    // Profiling
    ProfilingSampler profilingSampler;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        this.computeShader = TransformationUtilities.LoadComputeShader(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);

        // Profiling
        this.profilingSampler = new ProfilingSampler("Render Turbulence Fluid");
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
    {
        if (simulation == null)
            return;
        
        // Set shader variables
        RTHandle framebuffer = hdCamera.GetCurrentFrameRT((int) HDCameraFrameHistoryType.ColorBufferMipChain);
        computeShader.SetTexture(handle, sDensity, simulation.DensityGrid().GetTexture());
        computeShader.SetTexture(handle, sFramebuffer, framebuffer);
        computeShader.SetMatrix(sTransform, simulation.gameObject.transform.worldToLocalMatrix);

        // Render!
        uint groupSizeX = 1, groupSizeY = 1, groupSizeZ = 1;
        computeShader.GetKernelThreadGroupSizes(handle, out groupSizeX, out groupSizeY, out groupSizeZ);
        cmd.DispatchCompute(computeShader, handle, 
            (int) Mathf.Ceil(framebuffer.rt.width / groupSizeX),
            (int) Mathf.Ceil(framebuffer.rt.height / groupSizeY),
            (int) Mathf.Ceil(framebuffer.rt.volumeDepth / groupSizeZ));
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}

} // namespace Turbulence