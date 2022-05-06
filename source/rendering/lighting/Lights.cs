using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Turbulence
{

// GPU-mirrorable directional light struct
[GenerateHLSL(needAccessors = false)]
struct DirectionalLightMirror 
{
    Vector3 forward;
    Vector4 color;
    public void FromLight(Light light)
    {
        this.forward = light.gameObject.transform.forward;
        this.color = light.color;
    }
}


[ExecuteInEditMode]
class Lights : MonoBehaviour
{
    // For now manually set lights in an array
    public Light[] lights;

    // Compute buffer for directional lights
    int[] directionalLightIndices;
    DirectionalLightMirror[] directionalLightArray;
    ComputeBuffer directionalLightBuffer;

    void Start()
    {
        directionalLightBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightMirror)));
        directionalLightIndices = new int[1];
    }

    void Update()
    {
        // Count light types
        int numDirectionalLights = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == UnityEngine.LightType.Directional)
            {
                directionalLightIndices[numDirectionalLights] = i;
                numDirectionalLights++;
            }
        }

        // Resize arrays optional
        int desiredSize = numDirectionalLights == 0 ? 1 : numDirectionalLights;
        if (directionalLightArray == null || directionalLightArray.Length != desiredSize)
        {
            directionalLightArray = new DirectionalLightMirror[desiredSize];   
        }

        if (directionalLightBuffer == null || directionalLightBuffer.count != desiredSize)
        {
            if (directionalLightBuffer != null)
                directionalLightBuffer.Release();
            directionalLightBuffer = new ComputeBuffer(desiredSize, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightMirror)));
        }

        // Fill array
        for (int i = 0; i < numDirectionalLights; i++)
        {
            directionalLightArray[i].FromLight(lights[directionalLightIndices[i]]);
        }

        // Set global compute buffer
        directionalLightBuffer.SetData(directionalLightArray);
        Shader.SetGlobalBuffer("_TurbulenceDirectionalLights", directionalLightBuffer);
        Shader.SetGlobalInteger("_TurbulenceNumDirectionalLights", directionalLightArray.Length);
    }
}

} // namespace Turbulence