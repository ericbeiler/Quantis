using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class PriceTrendPredictor
    {
        public TrainingParameters? TrainingParameters { get; }
        public IPredictor[] Predictors { get; }

        internal PriceTrendPredictor(TrainingParameters? trainingParameters, IPredictor[] predictors)
        {
            TrainingParameters = trainingParameters;
            Predictors = predictors;
        }
    }
}
