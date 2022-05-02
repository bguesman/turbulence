using UnityEngine;

namespace Turbulence
{

/**
 * @brief: Adds an amount to a grid.
 * */
class Add : ITransformation
{
    // Public settings
    public Vector3 constant;
    public Bounds bounds;

    // Name of this transformation
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "Add";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sTexture = "_Tex";
    const string sTextureResolution = "_Tex_resolution";
    const string sConstant = "_constant";
    const string sDt = "_dt";
    const string sBoundsLow = "_boundLow";
    const string sBoundsHigh = "_boundHigh";

    public Add(float constant, Bounds bounds=null, string name="Set Constant") 
        : this(new Vector3(constant, 0, 0), bounds, name)
    {
    }

    public Add(Vector2 constant, Bounds bounds=null, string name="Set Constant") 
        : this(new Vector3(constant.x, constant.y, 0), bounds, name)
    {
    }

    public Add(Vector3 constant, Bounds bounds=null, string name="Set Constant")
    {
        // Arguments
        this.constant = constant;
        this.bounds = bounds == null ? new Bounds(0, 1, 0, 1, 0, 1) : bounds;
        this.name = name;

        // Compute shader data
        this.computeShader = Resources.Load<ComputeShader>(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);
    }

    public void Transform(IGrid grid, float dt)
    {
        Bind(grid, dt);
        TransformationUtilities.DispatchAcrossGrid(grid, computeShader, handle);
    }

    private void Bind(IGrid grid, float dt)
    {
        grid.Bind(computeShader, handle, sTexture, sTextureResolution);
        bounds.Bind(computeShader, sBoundsLow, sBoundsHigh);
        computeShader.SetVector(sConstant, new Vector4(constant.x, constant.y, constant.z, 0));
        computeShader.SetFloat(sDt, dt);
    }
}

} // namespace Turbulence