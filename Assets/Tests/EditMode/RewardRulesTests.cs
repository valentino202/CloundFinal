using NUnit.Framework;
using ReactionDomino.Core;

namespace ReactionDomino.Core.Tests
{
    /// <summary>Pruebas de los premios. Solo el servidor los calcula, una vez, al terminar la partida.</summary>
    public class RewardRulesTests
    {
        [TestCase(1)]
        [TestCase(2)]
        public void ElGanadorRecibeElPremioDeVictoria(int seat)
        {
            var r = RewardRules.For(seat, winner: seat);
            Assert.AreEqual(RewardRules.WinCoins, r.coins);
            Assert.AreEqual(RewardRules.WinGems, r.gems);
        }

        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void ElPerdedorRecibeElPremioDeDerrota(int seat, int winner)
        {
            var r = RewardRules.For(seat, winner);
            Assert.AreEqual(RewardRules.LoseCoins, r.coins);
            Assert.AreEqual(RewardRules.LoseGems, r.gems);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ElEmpateDaElMismoPremioALosDos(int seat)
        {
            var r = RewardRules.For(seat, winner: 0);
            Assert.AreEqual(RewardRules.DrawCoins, r.coins);
            Assert.AreEqual(RewardRules.DrawGems, r.gems);
        }

        [Test]
        public void GanarPagaMasQueEmpatarYEmpatarMasQuePerder()
        {
            Assert.Greater(RewardRules.WinCoins, RewardRules.DrawCoins);
            Assert.Greater(RewardRules.DrawCoins, RewardRules.LoseCoins);
            Assert.Greater(RewardRules.WinGems, RewardRules.DrawGems);
            Assert.Greater(RewardRules.DrawGems, RewardRules.LoseGems);
        }
    }
}
