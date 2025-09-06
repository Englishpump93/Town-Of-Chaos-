using System.Collections.Generic;
using System.Linq;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using Lotus.Victory.Conditions;
using UnityEngine;
using Lotus.Roles.Internals.Enums;
using VentLib.Localization.Attributes;
using Lotus.API.Stats;
using Lotus.Factions;
using Lotus.Roles.Events;
using Lotus.API.Odyssey;
using Lotus.Victory;
using Lotus.Roles.RoleGroups.Neutral;

[Localized("Lawyer")]
public class Lawyer : CustomRole
{
    private byte clientId = byte.MaxValue; // Track by PlayerId

    protected override void PostSetup()
    {
        base.PostSetup();
        // Select a random living player as the client (not the Lawyer)
        List<PlayerControl> candidates = new List<PlayerControl>();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p != MyPlayer && !p.Data.IsDead)
                candidates.Add(p);
        }
        if (candidates.Count > 0)
        {
            PlayerControl client = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            clientId = client.PlayerId;

            // Add a star icon to the client (using NameHolder and NameComponent)
            string starText = "<color=#FFD700>★</color>"; // Gold star
            NameComponent starIcon = new NameComponent(new LiveString(starText), Game.InGameStates, ViewMode.Additive, MyPlayer);
            client.NameModel().GCH<NameHolder>().Add(starIcon);
        }

        // Subscribe to win delegate for custom win logic
        Game.GetWinDelegate().AddSubscriber(GameEnd);
    }

    [RoleAction(LotusActionType.Exiled, ActionFlag.GlobalDetector)]
    private void OnPlayerExiled(PlayerControl exiled)
    {
        if (exiled.PlayerId == clientId && !MyPlayer.Data.IsDead)
            MyPlayer.Die(DeathReason.Exile, true);
    }

    // Lawyer dies if their client is killed
    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void OnClientDeath(PlayerControl deadPlayer)
    {
        if (deadPlayer.PlayerId == clientId && !MyPlayer.Data.IsDead)
        {
            MyPlayer.Die(DeathReason.Kill, true);
        }
    }

    // Custom win logic: Lawyer and client win together if both alive at end
    private void GameEnd(WinDelegate winDelegate)
    {
        if (MyPlayer.Data.IsDead || winDelegate.GetWinReason().ReasonType == ReasonType.SoloWinner) return;

        // Find client by ID
        PlayerControl client = null;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p.PlayerId == clientId)
            {
                client = p;
                break;
            }
        }
        if (client != null && !client.Data.IsDead)
        {
            winDelegate.AddAdditionalWinner(MyPlayer);
            winDelegate.AddAdditionalWinner(client);
        }
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier
        .RoleColor(new Color(0.62f, 0.58f, 0.08f))
        .SpecialType(SpecialType.Neutral)
        .RoleFlags(RoleFlag.CannotWinAlone)
        .Faction(FactionInstances.Neutral);

    public override List<Statistic> Statistics() => new();
}