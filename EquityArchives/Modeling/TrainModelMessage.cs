using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Modeling
{

    internal class TrainModelMessage
    {
        public string Message = "Train Model";
        public int TargetDuration { get; set; }
        public string? Index { get; set; }
    }
}
