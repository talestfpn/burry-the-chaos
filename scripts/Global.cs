using Godot;
using System.Collections.Generic;

public partial class Global : Node
{
    public enum EstadoDaRun
    {
        Menu,
        Jogando
    }

    public const string CenaMenu = "res://scene/main_menu.tscn";
    public const string CenaInicial = "res://scene/quarto.tscn";

    [Signal]
    public delegate void moedas_alteradasEventHandler(int total);

    public int moedas = 0;
    public EstadoDaRun estado_da_run { get; private set; } = EstadoDaRun.Menu;
    public string cena_atual { get; private set; } = CenaMenu;
    public string sala_atual { get; private set; } = "menu";
    public string spawn_pendente { get; private set; } = "Default";

    private bool _trocandoCena;
    private readonly HashSet<string> _atalhosAbertos = new();
    private readonly HashSet<string> _worldFlags = new();
    private readonly Dictionary<string, Vector2> _benches = new();
    public void RestAt(string room, Vector2 position) => _benches[room] = position;
    public bool TryGetRest(string room, out Vector2 position) => _benches.TryGetValue(room, out position);
    public void PrepareRespawn() { if (_benches.ContainsKey(sala_atual)) spawn_pendente = "Checkpoint"; }
    [Signal] public delegate void WorldChangedEventHandler();
    public bool HasWorldFlag(string id) => string.IsNullOrEmpty(id) || _worldFlags.Contains(id);
    public void SetWorldFlag(string id)
    {
        if (!string.IsNullOrEmpty(id) && _worldFlags.Add(id)) EmitSignal(SignalName.WorldChanged);
    }

    public void iniciar_novo_jogo()
    {
        resetar_moedas();
        _atalhosAbertos.Clear();
        _worldFlags.Clear();
        _benches.Clear();
        spawn_pendente = "Start";
        estado_da_run = EstadoDaRun.Jogando;
        trocar_cena(CenaInicial, "Start");
    }

    public void voltar_ao_menu()
    {
        estado_da_run = EstadoDaRun.Menu;
        trocar_cena(CenaMenu);
    }

    public async void trocar_cena(string caminho, string spawnDestino = "Default")
    {
        if (_trocandoCena || string.IsNullOrWhiteSpace(caminho))
            return;

        _trocandoCena = true;
        WorldFade? fade = GetNodeOrNull<WorldFade>("/root/WorldFade");
        if (fade is not null)
            await fade.FadeOut();

        spawn_pendente = string.IsNullOrWhiteSpace(spawnDestino) ? "Default" : spawnDestino;
        Error erro = GetTree().ChangeSceneToFile(caminho);
        if (erro != Error.Ok)
        {
            GD.PushError($"Não foi possível carregar a cena '{caminho}': {erro}.");
            if (fade is not null)
                await fade.FadeIn();
            _trocandoCena = false;
            return;
        }

        cena_atual = caminho;
        estado_da_run = caminho == CenaMenu ? EstadoDaRun.Menu : EstadoDaRun.Jogando;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (fade is not null)
            await fade.FadeIn();
        _trocandoCena = false;
    }

    public string consumir_spawn_pendente(string salaId, string fallback = "Default")
    {
        sala_atual = salaId;
        string spawn = string.IsNullOrWhiteSpace(spawn_pendente) ? fallback : spawn_pendente;
        spawn_pendente = "Default";
        return spawn;
    }

    public bool atalho_esta_aberto(string atalhoId) =>
        !string.IsNullOrWhiteSpace(atalhoId) && _atalhosAbertos.Contains(atalhoId);

    public void abrir_atalho(string atalhoId)
    {
        if (!string.IsNullOrWhiteSpace(atalhoId))
            _atalhosAbertos.Add(atalhoId);
    }

    public void adicionar_moedas(int quantidade = 1)
    {
        moedas += quantidade;
        EmitSignal("moedas_alteradas", moedas);
    }

    public bool gastar_moedas(int quantidade)
    {
        if (moedas < quantidade)
            return false;

        moedas -= quantidade;
        EmitSignal("moedas_alteradas", moedas);
        return true;
    }

    public void resetar_moedas()
    {
        moedas = 0;
        EmitSignal("moedas_alteradas", moedas);
    }
}
