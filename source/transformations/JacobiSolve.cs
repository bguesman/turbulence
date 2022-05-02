using UnityEngine;

namespace Turbulence
{

/**
 * @brief: Solves a jaboci matrix iteration problem.
 * */
class JacobiSolve : ITransformation
{
    // Name of this transformation
    string name;
    public float alpha, beta;

    // Jacobi steps
    JacobiStep[] steps = null;

    public JacobiSolve(int steps, float alpha, float beta, string name="Jacobi Solve")
    {
        // Arguments
        this.name = name;
        this.alpha = alpha;
        this.beta = beta;

        // Steps. Always use an odd number of steps to ensure our
        // final result lands in the expected texture.
        steps = (steps % 2 == 0) ? steps + 1 : steps;
        this.steps = new JacobiStep[steps];
        for (int i = 0; i < this.steps.Length; i++)
            this.steps[i] = new JacobiStep(alpha, beta, "Jacobi Step " + i);
    }

    public void Transform(IGrid A, IGrid B, IGrid Output)
    {
        IGrid inA = A;
        IGrid target = Output;
        foreach (JacobiStep s in steps)
        {
            // Do the transformation
            s.Transform(inA, B, target);

            // Swap grids for the next iteration
            IGrid temp = target;
            target = inA;
            inA = temp;
        }
    }
}

} // namespace Turbulence