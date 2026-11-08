using Newtonsoft.Json;
using NUnit.Framework;
using ReactionDomino.CloudCode;
using ReactionDomino.Online;

namespace ReactionDomino.CloudCode.Tests
{
    /// <summary>
    /// Comprueba el CONTRATO entre el servidor y el cliente de Unity: lo que escribe el modulo debe poder
    /// leerlo <c>OnlineDtos.cs</c>. Si cambias un campo en un lado y no en el otro, estas pruebas fallan
    /// aqui, sin necesidad de abrir Unity ni de llamar a UGS.
    /// El cliente de Unity deserializa con Newtonsoft (que no distingue mayusculas), por eso se prueba con
    /// los nombres tal cual los escribe el servidor (PascalCase) y tambien en camelCase.
    /// </summary>
    public class ClientContractTests
    {
        private static MatchRecord FinishedRecord()
        {
            var r = new MatchRecord { MatchId = "rd-T", GridSize = 6, MaxCellLevel = 4, MaxTurns = 30 };
            r.Players.Add(new MatchPlayerRecord { PlayerId = "alice", Seat = 1 });
            r.Players.Add(new MatchPlayerRecord { PlayerId = "bob", Seat = 2 });
            for (int i = 0; i < 4; i++)
            {
                r.Moves.Add(new MoveRecord { X = 2, Y = 2 });
                if (i < 3) r.Moves.Add(new MoveRecord { X = 2, Y = 3 });
            }
            r.Rewards["alice"] = new RewardGrantRecord { Coins = 100, Gems = 50 };
            return r;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ElClienteLeeLaRespuestaDePlayMove(bool camelCase)
        {
            var record = FinishedRecord();
            var dto = MatchStateStore.ToDto(record, MatchStateStore.Replay(record), "alice");
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = camelCase ? System.Text.Json.JsonNamingPolicy.CamelCase : null,
            };
            string json = System.Text.Json.JsonSerializer.Serialize(new PlayMoveResponse { Outcome = "ok", State = dto }, options);

            var client = JsonConvert.DeserializeObject<PlayMoveResponseDto>(json);

            Assert.IsTrue(client.Accepted);
            Assert.AreEqual("finished", client.state.phase);
            Assert.AreEqual(36, client.state.owner.Length);
            Assert.AreEqual(1, client.state.mySeat);
            Assert.AreEqual(1, client.state.winnerSeat);
            Assert.AreEqual(2, client.state.players.Length);
            Assert.AreEqual(2, client.state.lastMove.x);
            Assert.AreEqual(2, client.state.lastMoveExplosions.Length);
            Assert.AreEqual(new[] { 2, 3 }, client.state.lastMoveExplosions[1]);
            Assert.AreEqual(100, client.state.awardedCoins);
            Assert.IsFalse(client.state.IsMyTurn, "la partida termino");
        }

        [Test]
        public void ElIndiceDelTableroEsYPorGridSizeMasX()
        {
            var record = FinishedRecord();
            var dto = MatchStateStore.ToDto(record, MatchStateStore.Replay(record), "bob");
            string json = System.Text.Json.JsonSerializer.Serialize(dto);
            var client = JsonConvert.DeserializeObject<ReactionDomino.Online.MatchStateDto>(json);

            // La cadena dejo a (2,2) con un punto de nivel 1 del asiento 1 (la vuelve a tocar la explosion de (2,3)).
            Assert.AreEqual(1, client.OwnerAt(2, 2));
            Assert.AreEqual(1, client.LevelAt(2, 2));
            Assert.AreEqual(2, client.mySeat);
        }

        [Test]
        public void ElClienteLeeLaConfiguracionYLaVersionDelProtocolo()
        {
            string json = System.Text.Json.JsonSerializer.Serialize(new GameConfigResponse
            {
                ProtocolVersion = GameProtocol.Version, GridSize = 6, MaxCellLevel = 4, MaxTurns = 30,
            });
            var client = JsonConvert.DeserializeObject<GameConfigDto>(json);
            Assert.AreEqual(GameProtocol.Version, client.protocolVersion);
            Assert.AreEqual(6, client.gridSize);
        }
    }
}
