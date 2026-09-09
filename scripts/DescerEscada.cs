using Godot;

public partial class DescerEscada : Area2D
{
    [Export] public Marker2D? ponto_cima;
    [Export] public Marker2D? ponto_baixo;
    [Export] public Marker2D? ponto_saida_cima;
    [Export] public float velocidade = 0.8f;
    [Export] public float velocidade_ida = 0.4f;

    private bool _playerPerto;
    private Player? _player;
    private bool _emMovimento;

    public override void _Ready()
    {
        BodyEntered += _on_body_entered;
        BodyExited += _on_body_exited;
    }

    private void _on_body_entered(Node2D body)
    {
        if (body.Name == "bury" && body is Player player)
        {
            _playerPerto = true;
            _player = player;
        }
    }

    private void _on_body_exited(Node2D body)
    {
        if (body.Name == "bury" && !_emMovimento)
        {
            _playerPerto = false;
            _player = null;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("interact") && _playerPerto && _player is not null && !_emMovimento)
            usar_escada();
    }

    public async void usar_escada()
    {
        if (_player is null || ponto_cima is null || ponto_baixo is null)
            return;

        _emMovimento = true;
        Player player = _player;
        player.SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;

        AnimatedSprite2D animPlayer = player.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        CollisionShape2D colisao = player.GetNode<CollisionShape2D>("ColisaoNormal");

        float distCima = Mathf.Abs(player.GlobalPosition.Y - ponto_cima.GlobalPosition.Y);
        float distBaixo = Mathf.Abs(player.GlobalPosition.Y - ponto_baixo.GlobalPosition.Y);
        bool subindo = distBaixo < distCima;
        Marker2D pontoInicio = subindo ? ponto_baixo : ponto_cima;
        Marker2D pontoDestino = subindo ? ponto_cima : ponto_baixo;

        colisao.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        float alvoX = pontoInicio.GlobalPosition.X;
        int direcao = Mathf.Sign(alvoX - player.GlobalPosition.X);
        if (Mathf.Abs(alvoX - player.GlobalPosition.X) > 2.0f)
        {
            animPlayer.FlipH = direcao < 0;
            player.facing_direction = direcao;
            animPlayer.Play("walk");

            Tween tweenIr = GetTree().CreateTween();
            tweenIr.TweenProperty(player, "global_position:x", alvoX, velocidade_ida);
            await ToSignal(tweenIr, Tween.SignalName.Finished);
        }

        animPlayer.FlipH = false;
        player.facing_direction = 1;
        animPlayer.Play("personagem_descendo");

        Tween tweenEscada = GetTree().CreateTween();
        tweenEscada.TweenProperty(player, "global_position:y", pontoDestino.GlobalPosition.Y, velocidade);
        await ToSignal(tweenEscada, Tween.SignalName.Finished);

        if (subindo && ponto_saida_cima is not null)
        {
            animPlayer.Play("walk");
            int direcaoSaida = Mathf.Sign(ponto_saida_cima.GlobalPosition.X - player.GlobalPosition.X);
            animPlayer.FlipH = direcaoSaida < 0;
            player.facing_direction = direcaoSaida;

            Tween tweenSaida = GetTree().CreateTween();
            tweenSaida.TweenProperty(player, "global_position", ponto_saida_cima.GlobalPosition, velocidade_ida);
            await ToSignal(tweenSaida, Tween.SignalName.Finished);
        }

        colisao.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        animPlayer.Play("idle");
        player.Velocity = Vector2.Zero;
        player.SetPhysicsProcess(true);
        _emMovimento = false;
    }
}
