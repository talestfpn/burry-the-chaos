using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    private enum PlayerState
    {
        Grounded,
        Airborne,
        Dashing,
        WallGrip,
        Climbing,
        Attacking,
        Hurt,
        Dead,
        Sliding
    }

    [ExportGroup("Habilidades Liberadas")]
    [Export] public bool pode_pular = true;
    [Export] public bool pode_pulo_duplo = true;
    [Export] public bool pode_atacar = true;
    [Export] public bool pode_dash = true;

    [ExportGroup("Movimento Horizontal")]
    [Export] public float MaxSpeed = 85.0f;
    [Export] public float GroundAcceleration = 900.0f;
    [Export] public float GroundDeceleration = 1100.0f;
    [Export] public float AirAcceleration = 520.0f;
    [Export] public float AirDeceleration = 300.0f;
    [Export] public float ReversalAcceleration = 1200.0f;

    [ExportGroup("Ajustes de Pulo")]
    [Export] public float forca_do_pulo = -300.0f;
    [Export] public float JumpVelocity = -300.0f;
    [Export] public float CoyoteTime = 0.15f;
    [Export] public float JumpBufferTime = 0.12f;
    [Export] public float FallGravity = 1.55f;
    [Export(PropertyHint.Range, "0.1,1.0,0.05")]
    public float JumpCutMultiplier = 0.5f;
    [Export] public float TerminalVelocity = 520.0f;

    [ExportGroup("Dash")]
    [Export] public float DashSpeed = 250.0f;
    [Export] public float DashDuration = 0.16f;
    [Export] public float DashCooldown = 0.35f;
    [Export] public float DashCost = 50.0f;

    [ExportGroup("Stamina")]
    [Export] public float MaxStamina = 100.0f;
    [Export] public float StaminaDrainRate = 20.0f;
    [Export] public float StaminaRegenRate = 66.67f;
    [Export] public float StaminaRegenDelay = 0.65f;

    [ExportGroup("Cenas e Ladeiras")]
    [Export] public float velocidade_escorregamento = 150.0f;

    [ExportGroup("Ajustes de Parede")]
    [Export] public float velocidade_escalada_parede = 60.0f;
    [Export] public float velocidade_deslize_parede = 80.0f;
    [Export] public float offset_visual_parede_x = 2.0f;
    [Export] public float offset_visual_parede_y;
    [Export] public float gasto_stamina_parede = 20.0f;

    [ExportGroup("Wall Jump")]
    [Export] public float WallJumpPush = 180.0f;
    [Export] public float WallJumpLockTime = 0.10f;

    [ExportGroup("Ataque")]
    [Export] public float AttackWindup = 0.05f;
    [Export] public float AttackActiveTime = 0.09f;
    [Export] public float AttackRecovery = 0.11f;
    [Export] public float AttackKnockback = 35.0f;

    [ExportGroup("Dano")]
    [Export] public float InvulnerabilityDuration = 0.60f;
    [Export] public float KnockbackDuration = 0.18f;
    [Export] public float KnockbackHorizontal = 120.0f;
    [Export] public float KnockbackVertical = -80.0f;

    [ExportGroup("Camera")]
    [Export] public float CameraLookAhead = 16.0f;
    [Export] public float CameraVerticalLookAhead = 5.0f;
    [Export] public float CameraLookAheadSpeed = 5.0f;

    [ExportGroup("Debug")]
    [Export] public bool DebugMovement;

    private const int VIDA_MAXIMA = 4;
    private const int DANO_ATAQUE = 1;
    private const float FORCA_POGO = -250.0f;
    private const float LEGACY_JUMP_VELOCITY = -300.0f;
    private const float LEGACY_WALL_STAMINA_DRAIN = 20.0f;

    private AnimatedSprite2D _anim = null!;
    private CollisionShape2D _colisaoNormal = null!;
    private CollisionShape2D _colisaoDash = null!;
    private RayCast2D _tetoCheck = null!;
    private Area2D _hitboxAtaque = null!;
    private CollisionShape2D _hitboxShape = null!;
    private CpuParticles2D? _groundParticles;
    private CpuParticles2D? _dashParticles;
    private CpuParticles2D? _hitParticles;
    private Label? _movementDebugLabel;
    private Camera2D? _camera;
    private Vector2 _cameraBaseOffset;
    private Vector2 _cameraLookOffset;
    private Vector2 _spriteBaseScale = Vector2.One;
    private Tween? _squashTween;
    private readonly HashSet<ulong> _alvosAtingidosNoAtaque = new();

    public float stamina = 100.0f;
    public int vida_atual = VIDA_MAXIMA;
    public int facing_direction = 1;
    private Vector2 _dashDirection = Vector2.Zero;
    private bool _isDashing;
    private bool _estaAtacando;
    private bool _estaInvulneravel;
    private bool _estaEmKnockback;
    private bool _deuPuloDuplo;
    public bool bloqueio_animacao;
    private bool _estaMorto;
    private bool _emHitstop;
    private bool _cameraInicializada;
    private bool _impactoRegistradoNesteAtaque;
    private int _ataqueId;
    private int _dashId;
    private int _knockbackId;
    private int _invulnerabilidadeId;
    private int _hitstopId;
    private bool _cenaEscorregando;
    private float _direcaoEscorregamento = 1.0f;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _dashCooldownTimer;
    private float _staminaRegenDelayTimer;
    private float _wallJumpLockTimer;
    private float _wallJumpDirection;
    private float _shakeTimer;
    private float _shakeIntensity;

    public override void _Ready()
    {
        if (!Mathf.IsEqualApprox(forca_do_pulo, LEGACY_JUMP_VELOCITY) && Mathf.IsEqualApprox(JumpVelocity, LEGACY_JUMP_VELOCITY))
            JumpVelocity = forca_do_pulo;
        else
            forca_do_pulo = JumpVelocity;

        if (!Mathf.IsEqualApprox(gasto_stamina_parede, LEGACY_WALL_STAMINA_DRAIN) && Mathf.IsEqualApprox(StaminaDrainRate, LEGACY_WALL_STAMINA_DRAIN))
            StaminaDrainRate = gasto_stamina_parede;
        else
            gasto_stamina_parede = StaminaDrainRate;

        MaxStamina = Mathf.Max(1.0f, MaxStamina);
        stamina = Mathf.Clamp(stamina, 0.0f, MaxStamina);
        FloorSnapLength = 2.0f;
        FloorStopOnSlope = true;
        SafeMargin = 0.05f;

        _anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _colisaoNormal = GetNode<CollisionShape2D>("ColisaoNormal");
        _colisaoDash = GetNode<CollisionShape2D>("ColisaoDash");
        _tetoCheck = GetNode<RayCast2D>("TetoCheck");
        _hitboxAtaque = GetNode<Area2D>("HitboxAtaque");
        _hitboxShape = GetNode<CollisionShape2D>("HitboxAtaque/CollisionShape2D");
        _groundParticles = GetNodeOrNull<CpuParticles2D>("GroundParticles");
        _dashParticles = GetNodeOrNull<CpuParticles2D>("DashParticles");
        _hitParticles = GetNodeOrNull<CpuParticles2D>("HitParticles");
        _movementDebugLabel = GetNodeOrNull<Label>("MovementDebugLayer/MovementDebug");
        _spriteBaseScale = _anim.Scale;

        _hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _colisaoDash.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        if (_movementDebugLabel is not null)
            _movementDebugLabel.Visible = DebugMovement;

        Range? healthNode = GetNodeOrNull<Range>("%healthbar");
        if (healthNode is not null)
        {
            healthNode.MaxValue = VIDA_MAXIMA;
            healthNode.Value = vida_atual;
        }

        CallDeferred(nameof(_capturar_camera_base));
        _atualizar_UI();
    }

    public override void _ExitTree()
    {
        if (_emHitstop)
            Engine.TimeScale = 1.0;
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        float delta = (float)deltaValue;
        bool estavaNoChao = IsOnFloor();
        float velocidadeVerticalAntesDoMovimento = Velocity.Y;
        _atualizar_camera(delta);
        if (_estaMorto)
        {
            Vector2 velocity = Velocity;
            if (!IsOnFloor())
            {
                velocity += GetGravity() * delta;
                velocity.Y = Mathf.Min(velocity.Y, TerminalVelocity);
            }
            else
                velocity.X = Mathf.MoveToward(velocity.X, 0.0f, GroundDeceleration * delta);
            Velocity = velocity;
            MoveAndSlide();
            _atualizar_debug();
            return;
        }

        if (_cenaEscorregando)
        {
            _aplicar_gravidade(delta);
            Vector2 velocity = Velocity;
            velocity.X = _direcaoEscorregamento * velocidade_escorregamento;
            velocity.Y += 150.0f;
            Velocity = velocity;
            _anim.Offset = Vector2.Zero;
            if (_direcaoEscorregamento < 0.0f)
                _tocar_animacao_direta("personagem_escorregando_esq", "personagem_escorregando");
            else
                _tocar_animacao_direta("personagem_escorregando", "personagem_escorregando");
            MoveAndSlide();
            _atualizar_debug();
            return;
        }

        _atualizar_timers(delta);
        _recuperar_stamina(delta);
        _processar_stamina_parede(delta);
        _processar_inputs();

        if (_isDashing)
        {
            _processar_dash_fisica();
            _hitboxAtaque.Scale = new Vector2(facing_direction, _hitboxAtaque.Scale.Y);
            _detectar_aterrissagem(estavaNoChao, velocidadeVerticalAntesDoMovimento);
            _atualizar_debug();
            return;
        }

        _aplicar_gravidade(delta);
        _processar_movimento(delta);
        _hitboxAtaque.Scale = new Vector2(facing_direction, _hitboxAtaque.Scale.Y);
        MoveAndSlide();
        _detectar_aterrissagem(estavaNoChao, velocidadeVerticalAntesDoMovimento);
        _atualizar_debug();
    }

    public void forcar_escorregamento(float direcao_da_ladeira)
    {
        if (_isDashing)
            finalizar_dash();
        if (_estaAtacando)
            _cancelar_ataque();

        _cenaEscorregando = true;
        _direcaoEscorregamento = direcao_da_ladeira;
        _hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
    }

    private string _animacao_para_direcao(string nomeBase)
    {
        if (facing_direction < 0)
        {
            string nomeEsquerda = nomeBase + "_esq";
            if (_anim.SpriteFrames?.HasAnimation(nomeEsquerda) == true)
                return nomeEsquerda;
        }
        return nomeBase;
    }

    private void _tocar_animacao(string nomeBase)
    {
        _anim.FlipH = false;
        _anim.Play(_animacao_para_direcao(nomeBase));
    }

    private void _tocar_animacao_direta(string nomeDesejado, string fallback)
    {
        _anim.FlipH = false;
        _anim.Play(_anim.SpriteFrames?.HasAnimation(nomeDesejado) == true ? nomeDesejado : fallback);
    }

    private void _forcar_animacao_pos_dash()
    {
        if (_estaMorto)
            return;

        if (IsOnFloor())
        {
            float direction = Input.GetAxis("left", "right");
            if (direction != 0.0f)
                _tocar_animacao("walk");
            else if (Input.IsActionPressed("up"))
                _tocar_animacao_direta("look_up", "look_up");
            else
                _tocar_animacao("idle");
        }
        else
        {
            _tocar_animacao(Velocity.Y > 15.0f ? "fall" : "jump");
        }
    }

    private void _atualizar_timers(float delta)
    {
        if (IsOnFloor())
        {
            _coyoteTimer = CoyoteTime;
            _deuPuloDuplo = false;
        }
        else
        {
            _coyoteTimer = Mathf.Max(0.0f, _coyoteTimer - delta);
        }

        if (Input.IsActionJustPressed("jump"))
            _jumpBufferTimer = JumpBufferTime;
        else
            _jumpBufferTimer = Mathf.Max(0.0f, _jumpBufferTimer - delta);

        _dashCooldownTimer = Mathf.Max(0.0f, _dashCooldownTimer - delta);
        _staminaRegenDelayTimer = Mathf.Max(0.0f, _staminaRegenDelayTimer - delta);
        _wallJumpLockTimer = Mathf.Max(0.0f, _wallJumpLockTimer - delta);
    }

    private void _processar_stamina_parede(float delta)
    {
        if (_isDashing || _estaEmKnockback || _estaMorto)
            return;

        float direction = Input.GetAxis("left", "right");
        if (_esta_agarrando_parede(direction))
            _consumir_stamina(StaminaDrainRate * delta);

        stamina = Mathf.Clamp(stamina, 0.0f, MaxStamina);
        _atualizar_UI();
    }

    private bool _esta_agarrando_parede(float direction)
    {
        if (!IsOnWallOnly() || direction == 0.0f || stamina <= 0.0f)
            return false;

        float normalParede = GetWallNormal().X;
        return normalParede != 0.0f && Mathf.Sign(direction) == -Mathf.Sign(normalParede);
    }

    private void _aplicar_gravidade(float delta)
    {
        if (IsOnFloor())
            return;

        float direcao = Input.GetAxis("left", "right");
        Vector2 velocity = Velocity;
        if (_esta_agarrando_parede(direcao) && !_estaEmKnockback)
        {
            if (Input.IsActionPressed("up"))
                velocity.Y = -velocidade_escalada_parede;
            else if (Input.IsActionPressed("down"))
                velocity.Y = velocidade_deslize_parede;
            else
                velocity.Y = 0.0f;
        }
        else
        {
            Vector2 gravity = GetGravity();
            float gravityMultiplier = velocity.Y > 0.0f ? FallGravity : 1.0f;
            velocity += gravity * gravityMultiplier * delta;
            velocity.Y = Mathf.Min(velocity.Y, TerminalVelocity);
        }
        Velocity = velocity;
    }

    private void _processar_inputs()
    {
        if (_estaEmKnockback || _estaMorto)
            return;

        if (!_isDashing && Input.IsActionJustReleased("jump") && Velocity.Y < 0.0f)
            Velocity = new Vector2(Velocity.X, Velocity.Y * Mathf.Clamp(JumpCutMultiplier, 0.1f, 1.0f));

        if (!_isDashing)
        {
            if (pode_pular && _jumpBufferTimer > 0.0f)
            {
                if (_coyoteTimer > 0.0f)
                    _executar_pulo();
                else if (IsOnWallOnly() && stamina > 0.0f)
                    _executar_wall_jump();
                else if (pode_pulo_duplo && !_deuPuloDuplo)
                    _executar_pulo_duplo();
            }
            if (pode_dash && Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0.0f && stamina >= DashCost)
                executar_dash();
        }

        if (!_isDashing && pode_atacar && Input.IsActionJustPressed("attack") && !_estaAtacando && !IsOnWallOnly())
            executar_ataque();
    }

    private void _executar_pulo()
    {
        Velocity = new Vector2(Velocity.X, JumpVelocity);
        _jumpBufferTimer = 0.0f;
        _coyoteTimer = 0.0f;
        _emitir_particulas_chao();
        _aplicar_squash(new Vector2(0.92f, 1.08f), 0.12f);
    }

    private void _executar_wall_jump()
    {
        Vector2 wallNormal = GetWallNormal();
        Velocity = new Vector2(wallNormal.X * WallJumpPush, JumpVelocity);
        facing_direction = Mathf.Sign(wallNormal.X);
        _wallJumpDirection = Mathf.Sign(wallNormal.X);
        _wallJumpLockTimer = WallJumpLockTime;
        _jumpBufferTimer = 0.0f;
        _deuPuloDuplo = false;
        _emitir_particulas_parede(wallNormal);
        _aplicar_squash(new Vector2(0.92f, 1.08f), 0.12f);
    }

    private void _executar_pulo_duplo()
    {
        Velocity = new Vector2(Velocity.X, JumpVelocity);
        _jumpBufferTimer = 0.0f;
        _deuPuloDuplo = true;
        _anim.Stop();
        _tocar_animacao("jump");
        _aplicar_squash(new Vector2(0.90f, 1.10f), 0.12f);
    }

    private void _processar_movimento(float delta)
    {
        if (_estaEmKnockback || _isDashing)
            return;

        float inputDirection = Input.GetAxis("left", "right");
        float direction = _wallJumpLockTimer > 0.0f ? _wallJumpDirection : inputDirection;
        float speedMultiplier = _estaAtacando ? 0.5f : 1.0f;
        float targetSpeed = direction * MaxSpeed * speedMultiplier;
        Vector2 velocity = Velocity;
        _anim.Offset = Vector2.Zero;

        if (direction != 0.0f)
        {
            if (_wallJumpLockTimer > 0.0f)
            {
                velocity.X = Mathf.MoveToward(velocity.X, targetSpeed, AirDeceleration * delta);
            }
            else
            {
                bool reversing = velocity.X != 0.0f && Mathf.Sign(velocity.X) != Mathf.Sign(direction);
                float acceleration = reversing
                    ? ReversalAcceleration
                    : IsOnFloor() ? GroundAcceleration : AirAcceleration;
                velocity.X = Mathf.MoveToward(velocity.X, targetSpeed, acceleration * delta);
            }

            facing_direction = Mathf.Sign(direction);
            if (IsOnFloor() && !_estaAtacando && !bloqueio_animacao)
                _tocar_animacao("walk");
        }
        else
        {
            float deceleration = IsOnFloor() ? GroundDeceleration : AirDeceleration;
            velocity.X = Mathf.MoveToward(velocity.X, 0.0f, deceleration * delta);
            if (IsOnFloor() && !_estaAtacando && !bloqueio_animacao)
            {
                if (Input.IsActionPressed("up"))
                    _tocar_animacao_direta("look_up", "look_up");
                else
                    _tocar_animacao("idle");
            }
        }
        Velocity = velocity;

        if (!IsOnFloor() && !_estaAtacando && !bloqueio_animacao)
        {
            if (_esta_agarrando_parede(inputDirection))
            {
                float wallNormal = GetWallNormal().X;
                if (Mathf.Sign(inputDirection) == -Mathf.Sign(wallNormal))
                {
                    bool vertical = Input.IsActionPressed("up") || Input.IsActionPressed("down");
                    if (wallNormal > 0.0f)
                    {
                        _anim.Offset = new Vector2(offset_visual_parede_x, offset_visual_parede_y);
                        _tocar_animacao_direta(vertical ? "personagem_parede_esq" : "personagem_parede_idle_esq", vertical ? "personagem_parede" : "personagem_parede_idle");
                    }
                    else if (wallNormal < 0.0f)
                    {
                        _anim.Offset = new Vector2(-offset_visual_parede_x, offset_visual_parede_y);
                        _tocar_animacao_direta(vertical ? "personagem_parede" : "personagem_parede_idle", "personagem_parede");
                    }
                }
                else
                    _tocar_animacao(Velocity.Y > 15.0f ? "fall" : "jump");
            }
            else
                _tocar_animacao(Velocity.Y > 15.0f ? "fall" : "jump");
        }
    }

    public async void executar_dash()
    {
        if (_isDashing || _estaAtacando || _estaEmKnockback || _estaMorto || !pode_dash)
            return;
        if (_dashCooldownTimer > 0.0f || stamina < DashCost)
            return;

        _isDashing = true;
        int meuDashId = ++_dashId;
        _dashCooldownTimer = DashCooldown;
        _consumir_stamina(DashCost);
        if (IsOnWallOnly())
            _dashDirection = new Vector2(GetWallNormal().X, 0.0f).Normalized();
        else
        {
            Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
            _dashDirection = inputDir != Vector2.Zero ? inputDir.Normalized() : new Vector2(facing_direction, 0.0f);
        }

        if (_dashDirection.X != 0.0f)
            facing_direction = Mathf.Sign(_dashDirection.X);
        _anim.Offset = Vector2.Zero;
        _anim.Modulate = new Color(0.65f, 0.82f, 1.0f, 1.0f);
        if (!_estaAtacando)
            _tocar_animacao("dash");

        _colisaoNormal.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _colisaoDash.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _emitir_particulas_dash();
        criar_fantasma();
        GetNodeOrNull<Timer>("TimerRastro")?.Start();

        await ToSignal(GetTree().CreateTimer(DashDuration), SceneTreeTimer.SignalName.Timeout);
        if (meuDashId != _dashId)
            return;
        float tempoEsperandoTeto = 0.0f;
        while (_tetoCheck.IsColliding() && tempoEsperandoTeto < 0.20f)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            tempoEsperandoTeto += (float)GetPhysicsProcessDeltaTime();
            if (meuDashId != _dashId || !_isDashing)
                return;
        }
        if (meuDashId == _dashId)
            finalizar_dash();
    }

    private void _processar_dash_fisica()
    {
        Velocity = _dashDirection * DashSpeed;
        MoveAndSlide();
    }

    public void finalizar_dash()
    {
        if (!_isDashing)
            return;

        _isDashing = false;
        GetNodeOrNull<Timer>("TimerRastro")?.Stop();
        _colisaoNormal.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _colisaoDash.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        float direction = Input.GetAxis("left", "right");
        Vector2 velocity = Velocity;
        velocity.X = direction * MaxSpeed;
        if (velocity.Y < 0.0f)
            velocity.Y = 0.0f;
        Velocity = velocity;
        if (!_estaInvulneravel)
            _anim.Modulate = Colors.White;
        if (!_estaAtacando && !_estaMorto)
            _forcar_animacao_pos_dash();
    }

    public async void executar_ataque()
    {
        if (_estaAtacando || _isDashing || _estaEmKnockback || _estaMorto || IsOnWallOnly() || !pode_atacar)
            return;

        _estaAtacando = true;
        int meuAtaqueId = ++_ataqueId;
        _alvosAtingidosNoAtaque.Clear();
        _impactoRegistradoNesteAtaque = false;
        string animacaoEscolhida = "attack";
        Vector2 posicaoHitbox;
        bool segurandoLado = Input.IsActionPressed("left") || Input.IsActionPressed("right");
        if (Input.IsActionPressed("left"))
            facing_direction = -1;
        else if (Input.IsActionPressed("right"))
            facing_direction = 1;

        if (!IsOnFloor() && Input.IsActionPressed("down"))
        {
            animacaoEscolhida = "attack_down";
            posicaoHitbox = new Vector2(0.0f, 7.0f);
        }
        else if (Input.IsActionPressed("up"))
        {
            if (segurandoLado)
            {
                animacaoEscolhida = "attack_diag";
                posicaoHitbox = new Vector2(0.2f * facing_direction, -7.0f);
            }
            else
            {
                animacaoEscolhida = "attack_up";
                posicaoHitbox = new Vector2(0.0f, -7.0f);
            }
        }
        else
        {
            posicaoHitbox = new Vector2(facing_direction, 0.0f);
        }

        _anim.Offset = Vector2.Zero;
        _tocar_animacao(animacaoEscolhida);
        _hitboxAtaque.Position = posicaoHitbox;
        _hitboxAtaque.Scale = new Vector2(facing_direction, Mathf.Abs(_hitboxAtaque.Scale.Y));

        if (!await _aguardar_ataque(AttackWindup, meuAtaqueId))
            return;

        _hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        float tempoAtivo = Mathf.Max(AttackActiveTime, (float)GetPhysicsProcessDeltaTime());
        while (tempoAtivo > 0.0f && meuAtaqueId == _ataqueId && _estaAtacando)
        {
            _detectar_colisao_ataque(animacaoEscolhida);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            tempoAtivo -= (float)GetPhysicsProcessDeltaTime();
        }

        if (meuAtaqueId != _ataqueId || !_estaAtacando)
            return;
        _hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        if (!await _aguardar_ataque(AttackRecovery, meuAtaqueId))
            return;
        _finalizar_ataque();
    }

    private async System.Threading.Tasks.Task<bool> _aguardar_ataque(float duracao, int ataqueId)
    {
        if (duracao > 0.0f)
            await ToSignal(GetTree().CreateTimer(duracao), SceneTreeTimer.SignalName.Timeout);
        return ataqueId == _ataqueId && _estaAtacando && !_estaMorto;
    }

    private void _finalizar_ataque()
    {
        _estaAtacando = false;
        _hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _hitboxAtaque.Position = Vector2.Zero;
        _alvosAtingidosNoAtaque.Clear();
        if (!_isDashing && !_estaMorto)
            _forcar_animacao_pos_dash();
    }

    private void _detectar_colisao_ataque(string animacaoEscolhida)
    {
        foreach (Node2D body in _hitboxAtaque.GetOverlappingBodies())
            _tentar_acertar_alvo(body, animacaoEscolhida);
        foreach (Area2D area in _hitboxAtaque.GetOverlappingAreas())
            _tentar_acertar_alvo(area, animacaoEscolhida);
    }

    private void _tentar_acertar_alvo(Node2D alvo, string animacaoEscolhida)
    {
        if (alvo == this || !alvo.HasMethod("tomar_dano") || !_alvosAtingidosNoAtaque.Add(alvo.GetInstanceId()))
            return;

        alvo.Call("tomar_dano", DANO_ATAQUE, this);
        if (alvo.HasMethod("aplicar_impacto"))
            alvo.Call("aplicar_impacto", GlobalPosition, AttackKnockback);

        if (!_impactoRegistradoNesteAtaque)
        {
            _impactoRegistradoNesteAtaque = true;
            _emitir_particulas_impacto(alvo.GlobalPosition);
            aplicar_hitstop_e_shake();
            if (animacaoEscolhida == "attack_down")
                Velocity = new Vector2(Velocity.X, FORCA_POGO);
        }
    }

    private void _cancelar_ataque()
    {
        if (!_estaAtacando)
            return;
        _ataqueId++;
        _finalizar_ataque();
    }

    public async void aplicar_hitstop_e_shake()
    {
        adicionar_screen_shake(1.6f, 0.10f);
        if (_emHitstop)
            return;

        _emHitstop = true;
        int meuHitstopId = ++_hitstopId;
        Engine.TimeScale = 0.08;
        await ToSignal(GetTree().CreateTimer(0.035, true, false, true), SceneTreeTimer.SignalName.Timeout);
        if (meuHitstopId == _hitstopId)
        {
            Engine.TimeScale = 1.0;
            _emHitstop = false;
        }
    }

    public void tomar_dano(int quantidade, Node2D? origem = null)
    {
        if (quantidade <= 0 || _estaInvulneravel || _estaMorto)
            return;

        _estaInvulneravel = true;
        if (_isDashing)
            finalizar_dash();
        if (_estaAtacando)
            _cancelar_ataque();

        vida_atual = Mathf.Max(0, vida_atual - quantidade);
        _atualizar_UI();
        adicionar_screen_shake(2.4f, 0.14f);
        if (vida_atual <= 0)
        {
            _morrer();
            return;
        }

        aplicar_knockback(origem?.GlobalPosition ?? GlobalPosition - new Vector2(facing_direction, 0.0f));
        _iniciar_invulnerabilidade();
    }

    private async void _morrer()
    {
        if (_estaMorto)
            return;
        _estaMorto = true;
        _estaInvulneravel = true;
        _estaEmKnockback = false;
        _isDashing = false;
        _estaAtacando = false;
        _dashId++;
        _ataqueId++;
        _knockbackId++;
        _invulnerabilidadeId++;
        _hitstopId++;
        Engine.TimeScale = 1.0;
        _emHitstop = false;
        GetNodeOrNull<Timer>("TimerRastro")?.Stop();
        _colisaoNormal.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _colisaoDash.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        Velocity = new Vector2(-facing_direction * 50.0f, -250.0f);
        _anim.Offset = Vector2.Zero;
        _anim.Modulate = Colors.White;
        _anim.Stop();
        _tocar_animacao("personagem_morte");
        adicionar_screen_shake(3.0f, 0.22f);
        await ToSignal(_anim, AnimatedSprite2D.SignalName.AnimationFinished);
        if (IsInsideTree())
        {
            GetNodeOrNull<Global>("/root/Global")?.PrepareRespawn();
            GetTree().ReloadCurrentScene();
        }
    }

    public void RestAtBench()
    {
        if (_estaMorto) return;
        vida_atual = VIDA_MAXIMA;
        stamina = MaxStamina;
        _atualizar_UI();
        Global? world = GetNodeOrNull<Global>("/root/Global");
        if (world is not null) world.RestAt(world.sala_atual, GlobalPosition);
    }

    public async void aplicar_knockback(Vector2 posicao_origem)
    {
        _estaEmKnockback = true;
        int meuKnockbackId = ++_knockbackId;
        int direcao = GlobalPosition.X >= posicao_origem.X ? 1 : -1;
        Velocity = new Vector2(direcao * KnockbackHorizontal, KnockbackVertical);
        _anim.Offset = Vector2.Zero;
        _tocar_animacao("jump");
        await ToSignal(GetTree().CreateTimer(Mathf.Max(0.01f, KnockbackDuration)), SceneTreeTimer.SignalName.Timeout);
        if (meuKnockbackId == _knockbackId && !_estaMorto)
            _estaEmKnockback = false;
    }

    private void _recuperar_stamina(float delta)
    {
        if (stamina < MaxStamina && IsOnFloor() && !_isDashing && !_estaEmKnockback && _staminaRegenDelayTimer <= 0.0f)
        {
            float staminaAnterior = stamina;
            stamina = Mathf.MoveToward(stamina, MaxStamina, StaminaRegenRate * delta);
            if (staminaAnterior < MaxStamina && Mathf.IsEqualApprox(stamina, MaxStamina))
                _piscar_dash_pronto();
        }
        stamina = Mathf.Clamp(stamina, 0.0f, MaxStamina);
        _atualizar_UI();
    }

    private void _consumir_stamina(float quantidade)
    {
        if (quantidade <= 0.0f)
            return;
        stamina = Mathf.Clamp(stamina - quantidade, 0.0f, MaxStamina);
        _staminaRegenDelayTimer = Mathf.Max(_staminaRegenDelayTimer, StaminaRegenDelay);
        _atualizar_UI();
    }

    private void _piscar_dash_pronto()
    {
        if (_estaInvulneravel || _estaMorto || _isDashing)
            return;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(_anim, "modulate", new Color(1.7f, 1.9f, 2.2f, 1.0f), 0.05);
        tween.TweenProperty(_anim, "modulate", Colors.White, 0.16);
    }

    private void _atualizar_UI()
    {
        Range? healthNode = GetNodeOrNull<Range>("%healthbar");
        if (healthNode is not null)
            healthNode.Value = vida_atual;
        Range? staminaNode = GetNodeOrNull<Range>("%staminabar");
        if (staminaNode is not null)
        {
            staminaNode.MaxValue = MaxStamina;
            staminaNode.Value = stamina;
        }
    }

    private async void _iniciar_invulnerabilidade()
    {
        int meuId = ++_invulnerabilidadeId;
        float restante = Mathf.Max(0.01f, InvulnerabilityDuration);
        bool vermelho = true;
        while (restante > 0.0f && meuId == _invulnerabilidadeId && !_estaMorto)
        {
            _anim.Modulate = vermelho ? new Color(1.0f, 0.35f, 0.35f, 1.0f) : new Color(1.0f, 1.0f, 1.0f, 0.35f);
            vermelho = !vermelho;
            float passo = Mathf.Min(0.07f, restante);
            await ToSignal(GetTree().CreateTimer(passo), SceneTreeTimer.SignalName.Timeout);
            restante -= passo;
        }
        if (meuId == _invulnerabilidadeId && !_estaMorto)
        {
            _anim.Modulate = Colors.White;
            _estaInvulneravel = false;
        }
    }

    private void _detectar_aterrissagem(bool estavaNoChao, float velocidadeVerticalAnterior)
    {
        if (estavaNoChao || !IsOnFloor() || velocidadeVerticalAnterior < 50.0f)
            return;
        _emitir_particulas_chao();
        _aplicar_squash(new Vector2(1.10f, 0.90f), 0.14f);
        if (velocidadeVerticalAnterior > 260.0f)
            adicionar_screen_shake(0.8f, 0.08f);
    }

    private void _aplicar_squash(Vector2 multiplicador, float duracao)
    {
        if (_estaMorto)
            return;
        _squashTween?.Kill();
        _anim.Scale = new Vector2(_spriteBaseScale.X * multiplicador.X, _spriteBaseScale.Y * multiplicador.Y);
        _squashTween = GetTree().CreateTween();
        _squashTween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _squashTween.TweenProperty(_anim, "scale", _spriteBaseScale, duracao);
    }

    private void _emitir_particulas_chao()
    {
        if (_groundParticles is null)
            return;
        _groundParticles.Position = new Vector2(0.0f, 2.0f);
        _groundParticles.Direction = Vector2.Up;
        _groundParticles.Restart();
    }

    private void _emitir_particulas_parede(Vector2 normalParede)
    {
        if (_groundParticles is null)
            return;
        _groundParticles.Position = new Vector2(-normalParede.X * 4.0f, -3.0f);
        _groundParticles.Direction = normalParede;
        _groundParticles.Restart();
    }

    private void _emitir_particulas_dash()
    {
        if (_dashParticles is null)
            return;
        _dashParticles.Position = -_dashDirection * 5.0f + new Vector2(0.0f, -4.0f);
        _dashParticles.Direction = -_dashDirection;
        _dashParticles.Restart();
    }

    private void _emitir_particulas_impacto(Vector2 posicaoGlobal)
    {
        if (_hitParticles is null)
            return;
        _hitParticles.GlobalPosition = posicaoGlobal;
        _hitParticles.Direction = (posicaoGlobal - GlobalPosition).Normalized();
        _hitParticles.Restart();
    }

    private void _capturar_camera_base()
    {
        _camera = GetViewport().GetCamera2D() ?? GetNodeOrNull<Camera2D>("Camera2D");
        if (_camera is null)
            return;
        _cameraBaseOffset = _camera.Offset;
        _cameraLookOffset = Vector2.Zero;
        _cameraInicializada = true;
    }

    private void _atualizar_camera(float delta)
    {
        Camera2D? cameraAtiva = GetViewport().GetCamera2D();
        if (cameraAtiva is not null && cameraAtiva != _camera)
        {
            _camera = cameraAtiva;
            _cameraBaseOffset = _camera.Offset;
            _cameraLookOffset = Vector2.Zero;
            _cameraInicializada = true;
        }
        if (!_cameraInicializada || _camera is null)
            return;

        float targetX = Mathf.Abs(Velocity.X) > 8.0f ? Mathf.Sign(Velocity.X) * CameraLookAhead : 0.0f;
        float targetY = Mathf.Clamp(Velocity.Y / Mathf.Max(1.0f, TerminalVelocity), -1.0f, 1.0f) * CameraVerticalLookAhead;
        float peso = 1.0f - Mathf.Exp(-Mathf.Max(0.1f, CameraLookAheadSpeed) * delta);
        _cameraLookOffset = _cameraLookOffset.Lerp(new Vector2(targetX, targetY), peso);

        Vector2 shake = Vector2.Zero;
        if (_shakeTimer > 0.0f)
        {
            _shakeTimer = Mathf.Max(0.0f, _shakeTimer - delta);
            float intensidadeAtual = _shakeIntensity * Mathf.Clamp(_shakeTimer / 0.10f, 0.0f, 1.0f);
            shake = new Vector2(
                (float)GD.RandRange(-intensidadeAtual, intensidadeAtual),
                (float)GD.RandRange(-intensidadeAtual, intensidadeAtual));
            if (_shakeTimer <= 0.0f)
                _shakeIntensity = 0.0f;
        }
        _camera.Offset = _cameraBaseOffset + _cameraLookOffset + shake;
    }

    public void adicionar_screen_shake(float intensidade = 2.0f, float duracao = 0.12f)
    {
        _shakeIntensity = Mathf.Max(_shakeIntensity, Mathf.Max(0.0f, intensidade));
        _shakeTimer = Mathf.Max(_shakeTimer, Mathf.Max(0.0f, duracao));
    }

    private PlayerState _obter_estado_atual()
    {
        if (_estaMorto) return PlayerState.Dead;
        if (_cenaEscorregando) return PlayerState.Sliding;
        if (_estaEmKnockback) return PlayerState.Hurt;
        if (_isDashing) return PlayerState.Dashing;
        if (_estaAtacando) return PlayerState.Attacking;

        float direction = Input.GetAxis("left", "right");
        if (_esta_agarrando_parede(direction))
            return Input.IsActionPressed("up") || Input.IsActionPressed("down") ? PlayerState.Climbing : PlayerState.WallGrip;
        return IsOnFloor() ? PlayerState.Grounded : PlayerState.Airborne;
    }

    private void _atualizar_debug()
    {
        if (_movementDebugLabel is null)
            return;
        _movementDebugLabel.Visible = DebugMovement;
        if (!DebugMovement)
            return;

        _movementDebugLabel.Text =
            $"Velocity: ({Velocity.X:0.0}, {Velocity.Y:0.0})\n" +
            $"Grounded: {IsOnFloor()}\n" +
            $"CurrentState: {_obter_estado_atual()}\n" +
            $"Stamina: {stamina:0.0}/{MaxStamina:0.0}\n" +
            $"CoyoteTimer: {_coyoteTimer:0.000}\n" +
            $"JumpBufferTimer: {_jumpBufferTimer:0.000}\n" +
            $"DashCooldown: {_dashCooldownTimer:0.000}\n" +
            $"Invulnerable: {_estaInvulneravel}";
    }

    public void criar_fantasma()
    {
        if (_anim.SpriteFrames is null)
            return;
        Sprite2D fantasma = new()
        {
            Texture = _anim.SpriteFrames.GetFrameTexture(_anim.Animation, _anim.Frame),
            GlobalPosition = _anim.GlobalPosition,
            GlobalRotation = _anim.GlobalRotation,
            FlipH = false,
            Scale = _anim.Scale,
            ZIndex = _anim.ZIndex - 1,
            Modulate = new Color(0.25f, 0.55f, 1.0f, 0.62f)
        };
        GetParent().AddChild(fantasma);
        Tween tween = GetTree().CreateTween();
        tween.SetParallel();
        tween.TweenProperty(fantasma, "modulate:a", 0.0f, 0.22);
        tween.TweenProperty(fantasma, "scale", fantasma.Scale * 0.92f, 0.22);
        tween.Finished += fantasma.QueueFree;
    }

    public void _on_animacao_terminou()
    {
        // Mantido para compatibilidade; o ataque usa janelas de tempo explícitas.
    }

    public void _on_timer_rastro_timeout()
    {
        if (_isDashing)
            criar_fantasma();
    }
}
