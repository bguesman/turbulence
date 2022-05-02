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
    DenseGrid velocity, velocityTemp, density, densityTemp;

    // Transformations
    Fill clearDensity, clearVelocity;
    Add addDensity, addVelocity;
    Advect advectDensity, advectVelocity;
    Diffuse diffuseVelocity;

    // Grid resolution
    Vector3Int kResolution = new Vector3Int(128, 128, 128);

    public void OnEnable ()
    {
        // Constants
        kResolution = new Vector3Int(128, 128, 128);

        // Define grids
        this.velocity = new DenseGrid(kResolution, GridDatatype.eVector3, "Velocity");
        this.velocityTemp = new DenseGrid(kResolution, GridDatatype.eVector3, "Velocity Temp");
        this.density = new DenseGrid(kResolution, GridDatatype.eScalar, "Density");
        this.densityTemp = new DenseGrid(kResolution, GridDatatype.eScalar, "Density Temp");

        // Define transformations
        clearDensity = new Fill(0.0f, name: "Clear Density");
        clearVelocity = new Fill(Vector3.zero, name: "Clear Velocity");
        addDensity = new Add(50, 
            bounds: new Bounds(0.4f, 0.6f, 0.2f, 0.3f, 0.4f, 0.6f), 
            name: "Add Density");
        addVelocity = new Add(new Vector3(0, 0.01f, 0.05f), 
            bounds: new Bounds(0.4f, 0.6f, 0.2f, 0.3f, 0.4f, 0.6f), 
            name: "Add Velocity");
        advectDensity = new Advect("Advect Density");
        advectVelocity = new Advect("Advect Velocity");
        diffuseVelocity = new Diffuse(0.05f, "Diffuse Velocity");

        // Clear grids on start
        clearDensity.Transform(density);
        clearDensity.Transform(densityTemp);
        clearVelocity.Transform(velocity);
        clearVelocity.Transform(velocityTemp);
    }

    /**
     * @brief: steps fluid simulation forward by specified timestep.
     * */
    public void Update()
    {
        float dt = Time.deltaTime;

        // Swap grids
        DenseGrid densitySwap = densityTemp;
        densityTemp = density;
        density = densitySwap;

        // DenseGrid velocitySwap = velocityTemp;
        // velocityTemp = velocity;
        // velocity = velocitySwap;

        // Add forces
        addVelocity.constant.z = 0.05f + 0.2f * Mathf.Sin(3 * Time.time);
        addVelocity.Transform(velocity, dt);

        // Advect velocity by itself
        advectVelocity.Transform(velocity, velocityTemp, velocity, dt);

        // Diffuse according to viscosity
        diffuseVelocity.Transform(velocityTemp, velocity, dt);

        // Update density
        addDensity.Transform(densityTemp, dt);
        advectDensity.Transform(densityTemp, density, velocity, dt);
    }

    public DenseGrid DensityGrid()
    {
        return this.density;
    }
}

} // namespace Turbulence