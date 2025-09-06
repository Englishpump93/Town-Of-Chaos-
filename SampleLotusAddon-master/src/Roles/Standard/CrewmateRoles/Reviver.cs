using Lotus.Roles.Internals.Enums;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Interfaces;
using Lotus.Utilities;
using VentLib.Utilities.Optionals;
using Lotus.Extensions;
using UnityEngine;
using Lotus.Roles.Internals;
using VentLib.Options.UI;
using VentLib.Localization.Attributes;
using Lotus.Options;
using VentLib.Utilities;
using System.Collections.Generic;
using VentLib.Utilities.Extensions;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.GUI;
using VentLib.Utilities.Collections;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Events;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Logging;
using Lotus.Logging;
using System;
using Lotus.API;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.Roles.Overrides;
using Lotus.Victory.Conditions;
using Lotus.RPC;
using Lotus.Roles.Subroles;
using VentLib.Networking.RPC.Attributes;
using Object = UnityEngine.Object;
using Lotus.Roles.GUI; // <-- Needed for RoleUtils
using static Lotus.Options.GeneralOptionTranslations;

public class Reviver : Crewmate, IRoleCandidate, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Reviver));
    private HashSet<PlayerControl> revivedPlayers = new();
    private bool blockGhostInfo = true;
    private bool hasArrowsToBodies;
    public static HashSet<Type> ReviverBannedModifiers = new() { typeof(Oblivious), typeof(Sleuth) };

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Has Arrows to Bodies", "Has Arrows to Bodies")
                .AddBoolean(false)
                .BindBool(b => hasArrowsToBodies = b)
                .Build());

    [RoleAction(LotusActionType.RoundStart)]
    private void OnRoundStart()
    {
        GeneralOptions.GameplayOptions.GhostsSeeInfo = false;
    }
     
	 private Remote<IndicatorComponent>? arrowComponent; // Add this field

    [RoleAction(LotusActionType.ReportBody)]
    private void OnReport(Optional<NetworkedPlayerInfo> body, ActionHandle handle)
    {
        if (!body.Exists()) return;
        PlayerControl? deadPlayer = Players.GetPlayers(PlayerFilter.Dead).FirstOrDefault(p => p.PlayerId == body.Get().PlayerId);
        if (deadPlayer == null || !deadPlayer.PrimaryRole().RealRole.IsCrewmate()) return;
        log.Debug($"Reviving crewmate: {deadPlayer.name}");
        handle.Cancel();
        RevivePlayer(deadPlayer);
        GeneralOptions.GameplayOptions.GhostsSeeInfo = false;
		// Remove the arrow indicator after reporting
		var bodyObj = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(b => b.ParentId == deadPlayer.PlayerId);
        if (bodyObj != null)
        {
            Object.Destroy(bodyObj.gameObject);
        }
            arrowComponent?.Delete();
    }

    private void RevivePlayer(PlayerControl player)
    {
        player.PrimaryRole().Revive();
        revivedPlayers.Add(player);
        Game.MatchData.GameHistory.AddEvent(new GenericTargetedEvent(MyPlayer, player, $"{MyPlayer.name} revived {player.GetNameWithRole()} successfully."));
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath)]
    private void OnReviverDeath(PlayerControl victim)
    {
        if (victim == MyPlayer)
        {
            foreach (var revived in revivedPlayers)
            {
                if (revived.IsAlive()) revived.Die(DeathReason.Exile, true);
            }
            revivedPlayers.Clear();
            GeneralOptions.GameplayOptions.GhostsSeeInfo = true;
        }
    }

    [RoleAction(LotusActionType.RoundEnd, ActionFlag.WorksAfterDeath)]
    private void OnMeeting()
    {
        blockGhostInfo = false;
        GeneralOptions.GameplayOptions.GhostsSeeInfo = true;
    }

    private bool ShouldGhostSeeInfo(PlayerControl deadPlayer)
    {
        if (deadPlayer.IsAlive() || !GeneralOptions.GameplayOptions.GhostsSeeInfo) return false;
        return true;
    }

    public bool GhostsSeeInfo => !blockGhostInfo;
    public bool CanBeVotedOut() => false;
    public bool ShouldSkip() => false;
    
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.77f, 0.28f, 1.00f));

    public RoleButton ReportButton(IRoleButtonEditor reportButton) => reportButton
        .SetText(ReviverTranslations.ButtonText)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/reviver_revive.png", 130, true));

    // --- Arrow indicator logic ---
    [UIComponent(UI.Indicator)]
    private string Arrows() => hasArrowsToBodies
        ? Object.FindObjectsOfType<DeadBody>()
            .Where(b => !Game.MatchData.UnreportableBodies.Contains(b.ParentId))
            .Select(b => Lotus.Roles.RoleUtils.CalculateArrow(MyPlayer, b, RoleColor)).Fuse("")
        : "";

    [Localized(nameof(Reviver))]
    public static class ReviverTranslations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Revive";
    }
}