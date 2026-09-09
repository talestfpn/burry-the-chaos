using Godot;

public partial class PlayerPolishTarget : Area2D
{
    public int Hits { get; private set; }

    public void tomar_dano(int quantidade, Node2D? atacante = null)
    {
        if (quantidade > 0)
            Hits++;
    }
}
