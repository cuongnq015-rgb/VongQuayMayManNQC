using System.Collections.Generic;

namespace VongQuayMayManNQC.Models
{
    public class AppState
    {
        public List<Participant> Participants { get; set; }
        public List<WinnerRecord> Winners { get; set; }
        public List<PrizeConfig> PrizeConfigs { get; set; }
        public bool RemoveWinnerAfterWin { get; set; }

        public AppState()
        {
            Participants = new List<Participant>();
            Winners = new List<WinnerRecord>();
            PrizeConfigs = new List<PrizeConfig>();
            RemoveWinnerAfterWin = true;
        }
    }
}