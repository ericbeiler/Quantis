﻿using System;

namespace Visavi.Quantis.Modeling
{
    public class TrainModelMessage
    {
        public string Message = "Train Model";
        public int TargetDuration { get; set; }
        public string? Index { get; set; }
    }
}