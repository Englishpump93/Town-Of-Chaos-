using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Victory;
using Lotus.Victory.Conditions;
using Lotus.Extensions;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using UnityEngine;
using VentLib;
using VentLib.Options.UI;
using VentLib.Localization.Attributes;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Extensions;
using Lotus.Roles.RoleGroups.Neutral;
using Lotus.Roles;
using Lotus.GUI.Name.Holders;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Il2CppSystem.Text;
using Lotus.API;
using Lotus.API.Vanilla.Meetings;
using Lotus.API.Vanilla.Sabotages;
using Lotus;
using Lotus.Roles.Interactions;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;
using Lotus.Managers.History.Events;
using Lotus.Managers;
using VentLib.Utilities.Collections;
using Lotus.API.Player;
using Lotus.Factions.Interfaces;
using Lotus.GUI.Name.Components;
using Lotus.Roles.Interfaces;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.GUI;
using Lotus.Roles.Internals.Trackers;
using Lotus.Roles.Subroles;
using Lotus.RPC.CustomObjects.Interfaces;
using Lotus.Chat;

namespace SampleRoleAddon.Roles.Standard.NKRoles;

public class God : CustomRole, IRoleUI
{
    protected override void PostSetup()
    {
        Game.GetWinDelegate().AddSubscriber(GameEnd);
        RevealAllRoles();
    }

    private void GameEnd(WinDelegate winDelegate)
    {
        // Only trigger if God is alive and not already a solo winner
        if (!MyPlayer.IsAlive() || winDelegate.GetWinReason().ReasonType is ReasonType.SoloWinner) return;

        // God wins as a solo winner if only crewmates and God are alive
        bool onlyCrewAndGodAlive = true;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (!p.IsAlive()) continue;
            var role = p.GetCustomRoleSafe();
            if (role == null) continue;
            // Skip God and crewmates
            if (role == this) continue;
            if (!(role is Crewmate))
            {
                onlyCrewAndGodAlive = false;
                break;
            }
        }

        if (onlyCrewAndGodAlive)
        {
            // God wins as a solo winner
            winDelegate.SetWinners(new List<PlayerControl> { MyPlayer });
            winDelegate.SetWinReason(new WinReason(ReasonType.SoloWinner, "Avatar has hijacked the win!"));
            // No SetSoloWinner call, as it's not available in your framework
        }
    }

    private ChatHandler GetChatHandler() => ChatHandler.Of(title: RoleColor.Colorize(RoleName)).LeftAlign();

    [RoleAction(LotusActionType.RoundEnd)]
    private void RoundEnd()
    {
        if (MyPlayer != null && MyPlayer.IsAlive())
        {
            GetChatHandler().Message("<color=#FF0000>The</color> <color=#FF3333>Avatar</color> <color=#FF5555>is</color> <color=#FF7777>still</color><color=#FF9999> alive...</color>").Send();
        }
    }

    private void RevealAllRoles()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == MyPlayer) continue;
            var holders = player.NameModel().GCH<RoleHolder>();
            if (holders != null)
            {
                foreach (var holder in holders)
                {
                    holder?.AddViewer(MyPlayer);
                }
            }
        }
    }

    [RoleAction(LotusActionType.Chat, ActionFlag.GlobalDetector)]
    private void OnGodChat(PlayerControl chatter, string message)
    {
        // Only trigger if the sender is the God player and is alive
        if (chatter != MyPlayer || !MyPlayer.IsAlive()) return;

        message = message.ToLowerInvariant();

        // Forbidden words
        string[] forbiddenWords = { "impostor", "imp", "neutral", "crewmate", "crew", "god", "nk", "neutral killer", "killer", "bad", "kill", "shot", "shoot", "stab",
                                      "red", "blue", "green", "pink", "orange", "yellow", "black", "white", "purple", "brown", "cyan", "lime", "maroon", "rose", "banana", "gray",
                                      "vampire", "hustler", "ankle", "hostage", "sheriff", "blackmailer", "ninja", "shape","magician", "jester", "executioner","witch","medic", "doctor", "bodyguard", "pp","pickpocket", "lawyer","dracula","drac","god","avatar","sus", "evil"};
        foreach (var word in forbiddenWords)
        {
            if (message.Contains(word))
            {
                KillGod();
                return;
            }
        }

        // Check for any player's role name in the message (dead or alive)
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            var role = player.GetCustomRoleSafe();
            if (role != null)
            {
                var roleName = role.RoleName?.ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(roleName) && message.Contains(roleName))
                {
                    KillGod();
                    return;
                }
            }
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == MyPlayer || !player.IsAlive()) continue;

            if (!string.IsNullOrWhiteSpace(player.name))
            {
                string playerNameLower = player.name.ToLowerInvariant();
                // Check for any part of the player's name (split by spaces)
                var nameParts = playerNameLower.Split(' ');
                foreach (var part in nameParts)
                {
                    if (!string.IsNullOrWhiteSpace(part) && message.Contains(part))
                    {
                        KillGod();
                        return;
                    }
                }
                // Check for the full name with spaces removed
                string nameNoSpaces = playerNameLower.Replace(" ", "");
                if (!string.IsNullOrWhiteSpace(nameNoSpaces) && message.Contains(nameNoSpaces))
                {
                    KillGod();
                    return;
                }
            }

            // Check for color name (if available)
            var colorName = player.Data?.ColorName?.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(colorName) && message.Contains(colorName))
            {
                KillGod();
                return;
            }
        }
    }

    private void KillGod()
    {
        MyPlayer.Die(DeathReason.Kill, MyPlayer);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .Faction(FactionInstances.Neutral)
            .SpecialType(SpecialType.Neutral)
            .RoleColor(new Color(0.8f, 0.8f, 1f)); // Light blue for "God"
}