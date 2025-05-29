namespace GestioneClienti.ViewModel
{
    public class ProfiloViewModel
    {
        
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime DataRegistrazione { get; set; }
        public int Livello { get; set; }
        public bool IsPremium { get; set; }
        public int EsperienzaPercentuale { get; set; }
        public int Punteggio { get; set; }
        public int GiochiPosseduti { get; set; }
        public int OreGiocate { get; set; }
        public int AchievementsSbloccati { get; set; }
        public int SfideCompletate { get; set; }
    }
}