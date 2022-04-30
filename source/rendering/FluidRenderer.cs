using UnityEngine;
using Expanse;

namespace Turbulence
{

/**
 * @brief: main fluid simulation class.
 * */
[ExecuteInEditMode]
class FluidRenderer : MonoBehaviour
{
    public ProceduralCloudVolume cloudVolume;
    public FluidSimulation simulation;
    public bool clampModelingParameters;

    public void Update()
    {
        if (cloudVolume == null)
            return;

        // Only render the base noise
        if (clampModelingParameters)
        {
            cloudVolume.m_coverageIntensity = 1;
            cloudVolume.m_structureIntensity = 0;
            cloudVolume.m_structureMultiply = 0;
            cloudVolume.m_detailIntensity = 0;
            cloudVolume.m_detailMultiply = 0;
            cloudVolume.m_baseWarpIntensity = 0;
            cloudVolume.m_detailWarpIntensity = 0;
        }

        // Update transform
        cloudVolume.m_curved = false;
        cloudVolume.gameObject.transform.position = simulation.gameObject.transform.position;
        cloudVolume.transform.localScale = simulation.gameObject.transform.localScale;

        // Set the texture to be our density field
        cloudVolume.SetTexture(CloudDatatypes.CloudNoiseLayer.Base, simulation.DensityGrid().GetTexture(), 1);
    }
}

} // namespace Turbulence