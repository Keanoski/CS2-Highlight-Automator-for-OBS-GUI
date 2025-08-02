namespace HighlightReel.Models
{
    public class Highlight
    {
        public string PlayerName { get; set; } = string.Empty;
        public int StartTick { get; set; }
        public int EndTick { get; set; }
        public int KillCount { get; set; }
        public int RoundNumber { get; set; }
        public int TotalDamage { get; set; }
        public string WeaponsUsed { get; set; } = string.Empty;
    }
}
