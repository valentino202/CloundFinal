using UnityEngine;
using Unity.Services.Authentication;

/// <summary>
/// PASO 1 del PvP: modelo de BUSQUEDA de partida (solo datos, SIN RED todavia).
///
/// Es lo que un jugador "pone en la cola" cuando busca rival:
///   - modo     : 0 = Normal (con limite de turnos), 1 = Infinito (sin limite).
///   - maxTurns : el limite EXACTO que pide (30..100). Solo importa en Normal.
///   - playerId : quien busca (el PlayerId de Unity Auth).
///
/// Ejemplo: { modo=Normal, maxTurns=40, playerId=abc123 }
///   -> mas adelante (Paso 4) este dato viaja por el Lobby y solo empareja
///      con OTRO jugador que pida exactamente lo mismo (Normal + 40).
///   - Infinito filtra SOLO por modo (cualquier maxTurns sirve).
///
/// Todavia no hay red: al dar Play se imprime por consola y nada mas.
/// El juego local (humano vs bot) NO se toca.
/// </summary>
[System.Serializable]
public class BusquedaPvP
{
    // Modos: coinciden con GameManager.MatchMode y con PlayerPrefs "SelectedMatchMode".
    public const int ModoNormal = 0;
    public const int ModoInfinito = 1;

    public int modo;
    public int maxTurns;
    public string playerId;

    public BusquedaPvP(int modo, int maxTurns, string playerId)
    {
        this.modo = modo;
        this.maxTurns = Mathf.Clamp(maxTurns, 30, 100);
        this.playerId = playerId ?? "";
    }

    /// <summary>Crea la busqueda con la seleccion actual del menú
    /// (el modo y los turnos que ya estaban en PlayerPrefs).</summary>
    public static BusquedaPvP Crear(GameManager.MatchMode modo, int maxTurns)
    {
        return new BusquedaPvP((int)modo, maxTurns, PlayerIdActual());
    }

    /// <summary>PlayerId de la sesion activa. Si no hay sesion, string vacio
    /// (el juego local funciona igual, sin cuenta).</summary>
    static string PlayerIdActual()
    {
        try
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
                return AuthenticationService.Instance.PlayerId;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PvP] No se pudo leer el PlayerId: {e.Message}");
        }
        return "";
    }

    /// <summary>True si esta busqueda es compatible con otra (lo que usa el
    /// emparejamiento en el Paso 4): mismo modo y, si es Normal, mismos turnos.
    /// Infinito solo exige que el modo coincida.</summary>
    public bool CompatibleCon(BusquedaPvP otra)
    {
        if (otra == null) return false;
        if (modo != otra.modo) return false;
        if (modo == ModoInfinito) return true; // infinito: cualquiera sirve
        return maxTurns == otra.maxTurns;      // normal: EXACTO igual
    }

    /// <summary>Texto legible para la consola.</summary>
    public override string ToString()
    {
        string modoTxt = modo == ModoInfinito ? "Infinito" : "Normal";
        string turnosTxt = modo == ModoInfinito ? "sin limite" : $"{maxTurns} turnos";
        string jugador = string.IsNullOrEmpty(playerId) ? "sin-sesion" : playerId;
        return $"modo={modoTxt} ({turnosTxt}) playerId={jugador}";
    }
}
