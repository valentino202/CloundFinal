using System;

/// <summary>
/// Estados del flujo PvP (esqueleto, Paso 4+).
/// Se usa para que la UI sepa en qué fase está la partida.
/// </summary>
public enum PvpEstado
{
    Inactivo,        // sin buscar
    Buscando,        // en cola de matchmaking
    EnSala,          // lobby encontrado, esperando al otro / iniciando
    Jugando,         // partida en curso
    Finalizado,      // terminó (victoria/derrota/empate)
    Desconectado,    // se cayó la red
    Cancelado        // el jugador canceló la búsqueda
}
