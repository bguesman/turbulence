using UnityEngine;
using UnityEngine.Rendering;

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

    // Profiling
    ProfilingSampler profilingSampler;

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

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid A, IGrid B, IGrid Output)
    {
        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            IGrid inB = B;
            IGrid target = Output;
            foreach (JacobiStep s in steps)
            {
                // Do the transformation
                s.Transform(context, A, inB, target);

                // Swap grids for the next iteration
                IGrid temp = target;
                target = inB;
                inB = temp;
            }
        }
    }
}

} // namespace Turbulence