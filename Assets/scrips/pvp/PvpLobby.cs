using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ESQUELETO — Paso 4: Lobby (UGS Lobby) para emparejamiento.
/// Crea/une salas con atributos { modo, maxTurns } y filtra para que el
/// jugador de Normal 40 solo empareje con otro de Normal 40.
///
/// ESTADO: esqueleto (no implementado). Las llamadas reales a UGS Lobby
/// estan en los TODO. Se ensambla con el profesor.
/// Paquete necesario (no instalado aun): com.unity.services.lobby
/// </summary>
public class PvpLobby
{
    /// <summary>Busqueda actual del jugador (la que puso en cola).</summary>
    public BusquedaPvP BusquedaActual { get; private set; }

    /// <summary>Id de la sala (lobby) a la que estamos unidos.</summary>
    public string LobbyIdActual { get; private set; }

    /// <summary>Crea una sala con los datos de la busqueda y espera rival.
    /// Devuelve el id de la sala. (ESQUELETO)</summary>
    public async Task<string> CrearSala(BusquedaPvP busqueda)
    {
        BusquedaActual = busqueda;
        // TODO (con profesor): instalar com.unity.services.lobby y llamar:
        //   var options = new CreateLobbyOptions {
        //       IsPrivate = false,
        //       Data = new Dictionary<string, DataObject> {
        //           { "modo", new DataObject(DataObject.VisibilityOptions.Public, busqueda.modo.ToString()) },
        //           { "maxTurns", new DataObject(DataObject.VisibilityOptions.Public, busqueda.maxTurns.ToString()) }
        //       }
        //   };
        //   var lobby = await LobbyService.Instance.CreateLobbyAsync("partida", 2, options);
        //   LobbyIdActual = lobby.Id;
        //   return lobby.Id;
        Debug.Log($"[PvP-Lobby-ESQUELETO] CrearSala({busqueda}) — no implementado.");
        await Task.CompletedTask;
        return null;
    }

    /// <summary>Busca salas compatibles con la busqueda (mismo modo; si es
    /// Normal, mismos turnos). Devuelve la lista de ids compatibles.
    /// (ESQUELETO)</summary>
    public async Task<List<string>> BuscarSalas(BusquedaPvP busqueda)
    {
        BusquedaActual = busqueda;
        var compatibles = new List<string>();
        // TODO (con profesor): LobbyService.Instance.QueryLobbiesAsync con filtros:
        //   var query = new QueryLobbiesOptions {
        //       Filters = new List<QueryFilter> {
        //           new QueryFilter(QueryFilter.FieldOptions.S1, busqueda.modo.ToString(), QueryFilter.OpOptions.EQ),
        //           // si Normal: new QueryFilter(QueryFilter.FieldOptions.N1, busqueda.maxTurns.ToString(), QueryFilter.OpOptions.EQ)
        //       }
        //   };
        //   var response = await LobbyService.Instance.QueryLobbiesAsync(query);
        //   foreach (var l in response.Results) if (busqueda.CompatibleCon(LeerBusqueda(l))) compatibles.Add(l.Id);
        Debug.Log($"[PvP-Lobby-ESQUELETO] BuscarSalas({busqueda}) — no implementado.");
        await Task.CompletedTask;
        return compatibles;
    }

    /// <summary>Se une a una sala existente. (ESQUELETO)</summary>
    public async Task UnirseSala(string lobbyId)
    {
        LobbyIdActual = lobbyId;
        // TODO (con profesor): await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        Debug.Log($"[PvP-Lobby-ESQUELETO] UnirseSala({lobbyId}) — no implementado.");
        await Task.CompletedTask;
    }

    /// <summary>Sale de la sala actual. (ESQUELETO)</summary>
    public async Task SalirSala()
    {
        // TODO (con profesor): if (LobbyIdActual != null) await LobbyService.Instance.RemovePlayerAsync(LobbyIdActual, playerId);
        LobbyIdActual = null;
        Debug.Log("[PvP-Lobby-ESQUELETO] SalirSala — no implementado.");
        await Task.CompletedTask;
    }

    /// <summary>Convierte un lobby de UGS en una BusquedaPvP (para filtrar).
    /// (ESQUELETO)</summary>
    public static BusquedaPvP LeerBusquedaDeLobby(object lobby)
    {
        // TODO: leer lobby.Data["modo"] y lobby.Data["maxTurns"].
        return null;
    }
}
