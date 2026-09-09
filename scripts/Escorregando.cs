using Godot;

public partial class Escorregando : Area2D
{
    [Export]
    public float direcao_da_descida = 1.0f;

    [ExportGroup("Percurso da água")]
    [Export] public Marker2D? ponto_entrada;
    [Export] public Marker2D? ponto_curva;
    [Export] public Marker2D? ponto_fim_agua;
    [Export] public Marker2D? ponto_final;
    [Export] public TransicaoCena? transicao_final;
    [Export] public float duracao_entrada = 0.25f;
    [Export] public float duracao_descida = 2.2f;
    [Export] public float duracao_queda = 0.75f;
    [Export] public float deslocamento_visual_agua = 16.0f;

    private bool _escorregamentoEmAndamento;

    public override void _Ready()
    {
        BodyEntered += _on_body_entered;
    }

    public async void _on_body_entered(Node2D body)
    {
        if (_escorregamentoEmAndamento || body.Name != "bury" || body is not Player player)
            return;

        _escorregamentoEmAndamento = true;
        player.forcar_escorregamento(direcao_da_descida);

        if (ponto_entrada is null || ponto_curva is null || ponto_fim_agua is null || ponto_final is null)
        {
            GD.PushError("O percurso de escorregamento não está configurado na cena.");
            _escorregamentoEmAndamento = false;
            return;
        }

        player.SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;
        player.GlobalPosition = ponto_entrada.GlobalPosition;

        AnimatedSprite2D animPlayer = player.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        Vector2 posicaoAnimacaoOriginal = animPlayer.Position;
        animPlayer.Position = posicaoAnimacaoOriginal + new Vector2(0.0f, deslocamento_visual_agua);
        string animacaoEscorregando = direcao_da_descida < 0.0f
            ? "personagem_escorregando_esq"
            : "personagem_escorregando";
        animPlayer.Play(animPlayer.SpriteFrames?.HasAnimation(animacaoEscorregando) == true
            ? animacaoEscorregando
            : "idle");

        Tween tween = CreateTween();
        tween.TweenProperty(player, "global_position", ponto_curva.GlobalPosition, duracao_entrada)
            .SetTrans(Tween.TransitionType.Linear);
        tween.TweenProperty(player, "global_position", ponto_fim_agua.GlobalPosition, duracao_descida)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        tween.TweenCallback(Callable.From(() =>
        {
            string animacaoQueda = direcao_da_descida < 0.0f ? "jump_esq" : "jump";
            animPlayer.Play(animPlayer.SpriteFrames?.HasAnimation(animacaoQueda) == true
                ? animacaoQueda
                : "jump");
        }));
        tween.TweenProperty(player, "global_position", ponto_final.GlobalPosition, duracao_queda)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        await ToSignal(tween, Tween.SignalName.Finished);

        if (GodotObject.IsInstanceValid(animPlayer))
            animPlayer.Position = posicaoAnimacaoOriginal;

        if (transicao_final is not null && GodotObject.IsInstanceValid(transicao_final))
            transicao_final.mudar_de_cena();
        else
            GD.PushError("A transição final do escorregamento não está configurada na cena.");
    }
}
