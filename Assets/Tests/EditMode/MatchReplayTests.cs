using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReactionDomino.Core;

namespace ReactionDomino.Core.Tests
{
    /// <summary>
    /// Pruebas de la reproduccion de partidas: la base de la autoridad del servidor.
    /// El servidor guarda solo la lista de jugadas y las reproduce en cada llamada.
    /// </summary>
    public class MatchReplayTests
    {
        // Partida que termina por una explosion en cadena (ver GameLogicTests): 7 jugadas.
        private static List<GridPoint> ChainMatchMoves()
        {
            var moves = new List<GridPoint>();
            for (int i = 0; i < 4; i++)
            {
                moves.Add(new GridPoint(2, 2));
                if (i < 3) moves.Add(new GridPoint(2, 3));
            }
            return moves;
        }

        [Test]
        public void ReproducirLasJugadasDaElMismoEstadoQueJugarlas()
        {
            var moves = new List<GridPoint>
            {
                new GridPoint(0, 0), new GridPoint(5, 5), new GridPoint(0, 0), new GridPoint(5, 4), new GridPoint(3, 3),
            };

            var played = new GameLogic(6, 4, 30);
            foreach (var m in moves) played.PlacePoint(m.x, m.y, played.currentPlayer);

            var replayed = MatchReplay.Replay(6, 4, 30, moves);

            Assert.AreEqual(played.totalMoves, replayed.totalMoves);
            Assert.AreEqual(played.currentPlayer, replayed.currentPlayer);
            for (int x = 0; x < 6; x++)
                for (int y = 0; y < 6; y++)
                {
                    Assert.AreEqual(played.owner[x, y], replayed.owner[x, y], "owner " + x + "," + y);
                    Assert.AreEqual(played.level[x, y], replayed.level[x, y], "level " + x + "," + y);
                }
        }

        [Test]
        public void ReproducirDosVecesDaSiempreElMismoResultado()
        {
            var moves = ChainMatchMoves();
            var a = MatchReplay.Replay(6, 4, 30, moves);
            var b = MatchReplay.Replay(6, 4, 30, moves);
            Assert.AreEqual(a.FinalWinner, b.FinalWinner);
            Assert.AreEqual(a.totalMoves, b.totalMoves);
        }

        [Test]
        public void ElJugadorQueMueveSeDeduceDelOrden()
        {
            // La lista no dice quien juega: jugada 1 = jugador 1, jugada 2 = jugador 2...
            var game = MatchReplay.Replay(6, 4, 30, new List<GridPoint> { new GridPoint(0, 0), new GridPoint(1, 0) });
            Assert.AreEqual(1, game.owner[0, 0]);
            Assert.AreEqual(2, game.owner[1, 0]);
            Assert.AreEqual(1, game.currentPlayer);
        }

        [Test]
        public void UnaPartidaTerminadaSeReproduceComoTerminada()
        {
            var game = MatchReplay.Replay(6, 4, 30, ChainMatchMoves());
            Assert.IsTrue(game.IsOver);
            Assert.AreEqual(1, game.FinalWinner);
        }

        [Test]
        public void UnaListaVaciaEsUnaPartidaNueva()
        {
            var game = MatchReplay.Replay(6, 4, 30, new List<GridPoint>());
            Assert.AreEqual(0, game.totalMoves);
            Assert.AreEqual(1, game.currentPlayer);
        }

        [Test]
        public void TryReplayDiceCualJugadaEsInvalida()
        {
            // La jugada 1 (indice 1) intenta ocupar la celda que el rival acaba de tomar.
            var moves = new List<GridPoint> { new GridPoint(0, 0), new GridPoint(0, 0) };
            bool ok = MatchReplay.TryReplay(6, 4, 30, moves, out _, out int failedIndex);
            Assert.IsFalse(ok);
            Assert.AreEqual(1, failedIndex);
        }

        [Test]
        public void UnRegistroConJugadasDespuesDelFinalEsCorrupto()
        {
            var moves = ChainMatchMoves();
            moves.Add(new GridPoint(0, 0)); // jugada despues de terminada la partida
            bool ok = MatchReplay.TryReplay(6, 4, 30, moves, out _, out int failedIndex);
            Assert.IsFalse(ok);
            Assert.AreEqual(moves.Count - 1, failedIndex);
        }

        [Test]
        public void ReplayLanzaExcepcionSiElRegistroEstaCorrupto()
        {
            var moves = new List<GridPoint> { new GridPoint(9, 9) };
            Assert.Throws<InvalidOperationException>(() => MatchReplay.Replay(6, 4, 30, moves));
        }
    }
}
