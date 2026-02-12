using System;
using System.Collections.Generic;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class SyncReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string SyncType { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public double SuccessRate => TotalItems > 0 ? (double)SuccessfulItems / TotalItems * 100 : 0;
    }
}
