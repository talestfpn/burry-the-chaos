using Godot;
using System.Threading.Tasks;

public partial class PlayerPolishSmokeTest : Node2D
{
    private static bool _aguardandoReloadDaMorte;
    private static int _falhas;

    private Player _player = null!;
    private PlayerPolishTarget _target = null!;

    public override async void _Ready()
    {
        if (_aguardandoReloadDaMorte)
        {
            _registrar(true, "death_reload");
            _finalizar();
            return;
        }

        _player = GetNode<Player>("bury");
        _target = GetNode<PlayerPolishTarget>("Target");
        await _soltar_inputs_e_resetar(new Vector2(80.0f, 150.0f));

        _registrar(_player.IsOnFloor(), "ground_detection");
        await _testar_movimento_horizontal();
        await _testar_ataque();
        await _testar_pulos();
        await _testar_coyote_time();
        await _testar_jump_buffer();
        await _testar_dash();
        await _testar_parede_e_stamina();
        await _testar_dano_e_invulnerabilidade();
        await _testar_morte();
    }

    private async Task _testar_movimento_horizontal()
    {
        Input.ActionPress("right");
        await _esperar_frames(8);
        _registrar(_player.Velocity.X > 70.0f && _player.Velocity.X <= _player.MaxSpeed + 0.1f, "horizontal_acceleration");
        Input.ActionRelease("right");
        await _esperar_frames(7);
        _registrar(Mathf.Abs(_player.Velocity.X) < 1.0f, "horizontal_deceleration");
    }

    private async Task _testar_ataque()
    {
        await _soltar_inputs_e_resetar(new Vector2(80.0f, 150.0f));
        Input.ActionPress("attack");
        await _esperar_frames(1);
        Input.ActionRelease("attack");
        await _esperar_frames(24);
        _registrar(_target.Hits == 1, "attack_single_hit_per_target");
    }

    private async Task _testar_pulos()
    {
        await _soltar_inputs_e_resetar(new Vector2(80.0f, 150.0f));
        float inicioCurto = _player.GlobalPosition.Y;
        float topoCurto = inicioCurto;
        Input.ActionPress("jump");
        await _esperar_frames(1);
        Input.ActionRelease("jump");
        for (int i = 0; i < 120; i++)
        {
            await _esperar_frames(1);
            topoCurto = Mathf.Min(topoCurto, _player.GlobalPosition.Y);
            if (_player.IsOnFloor() && i > 4)
                break;
        }
        float alturaCurta = inicioCurto - topoCurto;

        await _soltar_inputs_e_resetar(new Vector2(80.0f, 150.0f));
        float inicioCheio = _player.GlobalPosition.Y;
        float topoCheio = inicioCheio;
        Input.ActionPress("jump");
        for (int i = 0; i < 12; i++)
        {
            await _esperar_frames(1);
            topoCheio = Mathf.Min(topoCheio, _player.GlobalPosition.Y);
        }
        Input.ActionRelease("jump");
        for (int i = 0; i < 120; i++)
        {
            await _esperar_frames(1);
            topoCheio = Mathf.Min(topoCheio, _player.GlobalPosition.Y);
            if (_player.IsOnFloor() && i > 4)
                break;
        }
        float alturaCheia = inicioCheio - topoCheio;
        _registrar(alturaCurta > 5.0f && alturaCheia > alturaCurta + 8.0f, "variable_jump_height");
    }

    private async Task _testar_coyote_time()
    {
        await _soltar_inputs_e_resetar(new Vector2(154.0f, 150.0f));
        Input.ActionPress("right");
        bool saiuDoChao = false;
        for (int i = 0; i < 60; i++)
        {
            await _esperar_frames(1);
            if (!_player.IsOnFloor())
            {
                saiuDoChao = true;
                break;
            }
        }
        await _esperar_frames(2);
        Input.ActionPress("jump");
        await _esperar_frames(2);
        Input.ActionRelease("jump");
        Input.ActionRelease("right");
        _registrar(saiuDoChao && _player.Velocity.Y < 0.0f, "coyote_time");
    }

    private async Task _testar_jump_buffer()
    {
        _player.pode_pulo_duplo = false;
        await _soltar_inputs_e_resetar(new Vector2(80.0f, 112.0f), 1);
        for (int i = 0; i < 90 && _player.GlobalPosition.Y < 136.0f; i++)
            await _esperar_frames(1);

        Input.ActionPress("jump");
        await _esperar_frames(1);
        Input.ActionRelease("jump");
        bool pulouAoAterrissar = false;
        for (int i = 0; i < 25; i++)
        {
            await _esperar_frames(1);
            if (_player.Velocity.Y < -80.0f)
            {
                pulouAoAterrissar = true;
                break;
            }
        }
        _registrar(pulouAoAterrissar, "jump_buffer");
        _player.pode_pulo_duplo = true;
    }

    private async Task _testar_dash()
    {
        await _soltar_inputs_e_resetar(new Vector2(80.0f, 150.0f));
        _player.stamina = _player.MaxStamina;
        Input.ActionPress("right");
        Input.ActionPress("dash");
        await _esperar_frames(2);
        Input.ActionRelease("dash");
        float staminaDepoisDoDash = _player.stamina;
        _registrar(_player.Velocity.X > 200.0f && Mathf.IsEqualApprox(staminaDepoisDoDash, _player.MaxStamina - _player.DashCost), "dash_start_and_cost");

        await _esperar_frames(9);
        Input.ActionPress("dash");
        await _esperar_frames(2);
        Input.ActionRelease("dash");
        Input.ActionRelease("right");
        _registrar(Mathf.IsEqualApprox(_player.stamina, staminaDepoisDoDash), "dash_cooldown");
    }

    private async Task _testar_parede_e_stamina()
    {
        await _soltar_inputs_e_resetar(new Vector2(198.0f, 100.0f), 1);
        _player.stamina = _player.MaxStamina;
        Input.ActionPress("right");
        await _esperar_frames(12);
        Input.ActionPress("up");
        await _esperar_frames(4);
        bool subindo = _player.IsOnWallOnly() && _player.Velocity.Y < -20.0f;
        bool consumiu = _player.stamina < _player.MaxStamina && _player.stamina >= 0.0f;
        Input.ActionRelease("up");
        Input.ActionRelease("right");
        _registrar(subindo, "wall_grip_and_climb");
        _registrar(consumiu, "wall_stamina_clamped");
    }

    private async Task _testar_dano_e_invulnerabilidade()
    {
        await _soltar_inputs_e_resetar(new Vector2(80.0f, 150.0f));
        int vidaInicial = _player.vida_atual;
        _player.tomar_dano(1, _target);
        _player.tomar_dano(1, _target);
        _registrar(_player.vida_atual == vidaInicial - 1, "invulnerability_blocks_multihit");
        _registrar(Mathf.Abs(_player.Velocity.X) >= _player.KnockbackHorizontal - 1.0f, "damage_knockback");
        await _esperar_frames(42);
        _player.tomar_dano(1, _target);
        _registrar(_player.vida_atual == vidaInicial - 2, "invulnerability_expires");
    }

    private async Task _testar_morte()
    {
        await _esperar_frames(42);
        _aguardandoReloadDaMorte = true;
        _player.tomar_dano(99, _target);
        await _esperar_frames(180);
        _registrar(false, "death_reload");
        _finalizar();
    }

    private async Task _soltar_inputs_e_resetar(Vector2 posicao, int frames = 4)
    {
        foreach (string action in new[] { "left", "right", "up", "down", "jump", "dash", "attack" })
            Input.ActionRelease(action);
        _player.GlobalPosition = posicao;
        _player.Velocity = Vector2.Zero;
        await _esperar_frames(frames);
    }

    private async Task _esperar_frames(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static void _registrar(bool passou, string nome)
    {
        GD.Print($"PLAYER_POLISH_TEST {nome}: {(passou ? "PASS" : "FAIL")}");
        if (!passou)
            _falhas++;
    }

    private void _finalizar()
    {
        GD.Print($"PLAYER_POLISH_TEST RESULT: {(_falhas == 0 ? "PASS" : "FAIL")} ({_falhas} failures)");
        GetTree().Quit(_falhas == 0 ? 0 : 1);
    }
}
