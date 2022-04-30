using UnityEngine;

namespace Turbulence
{

/**
 * @brief: sets entire grid to a constant.
 * */
class FillConstant : ITransformation
{
    // Public settings
    public Vector3 constant;

    // Name of this transformation
    string name;

    // Compute shader instance
    ComputeShader computeShader;
    const string kComputeShaderName = "FillConstant";
    const string kKernel = "MAIN";
    int handle;

    // Shader variable names
    const string sTexture = "_Tex";
    const string sConstant = "_constant";

    public FillConstant(Vector3 constant, string name="Set Constant")
    {
        this.constant = constant;
        this.name = name;
        this.computeShader = Resources.Load<ComputeShader>(kComputeShaderName);
        this.handle = computeShader.FindKernel(kKernel);
    }

    public void Transform(IGrid grid)
    {
        Bind(grid);
        computeShader.Dispatch(handle, 
            (int) Mathf.Ceil(grid.Resolution().x / 4.0f),
            (int) Mathf.Ceil(grid.Resolution().y / 4.0f),
            (int) Mathf.Ceil(grid.Resolution().z / 4.0f));
    }

    private void Bind(IGrid grid)
    {
        grid.Bind(computeShader, handle, sTexture);
        computeShader.SetVector(sConstant, new Vector4(constant.x, constant.y, constant.z, 0));
    }
}

} // namespace Turbulence