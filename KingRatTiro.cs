using Godot;

public partial class KingRatTiro : Area2D
{
    [Export] public float velocidade = 90.0f;
    [Export] public float tempo_de_vida = 3.0f;

    public int direcao = -1;

    private AnimatedSprite2D _anim = null!;

    public override async void _Ready()
    {
        _anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _anim.Play("tiro");
        _anim.FlipH = direcao < 0;

        await ToSignal(GetTree().CreateTimer(tempo_de_vida), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += new Vector2(direcao * velocidade * (float)delta, 0.0f);
    }
}
