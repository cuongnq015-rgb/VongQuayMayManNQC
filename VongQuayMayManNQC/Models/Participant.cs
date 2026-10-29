using System;

namespace VongQuayMayManNQC.Models
{
    public class Participant
    {
        public string Name { get; set; }
        public bool IsWinner { get; set; }

        public Participant()
        {
            Name = string.Empty;
            IsWinner = false;
        }

        public Participant(string name)
        {
            Name = name ?? string.Empty;
            IsWinner = false;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}