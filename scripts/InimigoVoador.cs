using Godot;

public partial class InimigoVoador : Area2D
{
    [Export] public float velocidade_patrulha = 40.0f;
    [Export] public float velocidade_ataque = 700.0f;
    [Export] public float velocidade_retorno = 150.0f;
    [Export] public float distancia_patrulha = 80.0f;
    [Export] public float tempo_vulneravel = 1.0f;
    [Export] public float cooldown_ataque = 2.0f;
    [Export] public float limite_do_chao_y = 152.0f;

    private float _posicaoInicialX;
    private float _alturaPatrulhaOriginal;
    private bool _indoDireita = true;
    private bool _podeAtacar = true;
    private string _estado = "patrulhando";
    private Vector2 _pontoDeRetorno;
    private int _vida = 3;
    private AnimatedSprite2D _anim = null!;
    private AnimatedSprite2D _spriteMachucado = null!;
    private Area2D _areaDeteccao = null!;

    public override void _Ready()
    {
        _anim = GetNode<AnimatedSprite2D>("InimigoAtaque");
        _spriteMachucado = GetNode<AnimatedSprite2D>("InimigoMachucado");
        _areaDeteccao = GetNode<Area2D>("AreaDeteccao");
        _posicaoInicialX = GlobalPosition.X;
        _alturaPatrulhaOriginal = GlobalPosition.Y;
        _anim.Play("fly");
        _spriteMachucado.Visible = false;
        _spriteMachucado.Modulate = Colors.White;
        RotationDegrees = 0.0f;
    }

    public override void _Process(double delta)
    {
        if (_estado == "morto")
            return;

        switch (_estado)
        {
            case "patrulhando": _logica_patrulha((float)delta); break;
            case "atacando": _logica_ataque((float)delta); break;
            case "retornando": _logica_retorno((float)delta); break;
        }
    }

    private void _logica_patrulha(float delta)
    {
        RotationDegrees = 0.0f;
        GlobalPosition = new Vector2(GlobalPosition.X, _alturaPatrulhaOriginal);
        if (_podeAtacar)
        {
            foreach (Node2D body in _areaDeteccao.GetOverlappingBodies())
            {
                if (body.Name == "bury")
                {
                    iniciar_ataque();
                    break;
                }
            }
        }

        float direcao = _indoDireita ? 1.0f : -1.0f;
        GlobalPosition += new Vector2(velocidade_patrulha * direcao * delta, 0.0f);
        _anim.FlipH = !_indoDireita;
        _spriteMachucado.FlipH = !_indoDireita;
        if (_indoDireita && GlobalPosition.X >= _posicaoInicialX + distancia_patrulha)
            _indoDireita = false;
        else if (!_indoDireita && GlobalPosition.X <= _posicaoInicialX - distancia_patrulha)
            _indoDireita = true;
    }

    private void _logica_ataque(float delta)
    {
        GlobalPosition += new Vector2(0.0f, velocidade_ataque * delta);
        if (GlobalPosition.Y >= limite_do_chao_y)
        {
            GlobalPosition = new Vector2(GlobalPosition.X, limite_do_chao_y);
            ficar_no_chao();
        }
    }

    private void _logica_retorno(float delta)
    {
        RotationDegrees = 0.0f;
        Vector2 direcao = GlobalPosition.DirectionTo(_pontoDeRetorno);
        GlobalPosition += direcao * velocidade_retorno * delta;
        if (GlobalPosition.DistanceTo(_pontoDeRetorno) < 5.0f)
        {
            GlobalPosition = _pontoDeRetorno;
            _estado = "patrulhando";
            iniciar_cooldown();
        }
    }

    public async void tomar_dano(int quantidade, Node2D? atacante = null)
    {
        if (_estado == "morto")
            return;

        _vida -= quantidade;
        _anim.Visible = false;
        _spriteMachucado.Visible = true;
        if (_vida <= 0)
        {
            _spriteMachucado.Modulate = Colors.White;
            morrer();
            return;
        }

        _spriteMachucado.Modulate = new Color(1.0f, 0.2f, 0.2f);
        if (_estado == "no_chao")
            _estado = "retornando";
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        if (_estado != "morto")
        {
            _spriteMachucado.Modulate = Colors.White;
            _spriteMachucado.Visible = false;
            _anim.Visible = true;
            _anim.Play(_estado is "patrulhando" or "retornando" ? "fly" : "hit");
        }
    }

    public void aplicar_impacto(Vector2 origem, float forca)
    {
        if (_estado == "morto")
            return;
        float direcao = GlobalPosition.X >= origem.X ? 1.0f : -1.0f;
        GlobalPosition += new Vector2(direcao * Mathf.Min(forca * 0.08f, 4.0f), -1.0f);
    }

    public async void morrer()
    {
        _estado = "morto";
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        SetDeferred(Area2D.PropertyName.Monitorable, false);
        GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        GetNodeOrNull<CollisionShape2D>("AreaDeteccao/CollisionShape2D")?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _anim.Visible = false;
        _spriteMachucado.Visible = true;
        _spriteMachucado.RotationDegrees = 0.0f;
        _spriteMachucado.Play("morte");
        await ToSignal(_spriteMachucado, AnimatedSprite2D.SignalName.AnimationFinished);
        QueueFree();
    }

    public async void iniciar_ataque()
    {
        if (_estado != "patrulhando" || !_podeAtacar)
            return;

        _estado = "preparando";
        _podeAtacar = false;
        _pontoDeRetorno = new Vector2(GlobalPosition.X, _alturaPatrulhaOriginal);
        await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
        if (_estado == "preparando")
        {
            _estado = "atacando";
            _anim.Play("hit");
        }
    }

    public async void ficar_no_chao()
    {
        if (_estado is "no_chao" or "morto")
            return;

        _estado = "no_chao";
        _anim.Play("hit");
        await ToSignal(GetTree().CreateTimer(tempo_vulneravel), SceneTreeTimer.SignalName.Timeout);
        if (_estado == "no_chao")
        {
            _estado = "retornando";
            _anim.Play("fly");
        }
    }

    public async void iniciar_cooldown()
    {
        _podeAtacar = false;
        await ToSignal(GetTree().CreateTimer(cooldown_ataque), SceneTreeTimer.SignalName.Timeout);
        _podeAtacar = true;
    }

    public void _on_body_entered(Node2D body)
    {
        if (body.Name != "bury" || body is not Player player)
            return;

        player.tomar_dano(1, this);
        if (_estado == "atacando")
        {
            _estado = "retornando";
            _anim.Play("fly");
        }
    }

    public void _on_area_deteccao_body_entered(Node2D body) { }
    public void _on_area_deteccao_body_exited(Node2D body) { }
}
