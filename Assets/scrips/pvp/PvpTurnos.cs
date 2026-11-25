using System;
using UnityEngine;

/// <summary>
/// ESQUELETO — Paso 6: sincronizacion de turnos y fin de partida.
/// Controla de quien es el turno, el contador de turnos (maxTurns o infinito)
/// y el evento de victoria/empate. Evita que ambos muevan a la vez (busy por red).
///
/// ESTADO: esqueleto (no implementado). Se ensambla con el profesor.
/// </summary>
public class PvpTurnos
{
    public int JugadorActual { get; private set; } = 1;
    public int MaxTurns { get; private set; } = 30;
    public bool EsInfinito => MaxTurns <= 0;
    public int TurnosJugados { get; private set; } = 0;
    public bool Busy { get; private set; } = false;

    /// <summary>Se dispara cuando cambia el turno (1 o 2).</summary>
    public event Action<int> OnTurnoCambiado;
    /// <summary>Se dispara al terminar: (ganador, modo). ganador 0=empate.</summary>
    public event Action<int, int> OnFinPartida; // winner, matchMode

    public void Configurar(int maxTurns)
    {
        MaxTurns = maxTurns;
        JugadorActual = 1;
        TurnosJugados = 0;
        Busy = false;
    }

    /// <summary>Intenta jugar. Devuelve false si no es tu turno o esta busy.
    /// (ESQUELETO)</summary>
    public bool IntentarJugar(int jugadorQueQuiereMover)
    {
        if (Busy) { Debug.Log("[PvP-Turnos-ESQUELETO] busy, espera."); return false; }
        if (jugadorQueQuiereMover != JugadorActual) { Debug.Log("[PvP-Turnos-ESQUELETO] no es tu turno."); return false; }
        Busy = true;
        return true;
    }

    /// <summary>Llamar cuando la jugada fue aplicada con exito.
    /// Actualiza turno y detecta fin de partida. (ESQUELETO)</summary>
    public void ConfirmarJugada(bool gameOver, int winner, int matchMode)
    {
        TurnosJugados++;
        Busy = false;
        if (gameOver)
        {
            OnFinPartida?.Invoke(winner, matchMode);
            return;
        }
        JugadorActual = JugadorActual == 1 ? 2 : 1;
        OnTurnoCambiado?.Invoke(JugadorActual);
    }

    /// <summary>Reinicia para una nueva partida. (ESQUELETO)</summary>
    public void Reset(int maxTurns)
    {
        Configurar(maxTurns);
    }
}
