using Godot;

public partial class Caveira : CharacterBody2D
{
	[ExportGroup("Status do Inimigo")]
	[Export] public float velocidade_patrulha = 25.0f;
	[Export] public float velocidade_perseguicao = 45.0f;
	[Export] public float velocidade_ataque = 1.0f;
	[Export] public float distancia_patrulha = 80.0f;
	[Export] public float distancia_visao = 180.0f;
	[Export] public float distancia_ataque = 30.0f;
	[Export] public float tempo_vulneravel = 5.0f;
	[Export] public int dano_ataque = 1;
	[Export] public int vida_maxima = 3;

	private readonly float _gravidade = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	private int _direcao = -1;
	private string _estado = "PATRULHA";
	private int _vida;
	private Player? _player;
	private float _posicaoBocaX;
	private float _timerVulneravel;
	private float _posicaoInicialX;
	private AnimatedSprite2D _anim = null!;
	private Area2D _hitboxAtaque = null!;
	private CollisionShape2D _hitboxShape = null!;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_hitboxAtaque = GetNode<Area2D>("HitboxAtaque");
		_hitboxShape = GetNode<CollisionShape2D>("HitboxAtaque/CollisionShape2D");
		_hitboxAtaque.BodyEntered += _on_hitbox_entered;
		_hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_vida = vida_maxima;
		_posicaoBocaX = Mathf.Abs(_hitboxAtaque.Position.X);
		_posicaoInicialX = GlobalPosition.X;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		if (!IsOnFloor())
			velocity.Y += _gravidade * (float)delta;
		Velocity = velocity;

		if (_estado == "MORTO")
		{
			MoveAndSlide();
			return;
		}

		_player ??= GetTree().Root.FindChild("bury", true, false) as Player;
		float distanciaPlayer = _player is null ? 9999.0f : GlobalPosition.DistanceTo(_player.GlobalPosition);

		switch (_estado)
		{
			case "PATRULHA":
				if (distanciaPlayer < distancia_visao)
					_estado = "PERSEGUICAO";
				else
					patrulhar();
				break;
			case "PERSEGUICAO":
				if (distanciaPlayer > distancia_visao + 50.0f)
				{
					_estado = "PATRULHA";
					_posicaoInicialX = GlobalPosition.X;
				}
				else if (distanciaPlayer <= distancia_ataque)
					iniciar_ataque();
				else
					perseguir();
				break;
			case "ATAQUE":
				Velocity = new Vector2(0.0f, Velocity.Y);
				if (_player is not null)
				{
					_direcao = _player.GlobalPosition.X < GlobalPosition.X ? -1 : 1;
					atualizar_direcao(_direcao);
				}
				break;
			case "VULNERAVEL":
				Velocity = new Vector2(0.0f, Velocity.Y);
				_timerVulneravel -= (float)delta;
				if (_timerVulneravel <= 0.0f)
					_estado = "PERSEGUICAO";
				break;
		}

		MoveAndSlide();
	}

	public void patrulhar()
	{
		Velocity = new Vector2(_direcao * velocidade_patrulha, Velocity.Y);
		_anim.Play("walk");
		atualizar_direcao(_direcao);
		if (IsOnWall() || Mathf.Abs(GlobalPosition.X - _posicaoInicialX) > distancia_patrulha)
		{
			_direcao *= -1;
			GlobalPosition += new Vector2(_direcao * 2.0f, 0.0f);
		}
	}

	public void perseguir()
	{
		if (_player is not null)
			_direcao = _player.GlobalPosition.X < GlobalPosition.X ? -1 : 1;
		Velocity = new Vector2(_direcao * velocidade_perseguicao, Velocity.Y);
		_anim.Play("walk");
		atualizar_direcao(_direcao);
	}

	public async void iniciar_ataque()
	{
		if (_estado == "ATAQUE")
			return;

		_estado = "ATAQUE";
		_anim.Play("attack", velocidade_ataque);
		_hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		await ToSignal(_anim, AnimatedSprite2D.SignalName.AnimationFinished);

		_hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_estado = "VULNERAVEL";
		_timerVulneravel = tempo_vulneravel;
		if (_anim.SpriteFrames.HasAnimation("idle"))
			_anim.Play("idle");
		else
		{
			_anim.Play("walk");
			_anim.Pause();
		}
	}

	public void atualizar_direcao(int dir)
	{
		_anim.FlipH = dir > 0;
		_hitboxAtaque.Position = new Vector2(dir > 0 ? _posicaoBocaX : -_posicaoBocaX, _hitboxAtaque.Position.Y);
	}

	private void _on_hitbox_entered(Node2D body)
	{
		if (body.Name == "bury" && _estado == "ATAQUE" && body is Player player)
			player.tomar_dano(dano_ataque, this);
	}

	public async void tomar_dano(int quantidade, Node2D? origem = null)
	{
		if (_estado == "MORTO")
			return;

		_vida -= quantidade;
		_anim.Modulate = new Color(1.0f, 0.3f, 0.3f);
		await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
		_anim.Modulate = Colors.White;
		if (_vida <= 0)
			morrer();
	}

	public void aplicar_impacto(Vector2 origem, float forca)
	{
		if (_estado == "MORTO")
			return;
		float direcao = GlobalPosition.X >= origem.X ? 1.0f : -1.0f;
		Velocity = new Vector2(direcao * forca, Mathf.Min(Velocity.Y, -35.0f));
		GlobalPosition += new Vector2(direcao * 2.0f, 0.0f);
	}

	public async void morrer()
	{
		_estado = "MORTO";
		Velocity = new Vector2(0.0f, Velocity.Y);
		GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_hitboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(this, "modulate:a", 0.0f, 0.5);
		await ToSignal(tween, Tween.SignalName.Finished);
		QueueFree();
	}
}
