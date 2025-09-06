using Lotus.Roles.RoleGroups.NeutralKilling;
using SampleRoleAddon.Gamemodes.BombTag;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals;
using AmongUs.GameOptions;
using Lotus.Extensions;
using Lotus.Roles;
using SampleRoleAddon.Gamemodes.BombTag.Options;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name;
using UnityEngine;
using Lotus.API.Odyssey;
using Lotus.GUI.Name.Holders;
using Lotus.GUI;
using VentLib.Utilities;
using VentLib.Localization.Attributes;
using Lotus.Roles.Interactions;
using System.Collections.Generic;
using Lotus.API.Player;
using System.Linq;
using VentLib.Utilities.Extensions;
using VentLib.Logging;

namespace SampleRoleAddon.Roles.BombTag;

// We use neutral killing base since the game auto desyncs them from other players because their team is Neutral.
[Localized($"Roles.{nameof(HasBomb)}")]
public class HasBomb: NeutralKillingBase
{
    public bool hasStarted = false;
    private NameComponent coloredName;
    public Cooldown bombDuration; // we will use the built in UI functions in order to display the cooldown

    [UIComponent(UI.Text)]
    public string SecondsLeftText() => bombDuration.IsReady() ? "" : RoleColor.Colorize("Seconds Left: " + bombDuration + "s");

    protected override void Setup(PlayerControl player)
    {
        // Create ColoredName Component
        coloredName = new NameComponent(new LiveString(() => MyPlayer.name, RoleColor), GameState.Roaming, ViewMode.Absolute);
    }
    protected override void PostSetup()
    {
        // Change kill cooldown.
        if (BombTagOptionHolder.BombCooldown == 0f) KillCooldown = 0.42069f;
        else KillCooldown = BombTagOptionHolder.BombCooldown;
    }

    // Add colored name for other players on round start
    [RoleAction(LotusActionType.RoundStart)]
    private void OnRoundStart()
    {
        bombDuration.Start(BombTagOptionHolder.BombDuration);
        // Add if we show to all players
        if (BombTagOptionHolder.ShowBombedPlayersToAll) MyPlayer.NameModel().GetComponentHolder<NameHolder>().Add(coloredName);
    }

    // When the player uses their kill button to pass it to someone.
    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        if (target.PrimaryRole() is HasBomb) return false;
        // Show GA shield if we have an actual cooldown.
        if (KillCooldown > 1f) MyPlayer.RpcMark(target);

        // Remove it
        if (BombTagOptionHolder.ShowBombedPlayersToAll) MyPlayer.NameModel().GetComponentHolder<NameHolder>().Remove(coloredName);

        // Remove Cooldowns from player
        MyPlayer.NameModel().GetComponentHolder<TextHolder>().Clear();
        target.NameModel().GetComponentHolder<TextHolder>().Clear();

        // Switch roles.
        BombTagGamemode.Instance.Assign(MyPlayer, BombTagRoles.Instance.Static.HasNoBomb);

        // Set last pass ID for us
        HasNoBomb role = MyPlayer.PrimaryRole<HasNoBomb>();
        role.lastPass = target.PlayerId;

        // Give the player the role and we are done.
        BombTagGamemode.Instance.Assign(target, BombTagRoles.Instance.Static.HasBomb);

        // Update durations
        float TimeRemaining = bombDuration.TimeRemaining();
        role.bombDuration.Start(TimeRemaining);
        role.hasStarted = true;

        HasBomb bomb = target.PrimaryRole<HasBomb>();
        bomb.bombDuration.Start(TimeRemaining);
        bomb.hasStarted = true;
        return false;
    }

    // Cancel any bodies they attempt to report.
    [RoleAction(LotusActionType.ReportBody)]
    private void TryReportBody(ActionHandle handle) => handle.Cancel();

    [RoleAction(LotusActionType.FixedUpdate)]
    private void SuicideOnCountdownEnd()
    {
        if (!hasStarted | bombDuration.NotReady()) return;
        bombDuration.Start(BombTagOptionHolder.BombDuration);
        StaticLogger.Debug($"Suiciding for {MyPlayer.name}"); // log to console

        // We suicide by using a FatalIntent. A fatal intent actually kills the player.
        // An UnblockedInteraction ensures that it actually kills the player if it gets canceled somehow.
        MyPlayer.InteractWith(MyPlayer, new UnblockedInteraction(new FatalIntent(), this));

        // Now we have to assign a new player.
        List<PlayerControl> potentialTargets = Players.GetAlivePlayers().Where(p => p.PrimaryRole() is not HasBomb).ToList();
        if (potentialTargets.Count <= 1) return;
        // Assign a new bomber since we died.
        // Pretty unlucky to whoever gets assigned this role because we couldn't get someone.
        PlayerControl target = potentialTargets.GetRandom();
        StaticLogger.Debug($"Passing bomb to {target.name}"); 
        BombTagGamemode.Instance.Assign(target, BombTagRoles.Instance.Static.HasBomb);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => 
        base.Modify(roleModifier)
            .DesyncRole(RoleTypes.Impostor)
            .RoleFlags(RoleFlag.DontRegisterOptions | RoleFlag.CannotWinAlone)
            .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.IsAbleToKill)
            .RoleColor(Color.green)
            .IntroSound(RoleTypes.Shapeshifter)
            .CanVent(BombTagOptionHolder.BombedPlayersCanVent); // you might think this will always be the option that is set when we start the game, but. this function runs AGAIN when the role is assigned
}