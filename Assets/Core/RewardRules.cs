namespace ReactionDomino.Core
{
    /// <summary>Premio de una partida para un jugador.</summary>
    public readonly struct Reward
    {
        public readonly int coins;
        public readonly int gems;

        public Reward(int coins, int gems)
        {
            this.coins = coins;
            this.gems = gems;
        }
    }

    /// <summary>
    /// Reglas de premio. Puro y probado. Los valores de victoria y derrota son los mismos que ya
    /// usaba <c>ValidateMatchResult.js</c> (100/50 y 25/10); el empate (50/25) es nuevo y puedes
    /// cambiarlo, pero cambialo aqui y en las pruebas.
    ///
    /// Quien llama a esto es SOLO el servidor, una vez, cuando una jugada termina la partida.
    /// El cliente nunca decide su premio.
    /// </summary>
    public static class RewardRules
    {
        public const int WinCoins = 100;
        public const int WinGems = 50;
        public const int DrawCoins = 50;
        public const int DrawGems = 25;
        public const int LoseCoins = 25;
        public const int LoseGems = 10;

        /// <param name="seat">Asiento del jugador (1 o 2).</param>
        /// <param name="winner">Ganador de la partida: 1, 2 o 0 si fue empate.</param>
        public static Reward For(int seat, int winner)
        {
            if (winner == 0) return new Reward(DrawCoins, DrawGems);
            if (winner == seat) return new Reward(WinCoins, WinGems);
            return new Reward(LoseCoins, LoseGems);
        }
    }
}
