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
using Lotus.API.Vanilla.Sabotages;
using Lotus.Chat;
using System;
using System.Collections.Generic;
using Lotus.Roles.Subroles;
using SampleRoleAddon.Roles.Standard.Modifiers;

namespace SampleRoleAddon.Roles.Standard.CrewmateRoles;

public class Kamikaze : Crewmate
{
    private static readonly Color kamikazeColor = new Color(0.8f, 0.2f, 0.8f);
    private Cooldown deathDelay;
    private byte killerId = byte.MaxValue;
    private NameComponent coloredName;
    private bool redNameDuringSabotage;
    private bool abilityEnabled;
    public static HashSet<Type> KamikazeBannedModifiers = new() { typeof(Bait), typeof(ChaosDemolitionist), typeof(Bewilder), typeof(Diseased) };

    protected override void Setup(PlayerControl player)
    {
        coloredName = new NameComponent(new LiveString(MyPlayer.name, kamikazeColor), GameState.Roaming, ViewMode.Absolute);
    }

    [RoleAction(LotusActionType.PlayerDeath)]
    private void DemoDeath(PlayerControl killer, Optional<FrozenPlayer> realKiller)
    {
        killer = realKiller.FlatMap(k => new UnityOptional<PlayerControl>(k.MyPlayer)).OrElse(killer);
        killerId = killer.PlayerId;

        // If the killer is Pelican, kill the Pelican instantly
        if (killer != null && killer.PrimaryRole().RoleName == "Dracula")
        {
            killer.Die(DeathReason.Kill, true);
        }
        if (killer != null && killer.PrimaryRole().RoleName == "Vampiress")
        {
            killer.Die(DeathReason.Kill, true);
        }
        if (killer != null && killer.PrimaryRole().RoleName == "Vampire")
        {
            killer.Die(DeathReason.Kill, true);
        }
        if (killer != null && killer.PrimaryRole().RoleName == "Pelican")
        {
            killer.Die(DeathReason.Kill, true);
        }

        string formatted = Translations.YouKilledKamikazeMessage.Formatted(RoleName);
        Cooldown textCooldown = deathDelay.Clone();
        textCooldown.Start();
        string Indicator() => formatted + Color.white.Colorize($" {textCooldown}s");

        Remote<TextComponent> remote = killer.NameModel().GCH<TextHolder>().Add(new TextComponent(new LiveString(Indicator, Color.red), GameState.Roaming, viewers: killer));

        Async.Schedule(() => DelayedDeath(killer), deathDelay.Duration);
    }

    private void DelayedDeath(PlayerControl killer)
    {
        killerId = byte.MaxValue;
        if (Game.State is not GameState.Roaming) return;
        if (killer.Data.IsDead || killer.inVent) return;
        killer.Die(DeathReason.Kill, true);
    }

    [RoleAction(LotusActionType.SabotageStarted, ActionFlag.GlobalDetector, priority: Lotus.API.Priority.Last)]
    private void KamikazeSabotageCheck(ISabotage sabotage, ActionHandle handle)
    {
        if (sabotage.SabotageType() != SabotageType.Lights || handle.IsCanceled) return;
        abilityEnabled = true;
        if (redNameDuringSabotage)
            MyPlayer.NameModel().GetComponentHolder<NameHolder>().Add(coloredName);
        SyncOptions();
    }

    [RoleAction(LotusActionType.SabotageFixed, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath)]
    private void KamikazeSabotageFix()
    {
        if (!abilityEnabled) return;
        abilityEnabled = false;
        if (redNameDuringSabotage)
        {
            MyPlayer.NameModel().GetComponentHolder<NameHolder>().Remove(coloredName);
            // Reset name color to white (or default)
            MyPlayer.NameModel().GetComponentHolder<NameHolder>().Add(
                new NameComponent(new LiveString(MyPlayer.name, Color.white), GameState.Roaming, ViewMode.Absolute)
            );
        }
        SyncOptions();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.CrewmateTab)
            .SubOption(sub => sub
                .KeyName("Death Delay", Translations.Options.DeathDelay)
                .BindFloat(deathDelay.SetDuration)
                .AddFloatRange(0.5f, 2f, 0.25f, 1, GeneralOptionTranslations.SecondsSuffix)
                .Build())
            .SubOption(sub => sub
                .KeyName("Show Red Name During Lights", "Show Kamikaze's name in red when lights are sabotaged")
                .BindBool(b => redNameDuringSabotage = b)
                .AddBoolean(true)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier)
    .RoleColor(new Color(0.01f, 0.99f, 0.62f));

    [Localized(nameof(Kamikaze))]
    public static class Translations
    {
        [Localized(nameof(YouKilledKamikazeMessage))]
        public static string YouKilledKamikazeMessage = "You Killed the {0}! you will die!";
        [Localized(nameof(Options))]
        public static class Options
        {
            public static string DeathDelay = "Death Delay";
        }
    }
}