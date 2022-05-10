using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Turbulence
{

[ExecuteInEditMode]
class VanillaFluidRenderer : MonoBehaviour
{
    public FluidSimulation simulation;
    public Material material;
    private Texture2D[] blueNoiseTextures;


    void Update()
    {
        if (blueNoiseTextures == null || blueNoiseTextures.Length == 0)
            LoadBlueNoise();

        // Pass to material
        if (simulation == null || material == null)
            return;
        
        material.SetTexture("_DensityTexture", simulation.DensityGrid().GetTexture());
        material.SetTexture("_Blue_Noise", blueNoiseTextures[Time.frameCount % blueNoiseTextures.Length]);
    }

    void LoadBlueNoise()
    {
        blueNoiseTextures = new Texture2D[64];
        for (int i = 0; i < blueNoiseTextures.Length; i++)
            blueNoiseTextures[i] = Resources.Load<Texture2D>("Spatio-temporal Blue Noise/stbn_scalar_2Dx1Dx1D_128x128x64x1_" + i);
    }
}

} // namespace Turbulence