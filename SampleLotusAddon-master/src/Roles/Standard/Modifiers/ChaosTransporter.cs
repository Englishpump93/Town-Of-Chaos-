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
using VentLib.Logging;
using Lotus.Logging;
using VentLib.Networking.RPC.Attributes;
using Lotus.Managers.History.Events;
using static Lotus.Roles.RoleGroups.Crew.Transporter.Translations;
using System;
using System.Collections.Generic;
using Lotus.Roles.RoleGroups.Neutral;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Impostors;

namespace SampleRoleAddon.Roles.Standard.Modifiers;

// The "Crew Crew" is a Crewmate role that does something.
// If "Crew Crew" successfully reports enough players (determined by host) they win the game! It's that simple
[Localized($"Roles.{nameof(ChaosTransporter)}")] // used for localization, not needed on files unless you utilize localization. You will have to go into the yaml file yourself and replace the default values.
public class ChaosTransporter : Subrole, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(ChaosTransporter));
    private int totalTransports;
    private int transportsRemaining;
    private TextComponent textComponent;
    public static HashSet<Type> ChaosTransporterBannedModifiers = new() { typeof(Vulture), typeof(Veteran), typeof(Ninja) };

    [UIComponent(UI.Cooldown)]
    private Cooldown transportCooldown;

    [UIComponent(UI.Counter)]
    private string RemainingTransportCounter() => RoleUtils.Counter(transportsRemaining, totalTransports);

    public RoleButton PetButton(IRoleButtonEditor editor) => editor
        .BindCooldown(transportCooldown)
        .SetText(Translations.ButtonText)
        .BindUses(() => transportsRemaining)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/transporter_transport.png", 130, true));

    protected override void PostSetup()
    {
        log.Fatal($"Total Transports: {totalTransports}");
        transportsRemaining = totalTransports;
    }

    [RoleAction(LotusActionType.OnPet)]
    public void TransportSelect(ActionHandle handle)
    {
        if (this.transportsRemaining == 0 || !transportCooldown.IsReady()) return;

        List<PlayerControl> players = Players.GetPlayers(PlayerFilter.Alive).ToList();
        if (players.Count < 2) return;

        PlayerControl target1 = players.PopRandom();
        PlayerControl target2 = players.PopRandom();

        if (target1.PlayerId == target2.PlayerId) return;

        transportCooldown.Start();
        this.transportsRemaining--;
        if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateTransporter)?.Send([MyPlayer.OwnerId], transportsRemaining);

        if (target1.inVent) target1.MyPhysics.ExitAllVents();
        if (target2.inVent) target2.MyPhysics.ExitAllVents();

        target1.MyPhysics.ResetMoveState();
        target2.MyPhysics.ResetMoveState();

        Vector2 player1Position = target1.GetTruePosition();
        Vector2 player2Position = target2.GetTruePosition();

        if (target1.IsAlive())
            Utils.Teleport(target1.NetTransform, new Vector2(player2Position.x, player2Position.y + 0.3636f));
        if (target2.IsAlive())
            Utils.Teleport(target2.NetTransform, new Vector2(player1Position.x, player1Position.y + 0.3636f));

        target1.InteractWith(target2, new TransportInteraction(target1, MyPlayer));
        target2.InteractWith(target1, new TransportInteraction(target2, MyPlayer));

        target1.moveable = true;
        target2.moveable = true;
        target1.Collider.enabled = true;
        target2.Collider.enabled = true;
        target1.NetTransform.enabled = true;
        target2.NetTransform.enabled = true;

        Game.MatchData.GameHistory.AddEvent(new TransportedEvent(MyPlayer, target1, target2));
    }
    public override string Identifier() => "Transporter";
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Number of Transports", Translations.Options.TotalTransports)
                .Bind(v => this.totalTransports = (int)v)
                .Values(4, 5, 10, 15, 20, 99).Build())
            .SubOption(sub => sub
                .KeyName("Transport Cooldown", Translations.Options.TransportCooldown)
                .Bind(v => this.transportCooldown.Duration = Convert.ToSingle((int)v))
                .Values(4, 3, 5, 10, 15, 20).Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor("#00EEFF")
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet);

    [ModRPC((uint)ModCalls.UpdateTransporter, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateTransporter(int transportsRemaining)
    {
        ChaosTransporter? transporter = PlayerControl.LocalPlayer.PrimaryRole<ChaosTransporter>();
        if (transporter == null) return;
        transporter.transportsRemaining = transportsRemaining;
        transporter.transportCooldown.Start();
    }

    private class TransportedEvent : AbilityEvent, IMultiTargetEvent
    {
        private PlayerControl target1;
        private PlayerControl target2;

        public TransportedEvent(PlayerControl user, PlayerControl target1, PlayerControl target2) : base(user)
        {
            this.target1 = target1;
            this.target2 = target2;
        }

        public List<PlayerControl> Targets() => new() { target1, target2 };

        public PlayerControl Target1() => target1;

        public PlayerControl Target2() => target2;

        public override string Message() => $"{Game.GetName(target1)} and {Game.GetName(target2)} were transported by {Game.GetName(Player())}.";
    }

    public class TransportInteraction : LotusInteraction
    {
        public PlayerControl transporter;
        public TransportInteraction(PlayerControl actor, PlayerControl transporter) : base(new NeutralIntent(), actor.PrimaryRole()) { this.transporter = transporter; }
    }

    [Localized(nameof(ChaosTransporter))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))] public static string ButtonText = "Transport";
        [Localized("Options")]
        public static class Options
        {
            [Localized(nameof(TotalTransports))]
            public static string TotalTransports = "Number of Transports";

            [Localized(nameof(TransportCooldown))]
            public static string TransportCooldown = "Transport Cooldown";
        }
    }
}