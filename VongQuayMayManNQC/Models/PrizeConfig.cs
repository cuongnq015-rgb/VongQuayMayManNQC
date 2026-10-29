using System;

namespace VongQuayMayManNQC.Models
{
    public class PrizeConfig
    {
        public string PrizeName { get; set; }
        public int Quantity { get; set; }
        public int AwardedCount { get; set; }

        public int Remaining
        {
            get { return Math.Max(0, Quantity - AwardedCount); }
        }

        public PrizeConfig()
        {
            PrizeName = string.Empty;
            Quantity = 0;
            AwardedCount = 0;
        }

        public override string ToString()
        {
            return PrizeName ?? string.Empty;
        }
    }
}