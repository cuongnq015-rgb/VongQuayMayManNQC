using System;

namespace VongQuayMayManNQC.Models
{
    public class WinnerRecord
    {
        public int STT { get; set; }
        public string Name { get; set; }
        public string PrizeName { get; set; }
        public DateTime WinTime { get; set; }

        public WinnerRecord()
        {
            STT = 0;
            Name = string.Empty;
            PrizeName = string.Empty;
            WinTime = DateTime.Now;
        }
    }
}