namespace ReactionDomino.Core
{
    /// <summary>
    /// Coordenada de una celda. Reemplaza a <c>UnityEngine.Vector2Int</c> dentro del Core, que no
    /// puede depender de Unity para poder compilarse tambien en el servidor.
    /// En la capa de Unity (<c>GameManager</c>) se convierte con <c>new Vector2Int(p.x, p.y)</c>.
    /// </summary>
    public readonly struct GridPoint
    {
        public readonly int x;
        public readonly int y;

        public GridPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString() => "(" + x + "," + y + ")";
    }
}
