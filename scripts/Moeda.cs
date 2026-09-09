using Godot;

public partial class Moeda : Area2D
{
    [Export]
    public int valor = 1;

    private bool _coletada;

    public override void _Ready()
    {
        BodyEntered += _on_body_entered;
    }

    public void _on_body_entered(Node2D body)
    {
        if (_coletada || !body.IsInGroup("Jogador"))
            return;

        _coletada = true;
        GetNode<Global>("/root/Global").adicionar_moedas(valor);
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        SetDeferred(Area2D.PropertyName.Monitorable, false);
        _animar_coleta();
    }

    private async void _animar_coleta()
    {
        Tween tween = GetTree().CreateTween();
        tween.Parallel().TweenProperty(this, "position:y", Position.Y - 10.0f, 0.18);
        tween.Parallel().TweenProperty(this, "scale", new Vector2(1.3f, 1.3f), 0.08);
        tween.Parallel().TweenProperty(this, "modulate:a", 0.0f, 0.18);
        await ToSignal(tween, Tween.SignalName.Finished);
        QueueFree();
    }
}
