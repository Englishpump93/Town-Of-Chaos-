using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals;
using AmongUs.GameOptions;
using Lotus.Roles;
using SampleRoleAddon.Gamemodes.BombTag.Options;
using UnityEngine;
using Lotus.GUI;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using Lotus.GUI.Name;

namespace SampleRoleAddon.Roles.BombTag;

// We use neutral killing base since the game auto desyncs them from other players because their team is Neutral.
[Localized($"Roles.{nameof(HasNoBomb)}")]
public class HasNoBomb: NeutralKillingBase
{
    public bool hasStarted = false;
    public Cooldown bombDuration; // we will use the built in UI functions in order to display the cooldown
    public byte lastPass = 255;

    [UIComponent(UI.Text)]
    public string SecondsLeftText() => bombDuration.IsReady() ? "" : RoleColor.Colorize("Seconds Left: " + bombDuration + "s");

    [RoleAction(LotusActionType.RoundStart)] // Start countdown
    private void OnRoundStart()
    {
        bombDuration.Start(BombTagOptionHolder.BombDuration);
        hasStarted = true;
    }

    [RoleAction(LotusActionType.Attack, ActionFlag.GlobalDetector, priority: Lotus.API.Priority.VeryHigh)]
    private void InterceptActionOnCooldown(PlayerControl actor, PlayerControl target, ActionHandle handle)
    {
        if (target.PlayerId != MyPlayer.PlayerId) return; // skip if its not us.
        // Condition 1: If they are the person we passed to last and we cant take it back.
        bool canCacel = actor.PlayerId == lastPass && !BombTagOptionHolder.CanTakeback && BombTagOptionHolder.BombCooldown == 0f;
        if(canCacel)
            handle.Cancel();  // cancel interaction
    }

    // Cancel any bodies they attempt to report.
    [RoleAction(LotusActionType.ReportBody)]
    private void TryReportBody(ActionHandle handle) => handle.Cancel();

    [RoleAction(LotusActionType.FixedUpdate)]
    private void CooldownUpdate()
    {
        if (!hasStarted | bombDuration.NotReady()) return;
        bombDuration.Start(BombTagOptionHolder.BombDuration);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
        .DesyncRole(RoleTypes.Impostor)
        .RoleFlags(RoleFlag.DontRegisterOptions | RoleFlag.CannotWinAlone)
        .RoleAbilityFlags(RoleAbilityFlag.CannotVent | RoleAbilityFlag.CannotSabotage)
        .RoleColor(Color.gray)
        .IntroSound(RoleTypes.Crewmate);
}