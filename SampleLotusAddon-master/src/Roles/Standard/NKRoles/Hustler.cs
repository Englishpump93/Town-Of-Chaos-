using System.Collections.Generic;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Roles;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using Lotus.Victory.Conditions;
using UnityEngine;
using Lotus.Roles.Internals.Enums;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using Lotus.API.Odyssey;
using Lotus.Chat;
using System;
using Lotus.Roles.Internals;
using VentLib.Utilities.Optionals;
using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.API.Vanilla.Meetings;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Roles.Overrides;

namespace SampleRoleAddon.Roles.Standard.NKRoles;

// The "Crew Crew" is a Crewmate role that does something.
// If "Crew Crew" successfully reports enough players (determined by host) they win the game! It's that simple
[Localized($"Roles.{nameof(Hustler)}")] // used for localization, not needed on files unless you utilize localization. You will have to go into the yaml file yourself and replace the default values.
public class Hustler : Impostor
{
    private int maximumVotes;
    private int currentVotes;
    private bool resetVotesAfterMeeting;

    [UIComponent(UI.Counter, GameStates = new[] { GameState.Roaming, GameState.InMeeting })]
    private string VoteCounter() => RoleUtils.Counter(currentVotes, color: new Color(1f, 0.45f, 0.25f));

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        bool killed = base.TryKill(target);
        if (!killed) return false;
        if (currentVotes < maximumVotes) currentVotes++;
        return true;
    }

    [RoleAction(LotusActionType.Vote)]
    private void EnhancedVote(Optional<PlayerControl> target, MeetingDelegate meetingDelegate)
    {
        for (int i = 0; i < currentVotes; i++) meetingDelegate.CastVote(MyPlayer, target);
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void ResetVoteCounter()
    {
        if (resetVotesAfterMeeting) currentVotes = 0;
    }
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
    base.Modify(roleModifier)
        .RoleColor(new Color(0.58f, 0.88f, 0.85f))
        .SpecialType(SpecialType.NeutralKilling)
        .Faction(FactionInstances.Neutral);
        
        
        
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddKillCooldownOptions(base.RegisterOptions(optionStream))
            .SubOption(sub => sub.KeyName("Maximum Additional Votes", Translations.Options.MaximumAdditionalVotes)
                .AddIntRange(1, 14, 1, 4)
                .BindInt(i => maximumVotes = i)
                .Build())
            .SubOption(sub => sub.KeyName("Reset Votes After Meetings", Translations.Options.ResetVotesAfterMeeting)
                .AddOnOffValues()
                .BindBool(b => resetVotesAfterMeeting = b)
                .Build());

    [Localized(nameof(Hustler))]
    private static class Translations
    {
        [Localized(nameof(Options))]
        public static class Options
        {
            [Localized(nameof(MaximumAdditionalVotes))]
            public static string MaximumAdditionalVotes = "Maximum Additional Votes";

            [Localized(nameof(ResetVotesAfterMeeting))]
            public static string ResetVotesAfterMeeting = "Reset Votes After Meetings";
        }
    }
}