using NUnit.Framework;
using ReactionDomino.CloudCode;
using ReactionDomino.Core;

namespace ReactionDomino.CloudCode.Tests
{
    /// <summary>
    /// Pruebas de la VISTA que recibe el cliente (MatchStateStore.ToDto). No necesitan Unity ni
    /// internet: se ejecutan con `dotnet test` dentro de CloudCode~/ReactionDominoModule.Tests.
    /// </summary>
    public class MatchStateDtoTests
    {
        private static MatchRecord NewRecord(int players)
        {
            var r = new MatchRecord
            {
                MatchId = "rd-TEST",
                GridSize = 6,
                MaxCellLevel = 4,
                MaxTurns = 30,
            };
            r.Players.Add(new MatchPlayerRecord { PlayerId = "alice", Seat = 1 });
            if (players > 1) r.Players.Add(new MatchPlayerRecord { PlayerId = "bob", Seat = 2 });
            return r;
        }

        // Partida de 7 jugadas que termina con una explosion en cadena y gana el asiento 1.
        private static void AddChainMoves(MatchRecord r)
        {
            for (int i = 0; i < 4; i++)
            {
                r.Moves.Add(new MoveRecord { X = 2, Y = 2 });
                if (i < 3) r.Moves.Add(new MoveRecord { X = 2, Y = 3 });
            }
        }

        [Test]
        public void ConUnSoloJugadorLaFaseEsWaiting()
        {
            var r = NewRecord(players: 1);
            var dto = MatchStateStore.ToDto(r, MatchStateStore.Replay(r), "alice");
            Assert.AreEqual("waiting", dto.Phase);
            Assert.AreEqual(0, dto.CurrentSeat);
            Assert.AreEqual(1, dto.MySeat);
        }

        [Test]
        public void ConDosJugadoresYSinJugadasLeTocaAlAsiento1()
        {
            var r = NewRecord(players: 2);
            var dto = MatchStateStore.ToDto(r, MatchStateStore.Replay(r), "bob");
            Assert.AreEqual("playing", dto.Phase);
            Assert.AreEqual(1, dto.CurrentSeat);
            Assert.AreEqual(2, dto.MySeat, "cada jugador ve su propio asiento");
            Assert.IsNull(dto.LastMove);
        }

        [Test]
        public void ElTableroSeReconstruyeDesdeLasJugadas()
        {
            var r = NewRecord(players: 2);
            r.Moves.Add(new MoveRecord { X = 1, Y = 4 }); // asiento 1
            r.Moves.Add(new MoveRecord { X = 0, Y = 0 }); // asiento 2
            var dto = MatchStateStore.ToDto(r, MatchStateStore.Replay(r), "alice");

            Assert.AreEqual(36, dto.Owner.Length);
            Assert.AreEqual(1, dto.Owner[4 * 6 + 1], "indice = y * GridSize + x");
            Assert.AreEqual(2, dto.Owner[0]);
            Assert.AreEqual(1, dto.Level[4 * 6 + 1]);
            Assert.AreEqual(2, dto.TotalMoves);
            Assert.AreEqual(1, dto.CurrentSeat);
        }

        [Test]
        public void UnaPartidaTerminadaMuestraGanadorYLasExplosionesDeLaUltimaJugada()
        {
            var r = NewRecord(players: 2);
            AddChainMoves(r);
            var dto = MatchStateStore.ToDto(r, MatchStateStore.Replay(r), "alice");

            Assert.AreEqual("finished", dto.Phase);
            Assert.AreEqual(1, dto.WinnerSeat);
            Assert.AreEqual(0, dto.CurrentSeat);
            Assert.AreEqual(7, dto.TotalMoves);
            Assert.AreEqual(2, dto.LastMoveExplosions.Length, "cadena de dos explosiones");
            Assert.AreEqual(new[] { 2, 2 }, dto.LastMoveExplosions[0]);
            Assert.AreEqual(new[] { 2, 3 }, dto.LastMoveExplosions[1]);
        }

        [Test]
        public void ElPremioQueSeVeEsElDeQuienPregunta()
        {
            var r = NewRecord(players: 2);
            AddChainMoves(r);
            r.Rewards["alice"] = new RewardGrantRecord { Coins = RewardRules.WinCoins, Gems = RewardRules.WinGems };
            r.Rewards["bob"] = new RewardGrantRecord { Coins = RewardRules.LoseCoins, Gems = RewardRules.LoseGems };

            var game = MatchStateStore.Replay(r);
            var forAlice = MatchStateStore.ToDto(r, game, "alice");
            var forBob = MatchStateStore.ToDto(r, game, "bob");

            Assert.AreEqual(RewardRules.WinCoins, forAlice.AwardedCoins);
            Assert.AreEqual(RewardRules.LoseCoins, forBob.AwardedCoins);
        }

        [Test]
        public void UnObservadorQueNoJuegaNoTieneAsientoNiPremio()
        {
            var r = NewRecord(players: 2);
            AddChainMoves(r);
            r.Rewards["alice"] = new RewardGrantRecord { Coins = 100, Gems = 50 };
            var dto = MatchStateStore.ToDto(r, MatchStateStore.Replay(r), "mallory");
            Assert.AreEqual(0, dto.MySeat);
            Assert.AreEqual(0, dto.AwardedCoins);
        }

        [Test]
        public void UnRegistroCorruptoSeDetectaAlReproducir()
        {
            var r = NewRecord(players: 2);
            r.Moves.Add(new MoveRecord { X = 0, Y = 0 });
            r.Moves.Add(new MoveRecord { X = 0, Y = 0 }); // celda del rival: imposible en una partida real
            Assert.Throws<InvalidOperationException>(() => MatchStateStore.Replay(r));
        }
    }
}
