using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    internal class CompositeModelRecord
    {
        public int Id { get; set; }
        public string? Parameters { get; set; }
    }

    public static class CompositeModelExtensions
    {
        internal static string GetName(this CompositeModelRecord model)
        {
            return $"Composite Model {model.Id}";
        }

        internal static string GetDescription(this CompositeModelRecord model)
        {
            return model?.Parameters ?? string.Empty;
        }

        internal static ModelSummary ToModelSummary(this CompositeModelRecord model)
        {
            return new ModelSummary(model.Id, model.GetName(), model.GetDescription(), ModelType.Composite);
        }
    }

}
