using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.Factions;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API;
using Lotus.API.Stats;
using Lotus.Extensions;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Logging;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Stock;
using Lotus.Victory.Conditions;
using Lotus.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using Lotus;
using Lotus.API.Player;
using Lotus.Roles.Events;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Overrides;
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
using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.API.Player;



namespace SampleRoleAddon.Roles.Standard.NKRoles;

public class Magician : TaskRoleBase
{
    private bool refreshTasks;
    private bool warpToTarget;
    private bool canKillAllied;

    protected override void OnTaskComplete(Optional<NormalPlayerTask> _)
    {
        if (HasAllTasksComplete && refreshTasks) Tasks.AssignAdditionalTasks(this);

        if (MyPlayer.Data.IsDead) return;
        List<PlayerControl> inRangePlayers = RoleUtils.GetPlayersWithinDistance(MyPlayer, 999, true).Where(p => canKillAllied || p.Relationship(MyPlayer) is Relation.None).ToList();
        if (inRangePlayers.Count == 0) return;
        PlayerControl target = inRangePlayers.GetRandom();
        var interaction = new RangedInteraction(new FatalIntent(!warpToTarget, () => new TaskDeathEvent(target, MyPlayer)), 0, this);

        bool death = MyPlayer.InteractWith(target, interaction) is InteractionResult.Proceed;
        Game.MatchData.GameHistory.AddEvent(new TaskKillEvent(MyPlayer, target, death));
        CheckGameWin();
    }
    
    private List<PlayerControl> AlivePlayers
{
    get
    {
        var alive = new List<PlayerControl>();
        foreach (var info in GameData.Instance.AllPlayers)
        {
            PlayerControl pc = null;
            foreach (PlayerControl candidate in PlayerControl.AllPlayerControls)
            {
                if (candidate.PlayerId == info.PlayerId)
                {
                    pc = candidate;
                    break;
                }
            }
            if (pc != null && pc.IsAlive())
                alive.Add(pc);
        }
        return alive;
    }

}

// Then update your win check:
    protected bool CheckGameWin()
{
    if (AlivePlayers.Count == 1 && AlivePlayers[0] == MyPlayer)
    {
        ManualWin.Activate(MyPlayer, ReasonType.SoloWinner, 999);
        return true; // Block other win logic
    }
    return false;
}
    

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddTaskOverrideOptions(base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Warp to Target", Translations.Options.WarpToTarget)
                .AddOnOffValues()
                .BindBool(b => warpToTarget = b)
                .Build())
            .SubOption(sub => sub.KeyName("Refresh Tasks When All Complete", Translations.Options.RefreshTasks)
                .AddOnOffValues()
                .BindBool(b => refreshTasks = b)
                .Build()));

   // protected override string ForceRoleImageDirectory() => "RoleOutfits/Imposter/crewpostor.yaml";
    
    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier
        .RoleColor(new Color(0.4f, 1.0f, 0.76f))
        .RoleAbilityFlags(RoleAbilityFlag.IsAbleToKill)
        .SpecialType(SpecialType.NeutralKilling)
        .Faction(FactionInstances.Neutral);
        

    public override List<Statistic> Statistics() => new() { VanillaStatistics.Kills };

    class TaskKillEvent : KillEvent, IRoleEvent
    {
        public TaskKillEvent(PlayerControl killer, PlayerControl victim, bool successful = true) : base(killer, victim, successful)
        {
        }

        public override string Message() => $"{Game.GetName(Player())} viciously completed his task and killed {Game.GetName(Target())}.";
    }

    class TaskDeathEvent : DeathEvent
{
    public TaskDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
    {
        // Check if killer is Magician and is now the only alive player
        
    }
}
    

    [Localized(nameof(Magician))]
    internal static class Translations
    {
        [Localized("Options")]
        internal static class Options
        {
            

            [Localized(nameof(WarpToTarget))]
            public static string WarpToTarget = "Warp To Target";

            [Localized(nameof(RefreshTasks))]
            public static string RefreshTasks = "Refresh Tasks When All Complete";
        }
    }
}