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

    void Update()
    {
        // Pass to material
        if (simulation == null || material == null)
            return;
        
        material.SetTexture("_Density", simulation.DensityGrid().GetTexture());
    }
}

} // namespace Turbulence