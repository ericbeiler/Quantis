using Microsoft.ML.Trainers.FastTree;

namespace Visavi.Quantis.Modeling
{
    internal class SupportedTrainers
    {
        // This is a list of trainers that are currently supported by Quantis 
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static FastTreeRegressionTrainer FastTreeRegressionTrainer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}
