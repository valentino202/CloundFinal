using System.Collections.Generic;
using NUnit.Framework;
using ReactionDomino.Core;

namespace ReactionDomino.Core.Tests
{
    /// <summary>
    /// Pruebas de las reglas del juego (Core). Se ejecutan en Window > General > Test Runner > EditMode.
    /// Cada prueba explica en su nombre QUE regla protege; si falla, esa regla se rompio.
    /// Las mismas reglas las ejecuta el servidor, asi que un fallo aqui es un fallo en las dos puntas.
    /// </summary>
    public class GameLogicTests
    {
        private static GameLogic NewGame(int maxTurns = 30) => new GameLogic(gridSize: 6, maxLevel: 4, maxTurns: maxTurns);

        [Test]
        public void ElConstructorAplicaLosMinimos()
        {
            var g = new GameLogic(gridSize: 1, maxLevel: 1, maxTurns: 30);
            Assert.AreEqual(3, g.gridSize);
            Assert.AreEqual(2, g.maxLevel);
        }

        [Test]
        public void EmpiezaElJugador1()
        {
            var g = NewGame();
            Assert.AreEqual(1, g.currentPlayer);
            Assert.AreEqual(MoveOutcome.NotYourTurn, g.PlacePoint(0, 0, 2).outcome);
        }

        [Test]
        public void UnaJugadaFueraDeTurnoNoCambiaElEstado()
        {
            var g = NewGame();
            var r = g.PlacePoint(0, 0, 2);
            Assert.IsFalse(r.success);
            Assert.AreEqual(0, g.totalMoves);
            Assert.AreEqual(0, g.owner[0, 0]);
            Assert.AreEqual(1, g.currentPlayer);
        }

        [Test]
        public void ElTurnoPasaAlRivalTrasUnaJugadaValida()
        {
            var g = NewGame();
            var r = g.PlacePoint(0, 0, 1);
            Assert.IsTrue(r.success);
            Assert.AreEqual(MoveOutcome.Ok, r.outcome);
            Assert.AreEqual(2, g.currentPlayer);
            Assert.AreEqual(1, g.owner[0, 0]);
            Assert.AreEqual(1, g.level[0, 0]);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(6, 0)]
        [TestCase(0, 6)]
        public void UnaCeldaFueraDelTableroSeRechazaSinExcepcion(int x, int y)
        {
            var g = NewGame();
            var r = g.PlacePoint(x, y, 1);
            Assert.AreEqual(MoveOutcome.OutOfRange, r.outcome);
            Assert.AreEqual(0, g.totalMoves);
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(-1)]
        public void UnJugadorInvalidoSeRechaza(int player)
        {
            var g = NewGame();
            Assert.AreEqual(MoveOutcome.InvalidPlayer, g.PlacePoint(0, 0, player).outcome);
        }

        [Test]
        public void NoSePuedeJugarEnUnaCeldaDelRival()
        {
            var g = NewGame();
            g.PlacePoint(0, 0, 1);
            var r = g.PlacePoint(0, 0, 2);
            Assert.AreEqual(MoveOutcome.CellOwnedByRival, r.outcome);
            // El rechazo no consume el turno: el jugador 2 sigue siendo quien debe jugar.
            Assert.AreEqual(2, g.currentPlayer);
            Assert.IsTrue(g.PlacePoint(5, 5, 2).success);
        }

        [Test]
        public void JugarEnUnaCeldaPropiaSubeSuNivel()
        {
            var g = NewGame();
            g.PlacePoint(0, 0, 1);
            g.PlacePoint(5, 5, 2);
            g.PlacePoint(0, 0, 1);
            Assert.AreEqual(2, g.level[0, 0]);
        }

        [Test]
        public void ValidateNoCambiaElEstado()
        {
            var g = NewGame();
            Assert.AreEqual(MoveOutcome.Ok, g.Validate(2, 2, 1));
            Assert.AreEqual(0, g.totalMoves);
            Assert.AreEqual(0, g.owner[2, 2]);
            Assert.AreEqual(1, g.currentPlayer);
        }

        [Test]
        public void UnaCeldaQueLlegaAlNivelMaximoExplotaYConquistaASusVecinas()
        {
            var g = NewGame();
            GameLogic.MoveResult last = default;
            // P1 sube (2,2) cuatro veces; P2 juega lejos y sin tocar nada.
            for (int i = 0; i < 4; i++)
            {
                last = g.PlacePoint(2, 2, 1);
                if (i < 3) g.PlacePoint(5, 5, 2);
            }

            Assert.AreEqual(1, last.explosions.Count);
            Assert.AreEqual(2, last.explosions[0].x);
            Assert.AreEqual(2, last.explosions[0].y);
            Assert.AreEqual(0, g.owner[2, 2], "la celda que explota queda neutral");
            Assert.AreEqual(0, g.level[2, 2]);
            foreach (var n in new[] { (1, 2), (3, 2), (2, 1), (2, 3) })
            {
                Assert.AreEqual(1, g.owner[n.Item1, n.Item2], "las vecinas pasan a ser del atacante");
                Assert.AreEqual(1, g.level[n.Item1, n.Item2]);
            }
            Assert.IsFalse(last.gameOver, "el rival todavia tiene celdas");
        }

        [Test]
        public void UnaExplosionPuedeProvocarOtraEnCadenaYEliminarAlRival()
        {
            var g = NewGame();
            // P2 sube (2,3) a nivel 3, pegada a (2,2). Cuando (2,2) explote, (2,3) llega a 4 y explota tambien.
            GameLogic.MoveResult last = default;
            for (int i = 0; i < 4; i++)
            {
                last = g.PlacePoint(2, 2, 1);
                if (i < 3) g.PlacePoint(2, 3, 2);
            }

            Assert.AreEqual(2, last.explosions.Count, "cadena: primero (2,2), luego (2,3)");
            Assert.AreEqual(2, last.explosions[0].y);
            Assert.AreEqual(3, last.explosions[1].y);
            Assert.AreEqual(0, g.CountCells(2), "el rival se quedo sin celdas");
            Assert.IsTrue(last.gameOver);
            Assert.AreEqual(1, last.winner);
            Assert.IsTrue(g.IsOver);
            Assert.AreEqual(1, g.FinalWinner);
        }

        [Test]
        public void TrasTerminarLaPartidaNoSeAceptanMasJugadas()
        {
            var g = NewGame();
            for (int i = 0; i < 4; i++)
            {
                g.PlacePoint(2, 2, 1);
                if (i < 3) g.PlacePoint(2, 3, 2);
            }
            Assert.IsTrue(g.IsOver);
            int movesBefore = g.totalMoves;

            var r = g.PlacePoint(0, 0, g.currentPlayer);
            Assert.AreEqual(MoveOutcome.MatchFinished, r.outcome);
            Assert.AreEqual(movesBefore, g.totalMoves);
        }

        [Test]
        public void LaEliminacionSoloCuentaCuandoLosDosHanJugado()
        {
            var g = NewGame();
            var r = g.PlacePoint(0, 0, 1);
            // El rival todavia no jugo (tiene 0 celdas), pero eso no es una victoria.
            Assert.IsFalse(r.gameOver);
            Assert.AreEqual(0, r.winner);
        }

        [Test]
        public void AlLlegarAlTopeDeTurnosGanaQuienTieneMasCeldas()
        {
            var g = NewGame(maxTurns: 3);
            g.PlacePoint(0, 0, 1);
            g.PlacePoint(5, 5, 2);
            var r = g.PlacePoint(0, 1, 1); // jugada 3: P1 tiene 2 celdas, P2 tiene 1
            Assert.IsTrue(r.gameOver);
            Assert.AreEqual(1, r.winner);
            Assert.AreEqual(1, g.FinalWinner);
        }

        [Test]
        public void AlLlegarAlTopeDeTurnosConIgualdadDeCeldasEsEmpate()
        {
            var g = NewGame(maxTurns: 2);
            g.PlacePoint(0, 0, 1);
            var r = g.PlacePoint(5, 5, 2);
            Assert.IsTrue(r.gameOver);
            Assert.AreEqual(0, r.winner);
            Assert.IsTrue(g.IsOver);
            Assert.AreEqual(0, g.FinalWinner);
        }

        [Test]
        public void ConTopeDeTurnosCeroLaPartidaNoTerminaPorTurnos()
        {
            var g = NewGame(maxTurns: 0);
            Assert.IsTrue(g.IsInfinite);
            // 32 jugadas en celdas distintas y separadas: sin explosiones ni eliminacion.
            for (int i = 0; i < 16; i++)
            {
                int a = 2 * i, b = 2 * i + 1;
                Assert.IsTrue(g.PlacePoint(a % 6, a / 6, 1).success);
                Assert.IsTrue(g.PlacePoint(b % 6, b / 6, 2).success);
            }
            Assert.IsFalse(g.IsOver);
            Assert.AreEqual(32, g.totalMoves);
        }

        [Test]
        public void ResetDejaLaPartidaComoNueva()
        {
            var g = NewGame(maxTurns: 2);
            g.PlacePoint(0, 0, 1);
            g.PlacePoint(5, 5, 2);
            Assert.IsTrue(g.IsOver);

            g.Reset();

            Assert.IsFalse(g.IsOver);
            Assert.AreEqual(0, g.FinalWinner);
            Assert.AreEqual(0, g.totalMoves);
            Assert.AreEqual(1, g.currentPlayer);
            Assert.AreEqual(0, g.CountCells(1) + g.CountCells(2));
        }
    }
}
