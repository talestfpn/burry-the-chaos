using Godot;

public partial class Espinho : Area2D
{
    [Export]
    public int dano = 1;

    public void _on_body_entered(Node2D body)
    {
        if (body.Name == "bury" && body is Player player)
            player.tomar_dano(dano, this);
    }
}
