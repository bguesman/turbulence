using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Turbulence
{

/**
 * @brief: a dense grid is a grid that is not at all sparse. It is
 * essentially a 3D texture.
 * */
class DenseGrid : IGrid
{
    // Private members.
    Vector3Int resolution;
    GridDatatype datatype;
    string name;
    RTHandle texture;

    public DenseGrid(Vector3Int resolution, GridDatatype datatype, string name="Dense Grid")
    {
        this.resolution = resolution;
        this.datatype = datatype;
        this.name = name;

        switch (datatype)
        {
            case GridDatatype.eScalar:
            {
                this.texture = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                    dimension: TextureDimension.Tex3D,
                    colorFormat: GraphicsFormat.R16_SFloat,
                    enableRandomWrite: true,
                    useMipMap: false,
                    autoGenerateMips: false,
                    name: this.name);
                break;
            }
            case GridDatatype.eVector2:
            {
                this.texture = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                    dimension: TextureDimension.Tex3D,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    enableRandomWrite: true,
                    useMipMap: false,
                    autoGenerateMips: false,
                    name: this.name);
                break;
            }
            case GridDatatype.eVector3:
            {
                this.texture = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                    dimension: TextureDimension.Tex3D,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    enableRandomWrite: true,
                    useMipMap: false,
                    autoGenerateMips: false,
                    name: this.name);
                break;
            }
            default:
                this.texture = null;
                break;
        }
    }

    ~DenseGrid()
    {
        if (texture != null)
            RTHandles.Release(texture);
        texture = null;
    }

    public Vector3Int Resolution()
    {
        return this.resolution;
    }

    public GridDatatype Datatype() 
    {
        return this.datatype;
    }

    public void Bind(ComputeShader computeShader, int kernel, string shaderVariable)
    {
        computeShader.SetTexture(kernel, shaderVariable, this.texture);
    }

    public void Bind(CommandBuffer commandBuffer, string shaderVariable)
    {
        commandBuffer.SetGlobalTexture(shaderVariable, this.texture);
    }

    /**
     * @return: underlying texture for rendering purposes.
     * */
    public RTHandle GetTexture()
    {
        return texture;
    }
}

} // namespace Turbulence