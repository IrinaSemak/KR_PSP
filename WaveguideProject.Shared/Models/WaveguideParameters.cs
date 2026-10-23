using System;

namespace WaveguideProject.Models // Важно, чтобы было именно Models
{
    public record WaveguideParameters(
        string TaskId,
        double Width,
        double Height,
        int GridResolution,
        int WalkersCount
    );

    public record CalculationResult(
        string TaskId,
        string NodeId,
        double[][] Field,
        TimeSpan ComputeTime
    );
}