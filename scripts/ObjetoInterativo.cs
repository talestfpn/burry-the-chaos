using Godot;

public partial class ObjetoInterativo : Area2D
{
    [ExportGroup("Textos e Animações")]
    [Export(PropertyHint.MultilineText)] public string texto_diálogo = "Lorem ipsum dolor sit amet...";
    [Export] public string animacao_parado = "desligado";
    [Export] public string animacao_interagindo = "ligado";

    [ExportGroup("Comportamento")]
    [Export] public bool tipo_interruptor;
    [Export] public bool interacao_unica;
    [Export] public bool dar_passinho = true;

    [ExportGroup("Nós Filhos (Arraste aqui)")]
    [Export] public AnimatedSprite2D? sprite_objeto;
    [Export] public Control? chat_box;
    [Export] public Label? label_texto;

    private bool _playerPerto;
    private Player? _player;
    private bool _interagindo;
    private bool _estadoLigado;
    private Tween? _tweenDialogo;
    private float _posicaoVisivel;
    private float _posicaoEscondida;

    public override void _Ready()
    {
        if (chat_box is not null)
        {
            _posicaoVisivel = chat_box.Position.Y;
            _posicaoEscondida = _posicaoVisivel + chat_box.Size.Y + 50.0f;
            chat_box.Position = new Vector2(chat_box.Position.X, _posicaoEscondida);
            chat_box.Show();
        }

        _tocar_animacao_objeto(animacao_parado);

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
        if (body.Name != "bury")
            return;

        if (tipo_interruptor)
        {
            _playerPerto = false;
            _player = null;
            return;
        }

        if (!_interagindo)
        {
            _playerPerto = false;
            _player = null;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("interact") || !_playerPerto || _player is null)
            return;

        if (interacao_unica && _estadoLigado)
            return;

        if (tipo_interruptor)
            acionar_interruptor();
        else if (_interagindo)
            encerrar_interacao();
        else if (_player.IsPhysicsProcessing())
            iniciar_interacao();
    }

    public void acionar_interruptor()
    {
        if (_player is null)
            return;

        _estadoLigado = interacao_unica || !_estadoLigado;
        _tocar_animacao_objeto(_estadoLigado ? animacao_interagindo : animacao_parado);

        int direcaoOlhar = _player.GlobalPosition.X > GlobalPosition.X ? -1 : 1;
        _player.facing_direction = direcaoOlhar;
        AnimatedSprite2D? anim = _player.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (anim is not null)
            anim.FlipH = direcaoOlhar < 0;
    }

    public async void iniciar_interacao()
    {
        if (_player is null)
            return;

        _estadoLigado = true;
        _interagindo = true;
        Player player = _player;
        player.SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;
        AnimatedSprite2D animPlayer = player.GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        if (dar_passinho)
        {
            int direcaoPasso = -player.facing_direction;
            float posicaoAlvo = player.GlobalPosition.X + direcaoPasso * 24.0f;
            animPlayer.Play("walk");
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(player, "global_position:x", posicaoAlvo, 0.25);
            await ToSignal(tween, Tween.SignalName.Finished);
            animPlayer.Play("idle");
            player.facing_direction = -direcaoPasso;
            animPlayer.FlipH = player.facing_direction < 0;
        }
        else
        {
            int direcaoOlhar = player.GlobalPosition.X > GlobalPosition.X ? -1 : 1;
            animPlayer.Play("idle");
            player.facing_direction = direcaoOlhar;
            animPlayer.FlipH = direcaoOlhar < 0;
        }

        if (label_texto is not null)
            label_texto.Text = texto_diálogo;

        if (chat_box is not null)
        {
            if (_tweenDialogo is not null && _tweenDialogo.IsValid())
                _tweenDialogo.Kill();
            _tweenDialogo = GetTree().CreateTween().SetTrans(Tween.TransitionType.Spring).SetEase(Tween.EaseType.Out);
            _tweenDialogo.TweenProperty(chat_box, "position:y", _posicaoVisivel, 0.5);
        }

        _tocar_animacao_objeto(animacao_interagindo);
    }

    public void encerrar_interacao()
    {
        _interagindo = false;
        if (chat_box is not null)
        {
            if (_tweenDialogo is not null && _tweenDialogo.IsValid())
                _tweenDialogo.Kill();
            _tweenDialogo = GetTree().CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
            _tweenDialogo.TweenProperty(chat_box, "position:y", _posicaoEscondida, 0.25);
        }

        if (!interacao_unica)
            _tocar_animacao_objeto(animacao_parado);

        if (_player is not null)
        {
            _player.SetPhysicsProcess(true);
            if (!OverlapsBody(_player))
            {
                _playerPerto = false;
                _player = null;
            }
        }
    }

    private void _tocar_animacao_objeto(string nome)
    {
        if (sprite_objeto?.SpriteFrames?.HasAnimation(nome) == true)
            sprite_objeto.Play(nome);
    }
}
