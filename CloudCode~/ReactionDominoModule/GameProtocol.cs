namespace ReactionDomino.CloudCode
{
    /// <summary>
    /// Version del contrato entre cliente y servidor. El cliente la compara con la que devuelve
    /// <c>GetGameConfig</c>. Subela (aqui Y en el gemelo del cliente) cada vez que cambies la forma
    /// de una funcion: parametros, campos de las respuestas o significado de un outcome.
    /// </summary>
    public static class GameProtocol
    {
        public const int Version = 1;
    }

    /// <summary>
    /// Reglas por defecto de una partida online. El servidor las fija al crear la partida y las
    /// guarda en el registro, asi los dos jugadores juegan SIEMPRE con las mismas reglas.
    /// TAREA para el alumno: leerlas de Remote Config (hoy son constantes).
    /// </summary>
    public static class GameDefaults
    {
        public const int GridSize = 6;
        public const int MaxCellLevel = 4;
        public const int MaxTurns = 30;
    }
}
