using Godot;

public partial class Rato : CharacterBody2D
{
    [ExportGroup("Visual")]
    [Export(PropertyHint.Enum, "branco,cinza,marrom")] public string tipo_rato = "marrom";
    [Export] public float animacao_fps = 8.0f;
    [Export] public bool virar_sprite_ao_andar = true;

    [ExportGroup("Patrulha")]
    [Export] public bool patrulhar = true;
    [Export] public float velocidade = 35.0f;
    [Export] public float distancia_patrulha = 80.0f;
    [Export] public bool comecar_para_esquerda = true;
    [Export] public float tempo_parado_ao_virar = 1.5f;

    [ExportGroup("Física")]
    [Export] public bool usar_gravidade = true;
    [Export] public float gravidade = 900.0f;

    private AnimatedSprite2D _anim = null!;
    private Vector2 _posicaoInicial;
    private int _direcao = -1;
    private bool _esperando;

    public override void _Ready()
    {
        _anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _posicaoInicial = GlobalPosition;
        _direcao = comecar_para_esquerda ? -1 : 1;
        _configurar_animacoes();
        _atualizar_sprite();
        _tocar_animacao_idle();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        if (usar_gravidade && !IsOnFloor())
            velocity.Y += gravidade * (float)delta;

        if (!patrulhar || _esperando)
        {
            velocity.X = 0.0f;
            Velocity = velocity;
            _tocar_animacao_idle();
            MoveAndSlide();
            return;
        }

        velocity.X = _direcao * velocidade;
        Velocity = velocity;
        _tocar_animacao_walk();
        _atualizar_sprite();
        MoveAndSlide();
        _verificar_limite_patrulha();
    }

    private void _verificar_limite_patrulha()
    {
        if (Mathf.Abs(GlobalPosition.X - _posicaoInicial.X) >= distancia_patrulha || IsOnWall())
            _parar_e_inverter_direcao();
    }

    private async void _parar_e_inverter_direcao()
    {
        if (_esperando)
            return;

        _esperando = true;
        Velocity = new Vector2(0.0f, Velocity.Y);
        _tocar_animacao_idle();
        await ToSignal(GetTree().CreateTimer(tempo_parado_ao_virar), SceneTreeTimer.SignalName.Timeout);
        _direcao *= -1;
        _atualizar_sprite();
        _esperando = false;
    }

    private void _atualizar_sprite()
    {
        if (virar_sprite_ao_andar)
            _anim.FlipH = _direcao < 0;
    }

    private void _tocar_animacao_idle() => _tocar_animacao_segura($"{tipo_rato}_idle");
    private void _tocar_animacao_walk() => _tocar_animacao_segura($"{tipo_rato}_walk");

    private void _tocar_animacao_segura(string nomeAnimacao)
    {
        if (_anim.SpriteFrames is null)
        {
            GD.Print("ERRO: AnimatedSprite2D está sem SpriteFrames.");
            return;
        }
        if (!_anim.SpriteFrames.HasAnimation(nomeAnimacao))
        {
            GD.Print("ERRO: animação não encontrada: ", nomeAnimacao);
            return;
        }
        if (_anim.Animation.ToString() != nomeAnimacao)
            _anim.Play(nomeAnimacao);
    }

    private void _configurar_animacoes()
    {
        if (_anim.SpriteFrames is null)
            return;

        string[] animacoes =
        {
            "branco_idle", "branco_walk", "cinza_idle",
            "cinza_walk", "marrom_idle", "marrom_walk"
        };
        foreach (string nome in animacoes)
        {
            if (_anim.SpriteFrames.HasAnimation(nome))
                _anim.SpriteFrames.SetAnimationSpeed(nome, animacao_fps);
        }
    }
}
