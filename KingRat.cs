using Godot;
using System.Threading.Tasks;

public partial class KingRat : CharacterBody2D
{
    [ExportGroup("Visual")]
    [Export] public float animacao_fps = 5.0f;
    [Export] public bool virar_sprite_ao_andar = true;
    [Export] public string nome_animacao_king_rat = "king_rat";

    [ExportGroup("Patrulha")]
    [Export] public bool patrulhar = true;
    [Export] public float velocidade = 25.0f;
    [Export] public float distancia_patrulha = 90.0f;
    [Export] public bool comecar_para_esquerda = true;
    [Export] public float tempo_parado_ao_virar = 1.2f;

    [ExportGroup("Ataque")]
    [Export] public bool atacar = true;
    [Export] public PackedScene? cena_tiro;
    [Export] public float intervalo_ataque = 2.0f;
    [Export] public int frame_do_tiro = 12;
    [Export] public float tempo_depois_do_tiro = 0.4f;
    [Export] public float velocidade_tiro = 90.0f;
    [Export] public bool atacar_parado = true;
    [Export] public bool reiniciar_animacao_ao_atacar = true;
    [Export] public float offset_tiro_x = 18.0f;
    [Export] public float offset_tiro_y;
    [Export] public bool inverter_direcao_do_tiro = true;

    [ExportGroup("Física")]
    [Export] public bool usar_gravidade;
    [Export] public float gravidade = 900.0f;

    private AnimatedSprite2D _anim = null!;
    private Timer _timerAtaque = null!;
    private Vector2 _posicaoInicial;
    private int _direcao = -1;
    private bool _esperando;
    private bool _atacando;

    public override void _Ready()
    {
        _anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _timerAtaque = GetNode<Timer>("TimerAtaque");
        _posicaoInicial = GlobalPosition;
        _direcao = comecar_para_esquerda ? -1 : 1;
        _configurar_animacao();
        _atualizar_sprite();
        _tocar_animacao_king_rat();

        _timerAtaque.WaitTime = intervalo_ataque;
        _timerAtaque.OneShot = false;
        _timerAtaque.Timeout += _on_timer_ataque_timeout;
        if (atacar)
            _timerAtaque.Start();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        if (usar_gravidade && !IsOnFloor())
            velocity.Y += gravidade * (float)delta;

        if (_atacando && atacar_parado)
        {
            velocity.X = 0.0f;
            Velocity = velocity;
            MoveAndSlide();
            return;
        }

        if (!patrulhar || _esperando)
        {
            velocity.X = 0.0f;
            Velocity = velocity;
            _tocar_animacao_king_rat();
            MoveAndSlide();
            return;
        }

        velocity.X = _direcao * velocidade;
        Velocity = velocity;
        _tocar_animacao_king_rat();
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
        if (_esperando || _atacando)
            return;

        _esperando = true;
        Velocity = new Vector2(0.0f, Velocity.Y);
        _tocar_animacao_king_rat();
        await ToSignal(GetTree().CreateTimer(tempo_parado_ao_virar), SceneTreeTimer.SignalName.Timeout);
        while (_atacando)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        _direcao *= -1;
        _atualizar_sprite();
        _esperando = false;
    }

    private void _on_timer_ataque_timeout()
    {
        if (atacar && !_atacando && !_esperando)
            _executar_ataque();
    }

    private async void _executar_ataque()
    {
        _atacando = true;
        Velocity = new Vector2(0.0f, Velocity.Y);
        _atualizar_sprite();
        if (reiniciar_animacao_ao_atacar)
        {
            _anim.Stop();
            _anim.Frame = 0;
        }
        _tocar_animacao_king_rat();
        await _esperar_frame_do_tiro();

        int direcaoTiro = inverter_direcao_do_tiro ? -_direcao : _direcao;
        _criar_tiro(direcaoTiro);
        await ToSignal(GetTree().CreateTimer(tempo_depois_do_tiro), SceneTreeTimer.SignalName.Timeout);
        _atacando = false;
    }

    private async Task _esperar_frame_do_tiro()
    {
        if (_anim.SpriteFrames is null || !_anim.SpriteFrames.HasAnimation(nome_animacao_king_rat))
        {
            await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
            return;
        }

        int totalFrames = _anim.SpriteFrames.GetFrameCount(nome_animacao_king_rat);
        int frameAlvo = Mathf.Clamp(frame_do_tiro, 0, totalFrames - 1);
        while (_anim.Frame < frameAlvo)
            await ToSignal(_anim, AnimatedSprite2D.SignalName.FrameChanged);
    }

    private void _criar_tiro(int direcaoTiro)
    {
        if (cena_tiro is null)
        {
            GD.Print("ERRO: cena_tiro não foi definida no Inspector.");
            return;
        }

        Node2D tiro = cena_tiro.Instantiate<Node2D>();
        if (tiro is KingRatTiro kingRatTiro)
        {
            kingRatTiro.direcao = direcaoTiro;
            kingRatTiro.velocidade = velocidade_tiro;
        }
        else
        {
            tiro.Set("direcao", direcaoTiro);
            tiro.Set("velocidade", velocidade_tiro);
        }
        tiro.GlobalPosition = GlobalPosition + new Vector2(offset_tiro_x * direcaoTiro, offset_tiro_y);
        GetParent().AddChild(tiro);
    }

    private void _atualizar_sprite()
    {
        if (virar_sprite_ao_andar)
            _anim.FlipH = _direcao < 0;
    }

    private void _tocar_animacao_king_rat()
    {
        if (_anim.SpriteFrames is null)
        {
            GD.Print("ERRO: AnimatedSprite2D está sem SpriteFrames.");
            return;
        }
        if (!_anim.SpriteFrames.HasAnimation(nome_animacao_king_rat))
        {
            GD.Print("ERRO: animação não encontrada: ", nome_animacao_king_rat);
            return;
        }
        if (_anim.Animation.ToString() != nome_animacao_king_rat || !_anim.IsPlaying())
            _anim.Play(nome_animacao_king_rat);
    }

    private void _configurar_animacao()
    {
        if (_anim.SpriteFrames?.HasAnimation(nome_animacao_king_rat) == true)
            _anim.SpriteFrames.SetAnimationSpeed(nome_animacao_king_rat, animacao_fps);
    }
}
