using Godot;
using System.Threading.Tasks;

public partial class WorldFade : CanvasLayer
{
    [Export] public float FadeDuration = 0.18f;

    private ColorRect _overlay = null!;
    private Tween? _tween;

    public override void _Ready()
    {
        _overlay = GetNode<ColorRect>("Overlay");
        _overlay.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }

    public Task FadeOut() => _fade_para(1.0f);
    public Task FadeIn() => _fade_para(0.0f);

    private async Task _fade_para(float alpha)
    {
        _tween?.Kill();
        _tween = CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(
            alpha > _overlay.Modulate.A ? Tween.EaseType.In : Tween.EaseType.Out);
        _tween.TweenProperty(_overlay, "modulate:a", alpha, Mathf.Max(0.01f, FadeDuration));
        await ToSignal(_tween, Tween.SignalName.Finished);
    }
}
