using Godot;

public partial class TransicaoCena : Area2D
{
    [Export] public string connection_id = "";
    [Export(PropertyHint.File, "*.tscn")] public string proxima_cena = "";
    [Export] public string spawn_destino = "Default";
    [Export] public bool precisa_interagir;
    [Export] public Node2D? sprite_do_elevador;

    private bool _playerNaArea;
    private Player? _player;
    private bool _transicionando;

    public override void _Ready()
    {
        BodyEntered += _on_body_entered;
        BodyExited += _on_body_exited;
    }

    private void _on_body_entered(Node2D body)
    {
        if (body.Name != "bury" || body is not Player player)
            return;

        _player = player;
        if (precisa_interagir)
            _playerNaArea = true;
        else
            iniciar_transicao();
    }

    private void _on_body_exited(Node2D body)
    {
        if (body.Name == "bury")
        {
            _playerNaArea = false;
            _player = null;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_playerNaArea && precisa_interagir && !_transicionando && @event.IsActionPressed("interact"))
            iniciar_transicao();
    }

    public async void iniciar_transicao()
    {
        if (_transicionando)
            return;

        _transicionando = true;
        if (precisa_interagir)
        {
            if (_player is not null && sprite_do_elevador is not null)
            {
                Player player = _player;
                AnimatedSprite2D animPlayer = player.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
                AnimatedSprite2D animElevador = sprite_do_elevador.GetNode<AnimatedSprite2D>("AnimatedSprite2D");

                player.SetPhysicsProcess(false);
                player.Velocity = Vector2.Zero;
                animPlayer.Play("personagem_entrando");
                await ToSignal(GetTree().CreateTimer(0.9), SceneTreeTimer.SignalName.Timeout);

                animPlayer.Pause();
                animElevador.Play("abrindo");
                await ToSignal(animElevador, AnimatedSprite2D.SignalName.AnimationFinished);

                animPlayer.Play("personagem_andando");
                player.ZIndex = -1;
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(player, "global_position:x", sprite_do_elevador.GlobalPosition.X, 0.6);
                tween.Parallel().TweenProperty(player, "scale", new Vector2(0.85f, 0.85f), 0.6);
                tween.Parallel().TweenProperty(player, "modulate", new Color(0.3f, 0.3f, 0.3f, 1.0f), 0.6);
                await ToSignal(tween, Tween.SignalName.Finished);

                animElevador.PlayBackwards("abrindo");
                await ToSignal(animElevador, AnimatedSprite2D.SignalName.AnimationFinished);
                await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
            }
            else
            {
                await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
            }
        }

        mudar_de_cena();
    }

    public void mudar_de_cena()
    {
        if (string.IsNullOrWhiteSpace(proxima_cena))
            return;

        Global? gameManager = GetNodeOrNull<Global>("/root/Global");
        if (gameManager is not null)
            gameManager.trocar_cena(proxima_cena, spawn_destino);
        else
            GetTree().ChangeSceneToFile(proxima_cena);
    }
}
