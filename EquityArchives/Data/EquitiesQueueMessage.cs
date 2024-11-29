using System;

namespace Visavi.Quantis.Data
{
    public class EquitiesQueueMessage
    {
        public string Message { get; set; }
        public uint? ReloadDays { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? SimFinId { get; set; }

        // Model Export Properties
        public string OutputPath { get; set; }
    }
}
