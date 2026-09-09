using Godot;
using System.Threading.Tasks;

// Integration tests deliberately position the player at interactions. Traversal is tested separately.
public partial class PremiumProgression : Node
{
    private Global _world = null!;
    private int _checks, _failures;
    private Node Room => GetTree().CurrentScene;
    private Player Bury => Room.GetNode<Player>("bury");
    private void Check(bool ok, string label)
    {
        _checks++; if (!ok) _failures++;
        GD.Print($"PROGRESSION_{(ok ? "PASS" : "FAIL")} {label}");
    }
    private async Task Frames(int n=4) { for(int i=0;i<n;i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private async Task Delay(double n=.4) => await ToSignal(GetTree().CreateTimer(n), SceneTreeTimer.SignalName.Timeout);
    private async Task Load(string name)
    {
        GetTree().ChangeSceneToFile($"res://scene/{name}.tscn");
        await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
        await Frames();
    }
    private void Press() => Input.ParseInputEvent(new InputEventAction { Action="interact",Pressed=true });
    private async Task Touch(string name, bool press=true)
    {
        var area=Room.GetNode<WorldInteraction>("Gameplay/"+name);
        Bury.GlobalPosition=area.GlobalPosition+new Vector2(0,8); Bury.Velocity=Vector2.Zero;
        await Frames(6);
        if(press) Press();
        await Delay(1.5);
    }
    private async Task ClearEncounter(string flag)
    {
        Node encounter=Room.GetNode("Gameplay/FinalEncounter");
        foreach(Node n in encounter.GetChildren()) n.Call("tomar_dano",3,Bury);
        await Delay(2);
        Check(_world.HasWorldFlag(flag), "all enemies cleared -> "+flag);
    }
    private async Task LiftTo(int floor)
    {
        var lift=Room.GetNode<WorldElevator>("Gameplay/CentralElevator");
        Bury.GlobalPosition=lift.GlobalPosition+new Vector2(0,14); Bury.Velocity=Vector2.Zero;
        await Frames(6); Press(); await Frames();
        Check(lift.GetNode<Control>("UI/Panel").Visible,"lift panel opens");
        var button=lift.GetNode<Button>($"UI/Panel/Floors/F{floor}");
        Check(!button.Disabled,"discovered floor selectable");
        button.EmitSignal(Button.SignalName.Pressed);
        await Delay(1.5);
    }
    public override async void _Ready()
    {
        await Frames(); Node boot=new(); GetTree().Root.AddChild(boot); GetTree().CurrentScene=boot;
        _world=GetNode<Global>("/root/Global");
        await Load("topo_predio");
        Check(!Bury.pode_atacar && !Bury.pode_dash && Bury.pode_pular,"arrival abilities");
        await Load("segredo_telhado"); Check(!Bury.pode_atacar,"secret roof cannot bypass sword progression");
        await Load("topo_predio");
        await Touch("SeventhFloor"); Check(Room.Name=="topo_predio","seventh floor locked without sword");
        await Touch("DashLesson",false); Check(Bury.pode_dash,"dash lesson trigger");
        await Touch("Sword"); Check(Bury.pode_atacar && _world.HasWorldFlag("sword"),"physical sword interaction");
        await Touch("SeventhFloor"); Check(Room.Name=="bury_the_chaos","roof -> seventh");
        await Touch("ArchiveSeal"); Check(Room.Name=="bury_the_chaos","archive initially locked");
        await Touch("NextFloor"); Check(Room.Name=="predio","seventh -> sixth");
        await Touch("NextFloor"); Check(Room.Name=="predio","sixth exit requires encounter");
        await ClearEncounter("encounter_6"); await Touch("NextFloor"); Check(Room.Name=="quinto_andar","sixth -> fifth");
        await Touch("ShaftRecovery"); Check(Room.Name=="quinto_andar" && Bury.Position.Y<680,"premature fall has a safe return");
        await ClearEncounter("encounter_5");
        Bury.GlobalPosition=new Vector2(2440,690); Bury.Velocity=Vector2.Zero;
        await Delay(3); Check(Room.Name=="profundezas","physical fifth-floor fall -> subsoil");
        Check(Bury.GlobalPosition.X<160 && Bury.GlobalPosition.Y<240,"fall landing spawn");
        await Touch("PumpFuse"); Check(_world.HasWorldFlag("pump_fuse"),"pump fuse pickup");
        await ClearEncounter("encounter_depths"); await Touch("NextFloor"); Check(Room.Name=="portao_king_rat","subsoil -> sewer");
        await Touch("OldInsignia"); Check(_world.HasWorldFlag("sigil_sewer"),"sewer insignia pickup");
        await Touch("NextFloor"); Check(Room.Name=="portao_king_rat","final approach gated");
        await ClearEncounter("encounter_sewer"); await Touch("NextFloor"); Check(Room.Name=="arena_rei_ratos","sewer -> final arena");
        await Touch("Return"); Check(Room.Name=="portao_king_rat","arena return");
        await LiftTo(1); Check(Room.Name=="bury_the_chaos","central lift sewer -> seventh");
        await Touch("ArchiveSeal"); Check(Room.Name=="arquivo_sete","late insignia unlocks early archive");
        await Touch("Reward"); Check(_world.HasWorldFlag("lift_core"),"archive core reward");
        await Touch("Return"); await LiftTo(2); Check(Room.Name=="predio","return to caretaker");
        await Touch("Caretaker"); Check(_world.HasWorldFlag("workshop_repaired"),"caretaker delivery");
        await Touch("WorkshopPassage"); Check(Room.Name=="quinto_andar","repaired workshop shortcut");
        await Touch("FloodedFlat"); Check(Room.Name=="sala_inundada","subsoil fuse unlocks fifth-floor room");
        await Touch("Reward"); Check(_world.HasWorldFlag("drained_room"),"drained room reward");
        await Touch("Return");
        await Touch("ServiceSpineUpper"); Check(!_world.HasWorldFlag("shortcut_quinto_andar"),"shortcut cannot open from above");
        await Touch("ServiceSpineLower"); Check(_world.HasWorldFlag("shortcut_quinto_andar") && Bury.Position.Y<240,"shortcut opens from below and returns");
        Check(Room.GetNode<CollisionShape2D>("Gameplay/ServiceHatch0/Hatch/CollisionShape2D").Disabled,"physical hatch opens");
        Bury.vida_atual=1; await Touch("Rest0"); Check(Bury.vida_atual==4,"rest restores health");
        Vector2 rest=Bury.GlobalPosition;
        _world.PrepareRespawn(); GetTree().ReloadCurrentScene();
        await ToSignal(GetTree(),SceneTree.SignalName.SceneChanged); await Frames(6);
        Check(Bury.GlobalPosition.DistanceTo(rest)<6,"checkpoint survives scene reload");
        Check(Room.GetNode<CollisionShape2D>("Gameplay/ServiceHatch0/Hatch/CollisionShape2D").Disabled,"shortcut persists after reload");
        Check(Room.GetNode("Gameplay/FinalEncounter").GetChildCount()==0,"cleared encounter stays cleared");
        GD.Print($"PROGRESSION_RESULT checks={_checks} failures={_failures}");
        GetTree().Quit(_failures==0?0:1);
    }
}
