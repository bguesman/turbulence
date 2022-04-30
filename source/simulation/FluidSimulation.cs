using UnityEngine;

namespace Turbulence
{

/**
 * @brief: main fluid simulation class.
 * */
[ExecuteInEditMode]
class FluidSimulation : MonoBehaviour
{
    // Simulation grids
    DenseGrid velocity, density;

    // Transformations
    FillConstant fillDensity;

    // Grid resolution
    Vector3Int kResolution = new Vector3Int(256, 256, 256);

    public void OnEnable ()
    {
        // Grids
        this.velocity = new DenseGrid(kResolution, GridDatatype.eVector3, "Velocity");
        this.density = new DenseGrid(kResolution, GridDatatype.eScalar, "Density");

        // Transformations
        fillDensity = new FillConstant(new Vector3(10.0f, 10.0f, 10.0f), "Fill Density");

        // Constants
        kResolution = new Vector3Int(256, 256, 256);
    }

    /**
     * @brief: steps fluid simulation forward by specified timestep.
     * */
    public void Update()
    {
        float dt = Time.deltaTime;

        // Run transforms
        fillDensity.Transform(density);
    }

    public DenseGrid DensityGrid()
    {
        return this.density;
    }
}

} // namespace Turbulence