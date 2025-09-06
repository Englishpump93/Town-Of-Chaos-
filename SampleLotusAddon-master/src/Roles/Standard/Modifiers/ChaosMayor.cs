using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Vanilla.Meetings;
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
using Lotus.Roles.Subroles;
using System;
using Lotus.Roles.Internals;
using Lotus.GUI.Name.Holders;
using Lotus.Patches.Systems;
using Lotus.Extensions;
using VentLib.Utilities.Extensions;
using Lotus.API.Player;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities;

using VentLib.Utilities.Optionals;
using static Lotus.Roles.RoleGroups.Crew.Mayor.Translations;

namespace SampleRoleAddon.Roles.Standard.Modifiers;

// The "Crew Crew" is a Crewmate role that does something.
// If "Chaos Mayor" successfully reports enough players (determined by host) they win the game! It's that simple
[Localized($"Roles.{nameof(ChaosMayor)}")] // used for localization, not needed on files unless you utilize localization. You will have to go into the yaml file yourself and replace the default values.
public class ChaosMayor: Subrole // There are a couple built-in role types you can extend from, crewmate is one of them.
{
    

    private int additionalVotes;

    private int totalVotes;
    private int remainingVotes;

    

    private FixedUpdateLock updateLock = new(0.25f);



    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        
    }

    
    // Removes meeting use counter component if the option is disabled
    protected override void PostSetup()
    {
        remainingVotes = totalVotes;
        
    }


    [RoleAction(LotusActionType.Vote)]
    private void ChaosMayorVotes(Optional<PlayerControl> voted, MeetingDelegate meetingDelegate, ActionHandle handle)
    {
        
        if (!voted.Exists()) return;
        for (int i = 0; i < additionalVotes; i++) meetingDelegate.CastVote(MyPlayer, voted);
    }


    
    
    public override string Identifier() => "Mayor";
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Mayor Additional Votes", Translations.Options.MayorAdditionalVotes)
                .AddIntRange(0, 10, 1, 1)
                .BindInt(i => additionalVotes = i)
                .Build());
            

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.13f, 0.30f, 0.26f));
            

    [Localized(nameof(ChaosMayor))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Button";

        

        [Localized("Options")]
        internal static class Options
        {
            [Localized(nameof(MayorAdditionalVotes))]
            public static string MayorAdditionalVotes = "Mayor Additional Votes";

        }
    }
}