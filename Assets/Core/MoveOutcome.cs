namespace ReactionDomino.Core
{
    /// <summary>
    /// Resultado de intentar una jugada. Los rechazos son codigos, no excepciones: el servidor
    /// los devuelve y el cliente decide que mensaje mostrar. <c>Ok</c> = la jugada es legal/se aplico.
    /// </summary>
    public enum MoveOutcome
    {
        Ok = 0,
        MatchFinished,
        InvalidPlayer,
        NotYourTurn,
        OutOfRange,
        CellOwnedByRival,
    }
}
