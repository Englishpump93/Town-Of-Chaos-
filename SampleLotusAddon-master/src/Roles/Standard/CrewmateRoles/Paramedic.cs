using Lotus.API.Odyssey;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Optionals;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles.Interactions.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Lotus.Roles.Subroles;
using Lotus.API;
using Lotus.Extensions;
using Lotus.Roles.Subroles.Romantics;
using Lotus.Managers.History.Events;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using static Lotus.Options.GeneralOptionTranslations;
using static Lotus.Roles.RoleGroups.Crew.Medic.MedicTranslations;
using static Lotus.Roles.RoleGroups.Crew.Doctor.Translations;
using Lotus.Roles.Interfaces;
using SampleRoleAddon.Roles.Standard.Modifiers;
using Lotus;
using Lotus.Roles;
using Lotus.Utilities;
using Lotus.Patches.Systems;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using Lotus.GUI;
using VentLib.Utilities;



namespace SampleRoleAddon.Roles.Standard.CrewmateRoles;

public class Paramedic : Crewmate
{
    private static readonly Color ParamedicColor = new(0.2f, 0.7f, 1f); // Light blue
    private byte protectedPlayer = byte.MaxValue;
    private bool targetLockedIn;
    private bool confirmedVote;
    private Remote<IndicatorComponent>? protectedIndicator;
    public static HashSet<Type> ParamedicBannedModifiers = new() { typeof(Romantic), typeof(LastResort), typeof(ChaosDoctor) };

    private const string CrossText = "<size=2><b>+</b></size>";

    // --- Cause of Death display ---
    [NewOnSetup] private Dictionary<byte, Remote<TextComponent>> codComponents = null!;

    [RoleAction(LotusActionType.PlayerDeath)]
    private void ParamedicAnyDeath(PlayerControl dead, IDeathEvent causeOfDeath)
    {
        if (codComponents.ContainsKey(dead.PlayerId)) codComponents[dead.PlayerId].Delete();
        string coloredString = "<size=1.6>" + Color.white.Colorize($"({RoleColor.Colorize(causeOfDeath.SimpleName())})") + "</size>";

        TextComponent textComponent = new(new LiveString(coloredString), GameState.InMeeting, viewers: MyPlayer);
        codComponents[dead.PlayerId] = dead.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
    }
    // --- End Cause of Death display ---

    [RoleAction(LotusActionType.RoundEnd)]
    private void RoundEndMessage()
    {
        // Clean up cause of death components
       // foreach (var remote in codComponents.Values)
        //    remote.Delete();
      //  codComponents.Clear();

        // Do NOT reset protection at round end!
        // Only clean up indicator if protected player is dead
        if (protectedPlayer != byte.MaxValue && Players.FindPlayerById(protectedPlayer)?.Data.IsDead == true)
        {
            protectedIndicator?.Delete();
            protectedPlayer = byte.MaxValue;
            targetLockedIn = false;
            confirmedVote = false;
        }
        ResendMessages();
    }

    public void ResendMessages()
    {
        if (protectedPlayer == byte.MaxValue)
            CHandler("You are a Paramedic! Vote a player to protect during meetings.").Send(MyPlayer);
        else if (Players.FindPlayerById(protectedPlayer)?.Data.IsDead == true)
            CHandler("Your protected player is dead. You can no longer protect anyone.").Send(MyPlayer);
        else
            CHandler($"You are still protecting {Players.FindPlayerById(protectedPlayer)?.name} until you die!").Send(MyPlayer);
    }

    [RoleAction(LotusActionType.Vote)]
    private void HandleParamedicVote(Optional<PlayerControl> votedPlayer, MeetingDelegate _, ActionHandle handle)
    {
        // If protection is locked in, allow normal voting (skip, vote, etc.)
        if (targetLockedIn) return;

        if (!votedPlayer.Exists())
        {
            // If not confirmed yet, allow normal voting and reset selection
            if (!confirmedVote)
            {
                protectedPlayer = byte.MaxValue;
                protectedIndicator?.Delete();
                CHandler("No one selected. You can vote normally.").Send(MyPlayer);
            }
            // If already confirmed, allow normal voting, do not reset protection
            return;
        }

        PlayerControl voted = votedPlayer.Get();
        byte player = voted.PlayerId;

        if (player == MyPlayer.PlayerId) return;

        if (protectedPlayer != player)
        {
            // First vote: select player for protection, ask to vote again to confirm
            protectedIndicator?.Delete();
            protectedPlayer = player;
            confirmedVote = false;
            handle.Cancel(); // Cancel this vote, require confirmation
            protectedIndicator = voted.NameModel().GCH<IndicatorHolder>().Add(new SimpleIndicatorComponent(CrossText, ParamedicColor, Game.InGameStates, MyPlayer));
            CHandler($"Selected {Players.FindPlayerById(protectedPlayer)?.name} for protection. Vote again to confirm.").Send(MyPlayer);
            return;
        }

        if (!confirmedVote)
        {
            // Second vote: confirm protection and lock it in for the rest of the game
            confirmedVote = true;
            targetLockedIn = true;
            handle.Cancel(); // Cancel this vote, allow normal voting next
            CHandler($"You are now protecting {Players.FindPlayerById(protectedPlayer)?.name} until you die!").Send(MyPlayer);
            return;
        }

        // After confirmation, allow normal voting (do not reset protection)
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void HandleMyDeath(PlayerControl player, ActionHandle handle)
    {
        if (player.PlayerId != MyPlayer.PlayerId) return;
        protectedIndicator?.Delete();
        protectedPlayer = byte.MaxValue;
        targetLockedIn = false;
        confirmedVote = false;
    }

    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector)]
    private void PreventKill(PlayerControl target, PlayerControl interactor, Interaction interaction, ActionHandle handle)
    {
        if (protectedPlayer != target.PlayerId) return;
        if (interactor.PlayerId == MyPlayer.PlayerId) return;
        if (Game.State is not GameState.Roaming) return;

        // Cancel any kill/interaction on protected player
        handle.Cancel();
        CHandler($"{Players.FindPlayerById(protectedPlayer)?.name} is protected by the Paramedic!").Send(interactor);
    }

    private ChatHandler CHandler(string message) => new ChatHandler()
        .Title(t => t.PrefixSuffix(CrossText).Color(ParamedicColor).Text(RoleName).Build())
        .LeftAlign().Message(message);

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(1.00f, 0.38f, 0.00f));

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream);

    [Localized(nameof(Paramedic))]
    private static class Translations
    {
        // Add translations here if needed
    }
}