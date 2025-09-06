using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Options;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.Roles.Subroles;
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Vanilla.Meetings;
using Lotus.Roles;
using Lotus.Utilities;
using Lotus.Victory.Conditions;
using Lotus.Chat;
using System;
using Lotus.Patches.Systems;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using Lotus.Managers.History.Events;
using static Lotus.Roles.RoleGroups.Crew.Doctor.Translations;


namespace SampleRoleAddon.Roles.Standard.Modifiers;

// The "Crew Crew" is a Crewmate role that does something.
// If "Crew Crew" successfully reports enough players (determined by host) they win the game! It's that simple
[Localized($"Roles.{nameof(ChaosDoctor)}")] // used for localization, not needed on files unless you utilize localization. You will have to go into the yaml file yourself and replace the default values.
public class ChaosDoctor : Subrole
{
    [NewOnSetup] private Dictionary<byte, Remote<TextComponent>> codComponents = null!;

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void DoctorAnyDeath(PlayerControl dead, IDeathEvent causeOfDeath)
    {
        if (codComponents.ContainsKey(dead.PlayerId)) codComponents[dead.PlayerId].Delete();
        string coloredString = "<size=1.6>" + Color.white.Colorize($"({RoleColor.Colorize(causeOfDeath.SimpleName())})") + "</size>";

        TextComponent textComponent = new(new LiveString(coloredString), GameState.InMeeting, viewers: MyPlayer);
        codComponents[dead.PlayerId] = dead.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
    }
    public override string Identifier() => "Doctor";
    

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.50f, 1.00f, 0.87f));
}