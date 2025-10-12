using System;
using System.Collections.Generic;
using System.Reflection; // <-- Add this for reflection
using Lotus.API.Player;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.NeutralKilling;
using UnityEngine;
using VentLib.Options.UI;
using VentLib.Utilities.Collections;
using Lotus.Roles.Interactions;
using Lotus;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.Events;
using Lotus.Roles.Internals.Enums; 
using Lotus.Utilities;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.Roles.Overrides;
using Lotus.GUI;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Victory.Conditions;
using VentLib.Localization.Attributes;
using Lotus.API.Odyssey;
using Lotus.Chat;
using VentLib.Utilities.Optionals;
using Lotus.API.Vanilla.Meetings;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Options;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using System.Linq;
using Lotus.Patches.Systems;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using Lotus.Managers.History.Events;
using SampleRoleAddon.Roles.Standard.CrewmateRoles;
using Lotus.Logging; // For Kamikaze
using Lotus.API.Vanilla.Meetings;

namespace SampleRoleAddon.Roles.Standard.NKRoles
{
    public class AnkleBitter : NeutralKillingBase
    {
        private bool dragging = false;
        private PlayerControl draggedPlayer = null;
        private Escort.BlockDelegate draggedBlock = null;
        private Remote<NameComponent> healthBarRemote = null;
        private float dragDuration = 5f;
        private float dragStartTime = 0f;
        private float healthTickInterval = 1f;
        private float lastHealthTick = 0f;
        private int draggedHealth = 100;
        private const int MaxHealth = 100;

        // Track health for each player
        private Dictionary<byte, int> playerHealths = new();

        // Custom death reason for AnkleBitter
        public static class AnkleBitterDeathNames
        {
            public const string AnkleBitter = "Nibbled";
        }

        protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
            base.RegisterOptions(optionStream)
                .SubOption(sub => sub
                    .Name("Drag Duration")
                    .BindFloat(v => dragDuration = v)
                    .AddFloatRange(1f, 10f, 1f, 2, "s")
                    .Build());

        [RoleAction(LotusActionType.Attack)]
        public override bool TryKill(PlayerControl target)
        {
            // Veteran alert check (reflection, since field is private)
            var role = target.PrimaryRole();
            if (role is Veteran veteranRole)
            {
                var field = typeof(Veteran).GetField("veteranDuration", BindingFlags.NonPublic | BindingFlags.Instance);
                var veteranDuration = field?.GetValue(veteranRole);
                if (veteranDuration != null)
                {
                    var isReadyMethod = veteranDuration.GetType().GetMethod("IsReady");
                    if (isReadyMethod != null)
                    {
                        bool isReady = (bool)isReadyMethod.Invoke(veteranDuration, null);
                        if (!isReady)
                        {
                            // Veteran is on alert, kill the AnkleBitter
                            MyPlayer.Die(DeathReason.Kill, true);
                            return false;
                        }
                    }
                }
            }

            if (dragging || target.Data.IsDead) return false;
            dragging = true;
            draggedPlayer = target;
            draggedBlock = Escort.BlockDelegate.Block(draggedPlayer, MyPlayer, -1f);
            dragStartTime = Time.time;
            lastHealthTick = Time.time;
            // Use stored health or default to MaxHealth
            draggedHealth = playerHealths.ContainsKey(target.PlayerId) ? playerHealths[target.PlayerId] : MaxHealth;
            healthBarRemote = SetupHealthBar(draggedPlayer);
            return true;
        }

        [RoleAction(LotusActionType.FixedUpdate)]
        private void DragUpdate()
        {
            if (!dragging || draggedPlayer == null || draggedPlayer.Data.IsDead)
            {
                EndDrag(false);
                return;
            }

            // Health loss over time
            if (Time.time - lastHealthTick >= healthTickInterval)
            {
                int healthLoss = Mathf.CeilToInt(MaxHealth / dragDuration);
                draggedHealth = Mathf.Max(0, draggedHealth - healthLoss);
                lastHealthTick = Time.time;
                // Remove old health display and add updated one
                if (healthBarRemote != null)
                {
                    healthBarRemote.Delete();
                    healthBarRemote = SetupHealthBar(draggedPlayer);
                }
            }

            // Teleport dragged player to AnkleBitter
            Utils.Teleport(draggedPlayer.NetTransform, MyPlayer.transform.position);

            // Check for freeing by other players
            foreach (var player in Players.GetPlayers())
            {
                if (player == MyPlayer || player == draggedPlayer || !player.IsAlive()) continue;
                if (Vector2.Distance(player.transform.position, draggedPlayer.transform.position) < 0.5f)
                {
                    // Freed! Save health and end drag
                    EndDrag(true);
                    return;
                }
            }

            // Kill if health is zero or drag duration is over
            if (draggedHealth <= 0 || Time.time - dragStartTime >= dragDuration)
            {
                Debug.Log("AnkleBitter: Dragged player's health reached 0 or drag ended, attempting kill.");
                MyPlayer.InteractWith(draggedPlayer, new LotusInteraction(
                    new FatalIntent(false, () => new CustomDeathEvent(MyPlayer, draggedPlayer, AnkleBitterDeathNames.AnkleBitter)), this));
                EndDrag(false);
                return;
            }
        }

        // EndDrag now takes a bool: true if freed, false if killed or interrupted
        private void EndDrag(bool freed)
        {
            if (draggedBlock != null)
            {
                draggedBlock.Delete();
                draggedBlock = null;
            }
            if (healthBarRemote != null)
            {
                healthBarRemote.Delete();
                healthBarRemote = null;
            }
            if (dragging && draggedPlayer != null)
            {
                if (freed)
                {
                    // Save health if freed
                    playerHealths[draggedPlayer.PlayerId] = draggedHealth;
                }
                else
                {
                    // Remove health if killed
                    playerHealths.Remove(draggedPlayer.PlayerId);
                }
            }
            dragging = false;
            draggedPlayer = null;
        }

        private Remote<NameComponent> SetupHealthBar(PlayerControl player)
        {
            var viewers = new[] { player, MyPlayer };
            return player.NameModel().GCH<NameHolder>()
                .Insert(0, new NameComponent(HealthText(), new[] { GameState.Roaming }, ViewMode.Additive, viewers));
        }

        private LiveString HealthText()
        {
            Func<string> healthText = () =>
            {
                int percent = Mathf.RoundToInt((draggedHealth / (float)MaxHealth) * 100f);
                return $"Health: {percent}%";
            };
            return new LiveString(() => $"<size=3.5><color=#FF00FF>{healthText()}</color></size>\n");
        }

        protected override RoleModifier Modify(RoleModifier roleModifier) =>
            base.Modify(roleModifier)
                .RoleColor(new Color(1f, 0f, 1f))
                .SpecialType(SpecialType.NeutralKilling)
                .Faction(FactionInstances.Neutral)
                .RoleAbilityFlags(RoleAbilityFlag.CannotSabotage | RoleAbilityFlag.IsAbleToKill);

        [RoleAction(LotusActionType.RoundEnd)]
        private void ResetDrag()
        {
            // Kill the dragged player if dragging when round ends
            if (dragging && draggedPlayer != null && !draggedPlayer.Data.IsDead)
            {
                MyPlayer.InteractWith(draggedPlayer, new LotusInteraction(
                    new FatalIntent(false, () => new CustomDeathEvent(MyPlayer, draggedPlayer, AnkleBitterDeathNames.AnkleBitter)), this));
            }
            EndDrag(false);
            playerHealths.Clear();
        }
    }
}
                