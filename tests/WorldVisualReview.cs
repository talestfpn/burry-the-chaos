// Compatibility entry point: the former eight-room graph was replaced by the descent world.
public partial class WorldVisualReview : PremiumWorldReview
{
    public override void _Ready() { CaptureViews = true; base._Ready(); }
}
