using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: Solves a jaboci matrix iteration problem.
 * */
class PressureSolve : ITransformation
{
    // Name of this transformation
    string name;
    public int steps;

    // Jacobi solver
    JacobiSolveMultiGrid jacobi;
    Fill clearPressure;
    Divergence computeDivergence;
    Boundary pressureBoundary;
    
    // Profiling
    ProfilingSampler profilingSampler;

    public PressureSolve(int steps, string name="Pressure Solve")
    {
        // Arguments
        this.name = name;
        this.steps = steps;

        // Sub-transforms
        jacobi = new JacobiSolveMultiGrid(steps, -1.0f, 1.0f/6.0f);
        clearPressure = new Fill(0.0f, name: "Clear Pressure");
        computeDivergence = new Divergence(name: "Compute Velocity Divergence");
        pressureBoundary = new Boundary(Boundary.BoundaryCondition.eNeumann, "Pressure Boundary");

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid V, IGrid DivV, IGrid P, IGrid POut)
    {
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            // Compute divergence of V
            computeDivergence.Transform(context, V, DivV);

            // Initialize pressure grids
            clearPressure.Transform(context, P);
            clearPressure.Transform(context, POut); // TODO: probably unnecessary

            // Solve for pressure via jacobi iteration
            jacobi.Transform(context, DivV, P, POut);

            // Impose neumman boundary condition
            pressureBoundary.Transform(context, POut);
        }
    }
}

} // namespace Turbulence