using System;
using UnityEngine;

/// <summary>
/// ESQUELETO — Coordinador PvP (orquesta Lobby, Sync, Turnos, Recompensas,
/// Robustez). Punto unico de entrada para la UI del menu ("Jugar online").
///
/// ESTADO: esqueleto (no implementado). Se ensambla con el profesor.
/// </summary>
public class PvpManager
{
    public static readonly PvpManager Instance = new PvpManager();

    public PvpEstado Estado { get; private set; } = PvpEstado.Inactivo;
    public PvpLobby Lobby { get; } = new PvpLobby();
    public PvpSync Sync { get; } = new PvpSync();
    public PvpTurnos Turnos { get; } = new PvpTurnos();
    public PvpRecompensas Recompensas { get; } = new PvpRecompensas();
    public PvpRobustez Robustez { get; } = new PvpRobustez();

    public BusquedaPvP BusquedaActual { get; private set; }
    public GameLogic Game { get; private set; }

    /// <summary>Se dispara cuando cambia el estado.</summary>
    public event Action<PvpEstado> OnEstadoCambiado;
    /// <summary>Se dispara cuando se encontro rival y la partida inicia.</summary>
    public event Action OnPartidaEncontrada;

    /// <summary>Inicia la busqueda de partida con la seleccion del menu.
    /// (ESQUELETO)</summary>
    public void IniciarBusqueda(BusquedaPvP busqueda)
    {
        BusquedaActual = busqueda;
        CambiarEstado(PvpEstado.Buscando);
        // TODO (con profesor):
        //   1. Buscar salas compatibles (Lobby.BuscarSalas).
        //   2. Si hay, unirse (Lobby.UnirseSala); si no, crear (Lobby.CrearSala).
        //   3. Al tener 2 jugadores en la sala -> OnPartidaEncontrada.
        Debug.Log($"[PvP-Manager-ESQUELETO] IniciarBusqueda({busqueda}) — no implementado.");
    }

    /// <summary>Cancela la busqueda. (ESQUELETO)</summary>
    public void CancelarBusqueda()
    {
        // TODO (con profesor): Lobby.SalirSala().
        CambiarEstado(PvpEstado.Cancelado);
        Debug.Log("[PvP-Manager-ESQUELETO] CancelarBusqueda — no implementado.");
    }

    /// <summary>Arranca la partida cuando el rival esta listo.
    /// Crea el GameLogic y configura turnos. (ESQUELETO)</summary>
    public void IniciarPartida(int maxTurns)
    {
        Game = new GameLogic(6, 4, maxTurns);
        Turnos.Configurar(maxTurns);
        CambiarEstado(PvpEstado.Jugando);
        // TODO (con profesor): sincronizar GameLogic inicial entre clientes.
        Debug.Log($"[PvP-Manager-ESQUELETO] IniciarPartida(maxTurns={maxTurns}) — no implementado.");
    }

    /// <summary>Reporta el resultado final (llama a PvpRecompensas). (ESQUELETO)</summary>
    public async void FinalizarPartida(string winnerId, string loserId, bool draw,
        int gridSize, int totalMoves, int winnerCells, int loserCells, int matchMode, int maxTurns, float duration)
    {
        await Recompensas.ReportarResultado(winnerId, loserId, draw, gridSize, totalMoves,
            winnerCells, loserCells, matchMode, maxTurns, duration);
        CambiarEstado(PvpEstado.Finalizado);
    }

    void CambiarEstado(PvpEstado nuevo)
    {
        Estado = nuevo;
        OnEstadoCambiado?.Invoke(nuevo);
    }
}
