using Lotus;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Options.UI;

namespace SampleRoleAddon.Gamemodes.BombTag.Options;

// This class stores and creates the options for the Bomb Tag Gamemode.
// These won't save after closing the game as we don't use an OptionManager.
public class BombTagOptionHolder
{
    public static bool ShowBombedPlayersToAll = true;
    public static bool BombedPlayersCanVent = false;
    public static float BombDuration = 30f;
    public static bool CanTakeback = false;
    public static float BombCooldown = 0f;
    public static int BombCount = 1;
    public static float Delay = -1;
    public BombTagOptionHolder()
    {
        var optionsTab = BombTagGamemode.BombTagOptions;

        // Create a new title (divider) with our specified text
        optionsTab.AddOption(new GameOptionTitleBuilder()
            .Title("Bomb Tag Settings")
            .Color(Color.green)
            .Build());

        // Create the options for our gammemode
        optionsTab.AddOption(new GameOptionBuilder()
            .KeyName("Bomb Count", "Bomb Count")
            // if among us ever increases the player count. this will scale with it.
            .AddIntRange(1, ModConstants.MaxPlayers - 1, 1)
            .BindInt(v => BombCount = v)
            .Build());

        optionsTab.AddOption(new GameOptionBuilder()
            .KeyName("Bomb Duration", "Bomb Duration")
            .AddFloatRange(2.5f, 60f, 2.5f, 11, "s")
            .BindFloat(v => BombDuration = v)
            .Build());

        optionsTab.AddOption(new GameOptionBuilder()
            .KeyName("Bomb Cooldown", "Bomb Cooldown")
            .Value(v => v.Value(0f).Text("Disabled").Color(Color.red).Build())
            .AddFloatRange(2.5f, 60f, 2.5f, 0, "s")
            .BindFloat(v => BombCooldown = v)
            .ShowSubOptionPredicate(v => (float)v == 0f)
            .SubOption(sub => sub.KeyName("Takebacks", "Can Players Return the Bomb")
                .AddBoolean(false)
                .BindBool(v => CanTakeback = v)
                .Build())
            .Build());

        // optionsTab.AddOption(new GameOptionBuilder()
        //     .KeyName("Pass Delay", "Pass Delay")
        //     .Value(v => v.Value(-1f).Text("No Takebacks").Color(Color.red).Build())
        //     .AddFloatRange(5f, 60f, 5f, 1, "s")
        //     .BindFloat(v => Delay = v)
        //     .Build());

        optionsTab.AddOption(new GameOptionBuilder()
            .KeyName("Show Bombed Players to All", "Show Bombed Players to All")
            .AddBoolean()
            .BindBool(v => ShowBombedPlayersToAll = v)
            .Build());

        optionsTab.AddOption(new GameOptionBuilder()
            .KeyName("Bombed Players Can Vent", "Bombed Players Can Vent")
            .AddBoolean()
            .BindBool(v => BombedPlayersCanVent = v)
            .Build());
    }
}