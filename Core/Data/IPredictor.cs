using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Data
{
    public interface IPredictor
    {
        int? Id { get; }
        BinaryData InferencingModel { get; }
        public int TargetDuration { get; set; }
        public double RootMeanSquaredError { get; set; }
    }
}
