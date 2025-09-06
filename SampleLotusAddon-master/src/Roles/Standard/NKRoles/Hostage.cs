using System;
using Lotus;
using Lotus.Roles;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.Interactions;
using Lotus.Roles.Events;
using UnityEngine;
using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.Roles.Internals.Enums; 
using Lotus.Utilities;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.Roles.Overrides;
using System.Collections.Generic;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Victory.Conditions;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using Lotus.API.Odyssey;
using Lotus.Chat;
using System;
using VentLib.Utilities.Optionals;
using Lotus.API.Vanilla.Meetings;
using Lotus.Extensions;
using Lotus.Factions;
//using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Options;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
//using System.Collections.Generic;
using System.Linq;
using Lotus.Patches.Systems;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;

namespace SampleRoleAddon.Roles.Standard.NKRoles
{
    public class Hostage : Impostor
    {
        private bool dragging = false;
        private PlayerControl draggedPlayer = null;
        private Escort.BlockDelegate draggedBlock = null;
        private Remote<TextComponent> draggedTextRemote = null; // <-- Added for message

        private float killCooldown = 10f;
        private float dragDuration = 5f;
        private float dragStartTime = 0f;
        private Cooldown killCd = null!;

        protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
            base.RegisterOptions(optionStream)
                .SubOption(sub => sub
                    .Name("Kill Cooldown")
                    .BindFloat(v => killCooldown = v)
                    .AddFloatRange(2.5f, 60f, 2.5f, 2, "s")
                    .Build())
                .SubOption(sub => sub
                    .Name("Drag Duration")
                    .BindFloat(v => dragDuration = v)
                    .AddFloatRange(2.5f, 30f, 2.5f, 2, "s")
                    .Build());

        protected override void PostSetup()
        {
            killCd = new Cooldown(killCooldown);
        }

        [RoleAction(LotusActionType.Attack)]
        public override bool TryKill(PlayerControl target)
        {
            if (dragging || target.Data.IsDead || killCd.NotReady()) return false;
            dragging = true;
            draggedPlayer = target;
            draggedBlock = Escort.BlockDelegate.Block(draggedPlayer, MyPlayer, -1f);
            dragStartTime = Time.time;
            killCd.Start();

            // Show message above dragged player's head
            string message = "You are a Hostage, you can't kill";
            draggedTextRemote = draggedPlayer.NameModel().GCH<TextHolder>().Add(
                new TextComponent(new LiveString(message, Color.red), GameState.Roaming, viewers: draggedPlayer)
            );
            return true;
        }

        [RoleAction(LotusActionType.FixedUpdate)]
        private void DragUpdate()
        {
            if (!dragging || draggedPlayer == null || draggedPlayer.Data.IsDead)
            {
                RemoveDragBlock();
                dragging = false;
                draggedPlayer = null;
                return;
            }
            // End drag after dragDuration seconds
            if (Time.time - dragStartTime >= dragDuration)
            {
                RemoveDragBlock();
                dragging = false;
                draggedPlayer = null;
                return;
            }
            Utils.Teleport(draggedPlayer.NetTransform, MyPlayer.transform.position);
        }

        [RoleAction(LotusActionType.VentEntered)]
        private void OnHostageVent()
        {
            if (dragging && draggedPlayer != null && draggedPlayer.IsAlive())
            {
                draggedPlayer.InteractWith(draggedPlayer, LotusInteraction.FatalInteraction.Create(this));
                RemoveDragBlock();
                dragging = false;
                draggedPlayer = null;
            }
        }

        [RoleAction(LotusActionType.PlayerAction, ActionFlag.GlobalDetector)]
        private void BlockDraggedPlayerKill(PlayerControl source, ActionHandle handle, RoleAction action)
        {
            if (dragging && source == draggedPlayer && action.ActionType == LotusActionType.Attack)
            {
                handle.Cancel(); // Prevent kill action
            }
        }

        [RoleAction(LotusActionType.RoundEnd)]
        private void ResetDrag()
        {
            RemoveDragBlock();
            dragging = false;
            draggedPlayer = null;
        }

        private void RemoveDragBlock()
        {
            if (draggedBlock != null)
            {
                draggedBlock.Delete();
                draggedBlock = null;
            }
            if (draggedTextRemote != null)
            {
                draggedTextRemote.Delete();
                draggedTextRemote = null;
            }
        }

        [UIComponent(UI.Text)]
        private string DragTimeLeft()
        {
            if (!dragging) return "";
            float timeLeft = Mathf.Max(0, dragDuration - (Time.time - dragStartTime));
            return $"Dragging: {timeLeft:F1}s left";
        }

        [UIComponent(UI.Text)]
        private string DraggedPlayerMessage()
        {
            if (!dragging || draggedPlayer == null) return "";
            // Only show for the dragged player
            return MyPlayer == draggedPlayer ? "You are a Hostage, you can't kill" : "";
        }

        protected override RoleModifier Modify(RoleModifier roleModifier) =>
            base.Modify(roleModifier)
                .RoleColor(new Color(0.97f, 1.00f, 0.40f))
                .SpecialType(SpecialType.NeutralKilling)
                .Faction(FactionInstances.Neutral)
                .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.IsAbleToKill)
                .OptionOverride(Override.KillCooldown, () => killCooldown);
    }
}