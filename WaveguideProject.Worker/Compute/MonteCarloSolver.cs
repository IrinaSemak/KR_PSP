using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WaveguideProject.Models;

namespace WaveguideProject.Worker.Compute
{
    public class MonteCarloSolver
    {
        public static CalculationResult CalculateWaveguide(WaveguideParameters p, string nodeId)
        {
            var stopwatch = Stopwatch.StartNew();
            int nx = p.GridResolution;
            int ny = (int)(p.GridResolution * (p.Height / p.Width));

            double[][] field = new double[nx][];
            for (int i = 0; i < nx; i++) field[i] = new double[ny];

            Parallel.For(1, nx - 1, i =>
            {
                Random localRand = new Random(Guid.NewGuid().GetHashCode());
                for (int j = 1; j < ny - 1; j++)
                {
                    double sumBoundaryValues = 0;
                    for (int w = 0; w < p.WalkersCount; w++)
                    {
                        int x = i;
                        int y = j;
                        while (x > 0 && x < nx - 1 && y > 0 && y < ny - 1)
                        {
                            int step = localRand.Next(4);
                            if (step == 0) x++; else if (step == 1) x--; else if (step == 2) y++; else y--;
                        }
                        if (y == ny - 1) sumBoundaryValues += 1.0;
                    }
                    field[i][j] = sumBoundaryValues / p.WalkersCount;
                }
            });

            stopwatch.Stop();
            return new CalculationResult(p.TaskId, nodeId, field, stopwatch.Elapsed);
        }
    }
}