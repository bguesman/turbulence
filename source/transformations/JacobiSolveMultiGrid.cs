using UnityEngine;
using UnityEngine.Rendering;

namespace Turbulence
{

/**
 * @brief: Solves a jaboci matrix iteration problem.
 * */
class JacobiSolveMultiGrid : ITransformation
{
    // Name of this transformation
    string name;
    public float alpha, beta;
    public int tiers;

    // Jacobi steps
    JacobiStep[][] steps = null;
    Resample[] resampleA;
    Resample[] resampleB;

    // Grids used for resolution handling.
    IGrid[] downsampledA;
    IGrid[] downsampledB;

    // Profiling
    ProfilingSampler profilingSampler;

    public JacobiSolveMultiGrid(int steps, float alpha, float beta, int tiers=4, string name="Jacobi Solve")
    {
        // Arguments
        this.name = name;
        this.alpha = alpha;
        this.beta = beta;
        this.tiers = tiers;

        // Steps. Always use an odd number of steps to ensure our
        // final result lands in the expected texture.
        steps = (steps % 2 == 0) ? steps + 1 : steps;
        this.steps = new JacobiStep[tiers][];
        for (int i = 0; i < tiers; i++)
        {
            this.steps[i] = new JacobiStep[steps];
            for (int j = 0; j < this.steps[i].Length; j++)
            {
                this.steps[i][j] = new JacobiStep(alpha, beta, "Jacobi Step Tier " + i + " Step " + j);
            }
        }

        resampleA = new Resample[tiers];
        resampleB = new Resample[tiers];
        for (int i = 0; i < tiers; i++)
        {
            resampleA[i] = new Resample("Multigrid Resample A " + i);
            resampleB[i] = new Resample("Multigrid Resample B " + i);
        }

        // Profiling
        this.profilingSampler = new ProfilingSampler(name);
    }

    public void Transform(TransformationContext context, IGrid A, IGrid B, IGrid Output)
    {
        if (downsampledA == null || downsampledB == null)
            allocateGrids(A, B);

        using (new ProfilingScope(context.cmd, profilingSampler))
        {
            // Downsample target grids.
            for (int i = downsampledA.Length - 1; i >= 0; i--)
            {
                IGrid src = (i == (downsampledA.Length - 1)) ? A : downsampledA[i + 1];
                resampleA[i].Transform(context, src, downsampledA[i]);
            }

            // For each tier
            int stepsToRun = this.steps[0].Length;
            for (int i = 0; i < this.tiers; i++)
            {
                // Debug.Log("Running " + stepsToRun + " steps for tier " + i);

                IGrid targetA, B1, B2;
                bool finalTier = (i == this.tiers - 1);
                if (finalTier)
                {
                    // Low res tier
                    targetA = A;
                    B1 = B;
                    B2 = Output;
                }
                else 
                {
                    // Full res tier
                    targetA = downsampledA[i];
                    B1 = downsampledB[2 * i];
                    B2 = downsampledB[2 * i + 1];
                }

                for (int j = 0; j < stepsToRun; j++)
                {
                    // Do the transformation
                    this.steps[i][j].Transform(context, targetA, B1, B2);

                    // Swap grids for the next iteration
                    IGrid temp = B2;
                    B2 = B1;
                    B1 = temp;
                }

                if (!finalTier)
                {
                    // Upsample B
                    IGrid upsampleTarget = (i == this.tiers - 2) ? B : downsampledB[2 * (i + 1)];
                    resampleB[i].Transform(context, B2, upsampleTarget);
                }

                // Cut steps to run in half, making sure we always run at least 2
                stepsToRun = stepsToRun / 2;
                stepsToRun = stepsToRun % 2 == 0 ? stepsToRun - 1 : stepsToRun;
                stepsToRun = stepsToRun < 1 ? 1 : stepsToRun;
            }
        }
    }

    private void allocateGrids(IGrid A, IGrid B)
    {
        downsampledA = new IGrid[(this.tiers - 1)];
        downsampledB = new IGrid[(this.tiers - 1) * 2];
        for (int i = 0; i < this.tiers - 1; i++)
        {
            int downscale = (int) (Mathf.Pow(2, this.tiers - 1 - i));
            downsampledB[2 * i] = new DenseGrid(B.Resolution() / downscale, B.Datatype(), "Multigrid B Step " + i);
            downsampledB[2 * i + 1] = new DenseGrid(B.Resolution() / downscale, B.Datatype(), "Multigrid B Step " + i);
            downsampledA[i] = new DenseGrid(A.Resolution() / downscale, A.Datatype(), "Multigrid A Step " + i);
        }
    }
}

} // namespace Turbulence