using UnityEngine;

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
    JacobiSolve jacobi;
    Fill clearPressure;
    Divergence computeDivergence;
    Boundary pressureBoundary;

    public PressureSolve(int steps, string name="Pressure Solve")
    {
        // Arguments
        this.name = name;
        this.steps = steps;

        // Sub-transforms
        jacobi = new JacobiSolve(steps, -1.0f, 1.0f/6.0f);
        clearPressure = new Fill(0.0f, name: "Clear Pressure");
        computeDivergence = new Divergence(name: "Compute Velocity Divergence");
        pressureBoundary = new Boundary(Boundary.BoundaryCondition.eNeumann, "Pressure Boundary");
    }

    public void Transform(IGrid V, IGrid DivV, IGrid P, IGrid POut)
    {
        // Compute divergence of V
        computeDivergence.Transform(V, DivV);

        // Initialize pressure grids
        clearPressure.Transform(P);
        clearPressure.Transform(POut); // TODO: probably unnecessary

        // Solve for pressure via jacobi iteration
        jacobi.Transform(DivV, P, POut);

        // Impose neumman boundary condition
        pressureBoundary.Transform(POut);
    }
}

} // namespace Turbulence