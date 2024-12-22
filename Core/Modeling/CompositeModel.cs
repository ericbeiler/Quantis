using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class CompositeModel
    {
        public TrainingParameters? TrainingParameters { get; }
        public IPredictor[] Predictors { get; }

        internal CompositeModel(TrainingParameters? trainingParameters, IPredictor[] predictors)
        {
            TrainingParameters = trainingParameters;
            Predictors = predictors;
        }
    }
}
