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
    DenseGrid velocity, velocityTemp, density, densityTemp, pressure, pressureTemp, divergenceV, curlV;

    // Transformations
    Fill clearDensity, clearVelocity, clearDensityTemp, clearVelocityTemp;
    Add addDensity, addVelocity, addRandomVelocityPerturbations;
    Advect advectDensity, advectVelocity;
    Boundary velocityBoundary;
    Diffuse diffuseVelocity;
    PressureSolve pressureSolve;
    Project velocityProject;
    VorticityConfinement vorticity;

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
        this.pressure = new DenseGrid(kResolution, GridDatatype.eScalar, "Pressure");
        this.pressureTemp = new DenseGrid(kResolution, GridDatatype.eScalar, "Pressure Temp");
        this.divergenceV = new DenseGrid(kResolution, GridDatatype.eScalar, "Divergence V");
        this.curlV = new DenseGrid(kResolution, GridDatatype.eVector3, "Curl V");

        // Define transformations
        clearDensity = new Fill(0.0f, name: "Clear Density");
        clearDensityTemp = new Fill(0.0f, name: "Clear Density Temp");
        clearVelocity = new Fill(Vector3.zero, name: "Clear Velocity");
        clearVelocityTemp = new Fill(Vector3.zero, name: "Clear Velocity Temp");
        
        addDensity = new Add(1, 
            bounds: new Bounds(0.3f, 0.7f, 0.1f, 0.2f, 0.3f, 0.7f), 
            name: "Add Density");
        addVelocity = new Add(new Vector3(0, 2.0f, 0), 
            bounds: new Bounds(0.4f, 0.6f, 0, 1, 0.4f, 0.6f), 
            name: "Add Velocity");
        addRandomVelocityPerturbations = new Add(new Vector3(0, 0, 0), 
            bounds: new Bounds(0.4f, 0.6f, 0.4f, 0.6f, 0.4f, 0.6f), 
            name: "Add Velocity Perturbations");

        advectDensity = new Advect("Advect Density");
        advectVelocity = new Advect("Advect Velocity");
        velocityBoundary = new Boundary(Boundary.BoundaryCondition.eFreeSlip, "Velocity Boundary");
        diffuseVelocity = new Diffuse(0.005f, "Diffuse Velocity");
        pressureSolve = new PressureSolve(40, "Pressure Solve");
        velocityProject = new Project("Velocity Project");
        vorticity = new VorticityConfinement(50.0f, "Vorticity Confinement");

        // Clear grids on start
        clearDensity.Transform(density);
        clearDensityTemp.Transform(densityTemp);
        clearVelocity.Transform(velocity);
        clearVelocityTemp.Transform(velocityTemp);
    }

    /**
     * @brief: steps fluid simulation forward by specified timestep.
     * */
    public void Update()
    {
        float dt = Time.deltaTime;
        SwapGrids();
        AddForces(dt);
        SolveVelocity(dt);
        UpdateDensity(dt);
    }

    // Sub-update steps
    void SwapGrids()
    {
        // Swap grids
        DenseGrid densitySwap = densityTemp;
        densityTemp = density;
        density = densitySwap;

        // DenseGrid velocitySwap = velocityTemp;
        // velocityTemp = velocity;
        // velocity = velocitySwap;
    }

    void AddForces(float dt)
    {
        addVelocity.Transform(velocity, dt);

        if (Time.frameCount % 60 < 5)
            addRandomVelocityPerturbations.constant.x = 2 * Mathf.Sin(Time.time);
        else
            addVelocity.constant.x = 0;
        if (Time.frameCount % 93 < 5)
            addRandomVelocityPerturbations.constant.z = 2 * Mathf.Sin(Time.time * 1.31f);
        else
            addVelocity.constant.z = 0;
        if (Time.frameCount % 51 < 4)
            addRandomVelocityPerturbations.constant.y = 2 * Mathf.Sin(Time.time * 2.17f);
        else
            addVelocity.constant.y = 0;
        addRandomVelocityPerturbations.Transform(velocity, dt);
    }

    void SolveVelocity(float dt)
    {
        advectVelocity.Transform(velocity, velocityTemp, velocity, dt);
        diffuseVelocity.Transform(velocityTemp, velocity, dt);
        pressureSolve.Transform(velocity, divergenceV, pressureTemp, pressure);
        velocityProject.Transform(pressure, velocity);
        velocityBoundary.Transform(velocity);
        vorticity.Transform(velocity, curlV, dt);
    }

    void UpdateDensity(float dt)
    {
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