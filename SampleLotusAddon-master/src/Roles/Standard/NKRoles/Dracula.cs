using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Roles.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Options;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using Lotus.Roles.RoleGroups.NeutralKilling;

using HarmonyLib;

using Lotus.Factions;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Extensions;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities.Collections;
using Lotus.Roles;
using VentLib.Localization.Attributes;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.GUI;
using Lotus.GUI;


namespace SampleRoleAddon.Roles.Standard.NKRoles;

// The "Crew Crew" is a Crewmate role that does something.
// If "Crew Crew" successfully reports enough players (determined by host) they win the game! It's that simple
[Localized($"Roles.{nameof(Dracula)}")] // used for localization, not needed on files unless you utilize localization. You will have to go into the yaml file yourself and replace the default values.
public class Dracula: NeutralKillingBase// There are a couple built-in role types you can extend from, crewmate is one of them.
{
    

    private float killDelay;
    [NewOnSetup] private HashSet<byte> bitten = null!;

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        MyPlayer.RpcMark(target);
        InteractionResult result = MyPlayer.InteractWith(target, LotusInteraction.HostileInteraction.Create(this));
        if (result is InteractionResult.Halt) return false;

        bitten.Add(target.PlayerId);
        Game.MatchData.GameHistory.AddEvent(new BittenEvent(MyPlayer, target));

        Async.Schedule(() =>
        {
            if (!bitten.Remove(target.PlayerId)) return;
            if (!target.IsAlive()) return;
            MyPlayer.InteractWith(target, CreateInteraction(target));
        }, killDelay);

        return false;
    }

    [RoleAction(LotusActionType.RoundStart)]
    public void ResetBitten() => bitten.Clear();

    [RoleAction(LotusActionType.RoundEnd)]
    private void KillBitten()
    {
        bitten.Filter(Players.PlayerById).Where(p => p.IsAlive()).ForEach(p => MyPlayer.InteractWith(p, CreateInteraction(p)));
        bitten.Clear();
    }

    private DelayedInteraction CreateInteraction(PlayerControl target)
    {
        FatalIntent intent = new(true, () => new BittenDeathEvent(target, MyPlayer));
        return new DelayedInteraction(intent, killDelay, this);
    }

    

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Kill Delay", DraculaTranslations.Options.KillDelay)
                .Bind(v => killDelay = (float)v)
                .AddFloatRange(2.5f, 60f, 2.5f, 2, GeneralOptionTranslations.SecondsSuffix)
                .Build());

    

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.93f, 0.18f, 0.57f))
            .OptionOverride(new IndirectKillCooldown(KillCooldown))
            .IntroSound(AmongUs.GameOptions.RoleTypes.Shapeshifter);

    [Localized(nameof(Dracula))]
    public static class DraculaTranslations
    {
        [Localized(nameof(Options))]
        public static class Options
        {
            [Localized(nameof(KillDelay))]
            public static string KillDelay = "Kill Delay";
        }
    }
}