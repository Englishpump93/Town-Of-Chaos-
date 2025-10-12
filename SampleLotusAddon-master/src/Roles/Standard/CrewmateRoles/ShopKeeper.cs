using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Il2CppSystem.Text;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.API.Vanilla.Meetings;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Holders;
using Lotus.Options;
using Lotus;
using Lotus.Roles;
using Lotus.Roles.Interactions;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using VentLib.Options.UI;
using Lotus.Managers.History.Events;
using Lotus.Managers;
using VentLib.Utilities.Collections;
using Lotus.API.Player;
using Lotus.Factions.Interfaces;
using Lotus.GUI.Name.Components;
using Lotus.Roles.Interfaces;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.GUI;
using Lotus.Roles.Internals.Trackers;
using Lotus.Roles.Subroles;
using Lotus.RPC.CustomObjects.Interfaces;
using Lotus.Victory.Conditions;
using VentLib;
using VentLib.Networking.RPC.Attributes;

namespace SampleRoleAddon.Roles.Standard.CrewmateRoles;

public class ShopKeeper : Crewmate
{
    private static readonly Color PointColor = new(0.2f, 0.8f, 1f);
    private int points;
    private Dictionary<byte, ShopItem> meetingItems = new();

    // Voting system fields
    private int shopVoteStep = 0;
    private int selectedOption = -1;
    private byte selectedTarget = byte.MaxValue;

    // Track extra votes for each player (PlayerId -> extra vote count)
    private Dictionary<byte, int> extraVotes = new();

    // Track shielded players until next meeting
    private HashSet<byte> shieldedPlayers = new();

    // Track blocked voters for this meeting
    private HashSet<byte> blockedVoters = new();

    // Configurable options
    private int pointsForTask = 1;
    private int pointsForReport = 1;
    private int shieldCost = 2;
    private int revealRoleCost = 3;
    private int extraVoteCost = 2;
    private int blockVoteCost = 2;
    private bool hasArrowsToBodies = false;

    // Rope shop item options
    private int ropeCost = 3;
    private bool enableRope = true;
    private float ropeDragDuration = 5f;

    // Gun shop item options
    private int gunCost = 4;
    private bool enableGun = true;
    private HashSet<byte> playersWithGun = new();

    // Enable/disable toggles for each shop item
    private bool enableShield = true;
    private bool enableRevealRole = true;
    private bool enableExtraVote = true;
    private bool enableBlockVote = true;

    // --- Shop item name display under player names ---
    [NewOnSetup] private Dictionary<byte, Remote<TextComponent>> shopItemComponents = null!;

    // --- Rope drag system fields ---
    private bool dragging = false;
    private PlayerControl draggedPlayer = null;
    private Escort.BlockDelegate draggedBlock = null;
    private Remote<TextComponent> draggedTextRemote = null;
    private float dragStartTime = 0f;

    // --- Per-player Rope tracking ---
    private HashSet<byte> playersWithRope = new();

    public static class ShopKeeperDeathNames
    {
        public const string Gun = "Shopkeeper";
    }

    // --- Frozen players after rope drag ---
    // Changed to Dictionary to track freeze start time
    private Dictionary<byte, float> frozenPlayers = new();

    public override bool TasksApplyToTotal() => true;

    private List<ShopItem> AllItems => new List<ShopItem>
    {
        enableShield ? new ShopItem("Shield", new Color(0.8f, 0.8f, 0.2f), shieldCost, () => {/* effect handled in ApplyShopOption */}) : null,
        enableRevealRole ? new ShopItem("Reveal Role", new Color(0.5f, 0.2f, 0.8f), revealRoleCost, () => {/* effect handled in ApplyShopOption */}) : null,
        enableExtraVote ? new ShopItem("Extra Vote", new Color(0.2f, 0.8f, 0.2f), extraVoteCost, null) : null,
        enableBlockVote ? new ShopItem("Block Vote", new Color(1f, 0.2f, 0.2f), blockVoteCost, null) : null,
        enableRope ? new ShopItem("Rope", new Color(0.2f, 0.8f, 0.2f), ropeCost, null) : null,
        enableGun ? new ShopItem("Gun", new Color(0.9f, 0.9f, 0.9f), gunCost, null) : null,
    }.Where(item => item != null).ToList();

    [UIComponent(UI.Counter, gameStates: GameState.Roaming)]
    private string PointsCounter() => $"Points: {PointColor.Colorize(points.ToString())}";

    [UIComponent(UI.Text, ViewMode.Additive, GameState.Roaming)]
    private string ShopKeeperStatus()
    {
        var myId = MyPlayer.PlayerId;
        if (playersWithRope.Contains(myId))
            return "<color=yellow>You have a Rope! Use pet button to rope a player.</color>";
        if (playersWithGun.Contains(myId))
            return "<color=red>You have a Gun! Use pet button to kill one player.</color>";
        return "";
    }

    [UIComponent(UI.Indicator)]
    private string Arrows()
    {
        var arrows = "";
        if (hasArrowsToBodies)
        {
            arrows += UnityEngine.Object.FindObjectsOfType<DeadBody>()
                .Where(b => !Game.MatchData.UnreportableBodies.Contains(b.ParentId))
                .Select(b => Lotus.Roles.RoleUtils.CalculateArrow(MyPlayer, b, RoleColor)).Fuse("");
        }
        return arrows;
    }

    [UIComponent(UI.Text)]
    private string RopeDragTimeLeft()
    {
        if (!dragging) return "";
        float timeLeft = Mathf.Max(0, ropeDragDuration - (Time.time - dragStartTime));
        return $"Rope: {timeLeft:F1}s left";
    }

    // --- Pet Button Customization ---
    public RoleButton PetButton(IRoleButtonEditor editor) => editor
        .SetText("Pet");

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.KeyName("Points for Task", "Points for completing a task")
                .AddIntRange(1, 10, 1, 1)
                .BindInt(i => pointsForTask = i)
                .Build())
            .SubOption(sub => sub.KeyName("Points for Report", "Points for reporting a body")
                .AddIntRange(1, 10, 1, 1)
                .BindInt(i => pointsForReport = i)
                .Build())
            .SubOption(sub => sub.KeyName("Shield Cost", "Points required for Shield")
                .AddIntRange(1, 10, 1, 2)
                .BindInt(i => shieldCost = i)
                .Build())
            .SubOption(sub => sub.KeyName("Reveal Role Cost", "Points required for Reveal Role")
                .AddIntRange(1, 10, 1, 3)
                .BindInt(i => revealRoleCost = i)
                .Build())
            .SubOption(sub => sub.KeyName("Extra Vote Cost", "Points required for Extra Vote")
                .AddIntRange(1, 10, 1, 2)
                .BindInt(i => extraVoteCost = i)
                .Build())
            .SubOption(sub => sub.KeyName("Block Vote Cost", "Points required for Block Vote")
                .AddIntRange(1, 10, 1, 2)
                .BindInt(i => blockVoteCost = i)
                .Build())
            .SubOption(sub => sub.KeyName("Has Arrows to Bodies", "Has Arrows to Bodies")
                .AddBoolean(false)
                .BindBool(b => hasArrowsToBodies = b)
                .Build())
            .SubOption(sub => sub.KeyName("Enable Shield", "Enable Shield item")
                .AddBoolean(true)
                .BindBool(b => enableShield = b)
                .Build())
            .SubOption(sub => sub.KeyName("Enable Reveal Role", "Enable Reveal Role item")
                .AddBoolean(true)
                .BindBool(b => enableRevealRole = b)
                .Build())
            .SubOption(sub => sub.KeyName("Enable Extra Vote", "Enable Extra Vote item")
                .AddBoolean(true)
                .BindBool(b => enableExtraVote = b)
                .Build())
            .SubOption(sub => sub.KeyName("Enable Block Vote", "Enable Block Vote item")
                .AddBoolean(true)
                .BindBool(b => enableBlockVote = b)
                .Build())
            .SubOption(sub => sub.KeyName("Rope Cost", "Points required for Rope")
                .AddIntRange(1, 10, 1, 3)
                .BindInt(i => ropeCost = i)
                .Build())
            .SubOption(sub => sub.KeyName("Enable Rope", "Enable Rope item")
                .AddBoolean(true)
                .BindBool(b => enableRope = b)
                .Build())
            .SubOption(sub => sub.KeyName("Rope Drag Duration", "How long Rope drag lasts (seconds)")
                .AddFloatRange(2.5f, 30f, 2.5f, 2, "s")
                .BindFloat(f => ropeDragDuration = f)
                .Build())
            .SubOption(sub => sub.KeyName("Gun Cost", "Points required for Gun")
                .AddIntRange(1, 10, 1, 4)
                .BindInt(i => gunCost = i)
                .Build())
            .SubOption(sub => sub.KeyName("Enable Gun", "Enable Gun item")
                .AddBoolean(true)
                .BindBool(b => enableGun = b)
                .Build());

    protected override void OnTaskComplete(Optional<NormalPlayerTask> playerTask)
    {
        points += pointsForTask;
    }

    [RoleAction(LotusActionType.ReportBody)]
    private void OnReportBody(Optional<NetworkedPlayerInfo> deadPlayer)
    {
        points += pointsForReport;
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void RoundEnd()
    {
        shieldedPlayers.Clear();
        meetingItems.Clear();
        blockedVoters.Clear();
        frozenPlayers.Clear();
        playersWithGun.Clear();
        playersWithRope.Clear();

        var otherPlayers = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p.IsAlive())
            .ToList();

        var itemsPool = new List<ShopItem>(AllItems)
            .OrderBy(item =>
                    item.Name == "Gun" ? 0 :
                    item.Name == "Block Vote" ? 1 : 2)
            .ThenBy(item => item.Name)
            .ToList();

        var rand = new System.Random();
        var shuffledPlayers = otherPlayers.OrderBy(_ => rand.Next()).ToList();
        var shuffledItems = itemsPool.OrderBy(_ => rand.Next()).ToList();

        int count = Math.Min(shuffledPlayers.Count, shuffledItems.Count);
        for (int i = 0; i < count; i++)
        {
            meetingItems[shuffledPlayers[i].PlayerId] = shuffledItems[i];
        }

        UpdateShopItemDisplays();
        ResendMessages();
    }

    private void UpdateShopItemDisplays()
    {
        if (shopItemComponents != null)
        {
            foreach (var comp in shopItemComponents.Values)
                comp.Delete();
            shopItemComponents.Clear();
        }
        else
        {
            shopItemComponents = new();
        }

        foreach (var kvp in meetingItems)
        {
            var playerId = kvp.Key;
            var item = kvp.Value;
            var player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null) continue;

            string coloredString = $"<size=1.6>{item.Color.Colorize($"({item.Name} {PointColor.Colorize(item.Cost.ToString())})")}</size>";
            TextComponent textComponent = new(new LiveString(coloredString), GameState.InMeeting, viewers: MyPlayer);
            shopItemComponents[playerId] = player.NameModel().GetComponentHolder<TextHolder>().Add(textComponent);
        }
    }

    public void ResendMessages()
    {
        if (selectedTarget == byte.MaxValue)
            GetChatHandler().Message($"You are a ShopKeeper! Vote yourself to activate your shop or vote as normal.\nPoints: {PointColor.Colorize(points.ToString())}").Send(MyPlayer);
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void OnRoundStart(bool gameStart)
    {
        extraVotes.Clear();
        blockedVoters.Clear();
        frozenPlayers.Clear();

        if (gameStart)
        {
            points = 0;
            extraVotes.Clear();
            meetingItems.Clear();
            blockedVoters.Clear();
            frozenPlayers.Clear();
            playersWithGun.Clear();
            playersWithRope.Clear();
            shieldedPlayers.Clear();
        }
        if (shopItemComponents != null)
        {
            foreach (var comp in shopItemComponents.Values)
                comp.Delete();
            shopItemComponents.Clear();
        }
        RemoveRopeDrag();
    }

    [RoleAction(LotusActionType.Vote)]
    private void HandleVoting(Optional<PlayerControl> player, MeetingDelegate meetingDelegate, ActionHandle handle)
    {
        bool isSkip = !player.Exists() || player.Get().PlayerId == byte.MaxValue;

        if (shopVoteStep == 0 && player.Exists() && player.Get().PlayerId == MyPlayer.PlayerId)
        {
            shopVoteStep = 1;
            GetChatHandler().Message("Shop mode activated! Vote the player with the item you want.").Send();

            var itemList = PlayerControl.AllPlayerControls.ToArray()
                .Where(p =>p.IsAlive())
                .Select(p =>
                {
                    if (meetingItems.TryGetValue(p.PlayerId, out var item))
                        return $"{p.name}: {item.Name} ({item.Cost} points)";
                    return $"{p.name}: No item";
                });
            GetChatHandler().Message(
                $"ShopKeeper Points: {points}\nShop Items:\n" + string.Join("\n", itemList)
            ).Send();

            handle.Cancel();
            return;
        }
        if (shopVoteStep == 1)
        {
            if (isSkip)
            {
                shopVoteStep = 0;
                selectedOption = -1;
                selectedTarget = byte.MaxValue;
                GetChatHandler().Message("Shop cancelled. You may vote normally or Vote yourself to open the shop.").Send();
                handle.Cancel();
                return;
            }
            int votedIndex = GetVotedOptionIndex(player.Get());
            if (votedIndex == -1)
            {
                GetChatHandler().Message("Invalid choice. Vote again.").Send();
                handle.Cancel();
                return;
            }
            selectedOption = votedIndex;

            // Check points before proceeding to target selection
            var alivePlayers = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p.IsAlive())
                .ToList();
            var playerId = alivePlayers[selectedOption].PlayerId;
            if (!meetingItems.TryGetValue(playerId, out var item))
            {
                GetChatHandler().Message("No item found for selected player.").Send();
                selectedOption = -1;
                handle.Cancel();
                return;
            }
            if (points < item.Cost)
            {
                GetChatHandler().Message($"You do not have enough points to buy {item.Name}! You need {item.Cost} points. Vote for another item or skip to cancel shop.").Send(MyPlayer);
                selectedOption = -1;
                // Stay in shopVoteStep == 1 so player can vote again
                handle.Cancel();
                return;
            }

            shopVoteStep = 2;
            var itemName = item.Name;
            GetChatHandler().Message($"Selected item: {itemName}. Now vote who should receive this item.").Send();
            handle.Cancel();
            return;
        }
        if (shopVoteStep == 2)
        {
            if (isSkip)
            {
                shopVoteStep = 0;
                selectedOption = -1;
                selectedTarget = byte.MaxValue;
                GetChatHandler().Message("Shop cancelled. You may vote normally.").Send();
                handle.Cancel();
                return;
            }

            var alivePlayers = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p.IsAlive())
                .ToList();
            var playerId = alivePlayers[selectedOption].PlayerId;
            if (!meetingItems.TryGetValue(playerId, out var item)) return;

            // Only allow Gun or Rope to be given to yourself
            if ((item.Name == "Gun" || item.Name == "Rope") && player.Get().PlayerId != MyPlayer.PlayerId)
            {
                GetChatHandler().Message("You can only apply Gun or Rope to yourself! Vote yourself to receive this item, or skip to cancel.").Send();
                handle.Cancel();
                return; // Do NOT cancel the shop, let them vote again
            }

            selectedTarget = player.Get().PlayerId;
            ApplyShopOption(selectedOption, selectedTarget);

            shopVoteStep = 0;
            GetChatHandler().Message("Shop action applied! You may vote yourself again to buy another item, or vote normally.").Send();
            handle.Cancel();
            return;
        }
        if (shopVoteStep == 3)
        {
            if (isSkip)
            {
                shopVoteStep = 0;
                selectedOption = -1;
                selectedTarget = byte.MaxValue;
                return;
            }
            shopVoteStep = 0;
            selectedOption = -1;
            selectedTarget = byte.MaxValue;
            return;
        }
        shopVoteStep = 0;
        selectedOption = -1;
        selectedTarget = byte.MaxValue;
    }

    private int GetVotedOptionIndex(PlayerControl votedPlayer)
    {
        var players = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p.IsAlive())
            .ToList();
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].PlayerId == votedPlayer.PlayerId)
                return i;
        }
        return -1;
    }

    private void ApplyShopOption(int optionIndex, byte targetId)
    {
        var alivePlayers = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p.IsAlive())
            .ToList();

        if (optionIndex < 0 || optionIndex >= alivePlayers.Count) return;
        var playerId = alivePlayers[optionIndex].PlayerId;

        if (!meetingItems.TryGetValue(playerId, out var item))
            return;
        if (points < item.Cost)
        {
            GetChatHandler().Message($"You do not have enough points to buy {item.Name}! You need {item.Cost} points.").Send(MyPlayer);
            return;
        }

        points -= item.Cost;
        GetChatHandler().Message($"After purchase: points={points}").Send();

        var targetPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == targetId);

        if (item.Name == "Shield" && targetPlayer != null)
        {
            shieldedPlayers.Add(targetPlayer.PlayerId);
            GetChatHandler().Message($"{targetPlayer.name} is shielded until the next meeting!").Send();
        }
        if (item.Name == "Extra Vote" && targetPlayer != null)
        {
            if (!extraVotes.ContainsKey(targetPlayer.PlayerId))
                extraVotes[targetPlayer.PlayerId] = 0;
            extraVotes[targetPlayer.PlayerId]++;
        }
        if (item.Name == "Reveal Role" && targetPlayer != null)
        {
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                targetPlayer.NameModel().GCH<RoleHolder>().LastOrDefault()?.AddViewer(pc);
            }
            GetChatHandler().Message($"{targetPlayer.name}'s role has been revealed to everyone!").Send();
        }
        if (item.Name == "Block Vote" && targetPlayer != null)
        {
            blockedVoters.Add(targetPlayer.PlayerId);
            GetChatHandler().Message($"{targetPlayer.name}'s vote will not count this meeting!").Send();
        }
        if (item.Name == "Rope" && targetPlayer != null)
        {
            playersWithRope.Add(targetPlayer.PlayerId);
            GetChatHandler().Message($"You have received a Rope! Use the pet button to rope a player.").Send(targetPlayer);
        }
        if (item.Name == "Gun" && targetPlayer != null)
        {
            playersWithGun.Add(targetPlayer.PlayerId);
            GetChatHandler().Message($"You have received a Gun! Use the pet button to kill one player.").Send(targetPlayer);
        }

        item.Effect?.Invoke();

        GetChatHandler().Message($"{targetPlayer?.name ?? "Unknown"} receives: {item.Name}!").Send();
    }

    // --- Rope Drag System ---

    private void StartRopeDrag(PlayerControl target)
    {
        if (dragging || target.Data.IsDead) return;
        dragging = true;
        draggedPlayer = target;
        draggedBlock = Escort.BlockDelegate.Block(draggedPlayer, MyPlayer, -1f);
        dragStartTime = Time.time;

        string message = "You are roped! Can't kill or move freely.";
        draggedTextRemote = draggedPlayer.NameModel().GCH<TextHolder>().Add(
            new TextComponent(new LiveString(message, Color.yellow), GameState.Roaming, viewers: draggedPlayer)
        );
    }

    [RoleAction(LotusActionType.FixedUpdate)]
    private void RopeDragUpdate()
    {
        // Dragging logic
        if (dragging && draggedPlayer != null && !draggedPlayer.Data.IsDead)
        {
            if (Time.time - dragStartTime < ropeDragDuration)
            {
                Utils.Teleport(draggedPlayer.NetTransform, MyPlayer.transform.position);
            }
            else
            {
                // Drag finished, freeze if not Crewmate
                if (!(draggedPlayer.PrimaryRole() is Crewmate))
                {
                    frozenPlayers[draggedPlayer.PlayerId] = Time.time; // Store freeze start time
                }
                RemoveRopeDrag();
            }
        }

        // Freeze all frozen players in place
        foreach (var kvp in frozenPlayers.ToList())
        {
            var playerId = kvp.Key;
            var freezeStart = kvp.Value;
            var frozenPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == playerId && p.IsAlive());
            if (frozenPlayer != null)
            {
                Utils.Teleport(frozenPlayer.NetTransform, frozenPlayer.transform.position);

                // Unfreeze after 60 seconds
                if (Time.time - freezeStart >= 30f)
                {
                    frozenPlayers.Remove(playerId);
                    GetChatHandler().Message($"{frozenPlayer.name} is no longer frozen!").Send(frozenPlayer);
                    continue;
                }

                // Free if touched by a non-Crewmate
                foreach (var other in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (other == frozenPlayer || !other.IsAlive()) continue;
                    if (other.PrimaryRole() is Crewmate) continue;

                    if (Vector2.Distance(other.transform.position, frozenPlayer.transform.position) < 0.5f)
                    {
                        frozenPlayers.Remove(playerId);
                        GetChatHandler().Message($"{frozenPlayer.name} has been freed by {other.name}!").Send();
                        break;
                    }
                }
            }
        }
    }

    [RoleAction(LotusActionType.PlayerAction, ActionFlag.GlobalDetector)]
    private void BlockRopedPlayerKill(PlayerControl source, ActionHandle handle, RoleAction action)
    {
        if (dragging && source == draggedPlayer && action.ActionType == LotusActionType.Attack)
        {
            handle.Cancel();
        }
    }

    [RoleAction(LotusActionType.PlayerAction, ActionFlag.GlobalDetector)]
    private void BlockFrozenPlayerKill(PlayerControl source, ActionHandle handle, RoleAction action)
    {
        if (frozenPlayers.ContainsKey(source.PlayerId) && action.ActionType == LotusActionType.Attack)
        {
            handle.Cancel();
        }
    }

    [RoleAction(LotusActionType.RoundEnd)]
    private void ResetRopeDrag()
    {
        RemoveRopeDrag();
        playersWithRope.Clear();
        frozenPlayers.Clear();
        playersWithGun.Clear();
    }

    private void RemoveRopeDrag()
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
        dragging = false;
        draggedPlayer = null;
        dragStartTime = 0f;
    }

    // --- Pet button ability (Rope & Gun) ---

    [RoleAction(LotusActionType.OnPet)]
    public void OnPet()
    {
        var myId = MyPlayer.PlayerId;
        bool hasRope = playersWithRope.Contains(myId);
        bool hasGun = playersWithGun.Contains(myId);

        var target = MyPlayer.GetPlayersInAbilityRangeSorted()
            .FirstOrDefault(p => p != MyPlayer && p.IsAlive());

        if (hasGun)
        {
            if (target == null) return;
            playersWithGun.Remove(myId);
            MyPlayer.InteractWith(target, LotusInteraction.FatalInteraction.Create(this));
            GetChatHandler().Message($"You shot {target.name}!").Send(MyPlayer);
            return;
        }

        if (hasRope)
        {
            if (target == null) return;
            playersWithRope.Remove(myId);
            StartRopeDrag(target);
            GetChatHandler().Message($"You have roped {target.name} for {ropeDragDuration} seconds!").Send(MyPlayer);
        }
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet | RoleAbilityFlag.IsAbleToKill);

    [RoleAction(LotusActionType.Interaction, ActionFlag.GlobalDetector)]
    private void PreventKill(PlayerControl target, PlayerControl interactor, Interaction interaction, ActionHandle handle)
    {
        if (!shieldedPlayers.Contains(target.PlayerId)) return;
        if (interactor.PlayerId == MyPlayer.PlayerId) return;
        if (Game.State is not GameState.Roaming) return;

        handle.Cancel();
        GetChatHandler().Message($"{target.name} is shielded!").Send(interactor);
    }

    [RoleAction(LotusActionType.Vote, ActionFlag.GlobalDetector)]
    private void DoubleTargetVote(PlayerControl votingPlayer, Optional<PlayerControl> target, MeetingDelegate meetingDelegate, ActionHandle handle)
    {
        if (extraVotes.TryGetValue(votingPlayer.PlayerId, out int extra))
        {
            for (int i = 0; i < extra; i++)
            {
                meetingDelegate.CastVote(votingPlayer, target);
            }
            extraVotes.Remove(votingPlayer.PlayerId);
        }
    }

    [RoleAction(LotusActionType.Vote, ActionFlag.GlobalDetector)]
    private void BlockVotes(PlayerControl votingPlayer, Optional<PlayerControl> target, MeetingDelegate meetingDelegate, ActionHandle handle)
    {
        if (blockedVoters.Contains(votingPlayer.PlayerId))
        {
            int voteCount = 1;
            if (extraVotes.TryGetValue(votingPlayer.PlayerId, out int extra))
                voteCount += extra;

            if (voteCount > 1)
            {
                handle.Cancel();
                GetChatHandler().Message("Your vote has been blocked by the ShopKeeper! You cannot vote or skip this meeting.").Send(votingPlayer);
                return;
            }

            if (!target.Exists() || target.Get().PlayerId == byte.MaxValue)
            {
                return;
            }
            handle.Cancel();
            GetChatHandler().Message("Your vote has been blocked by the ShopKeeper! You can only skip.").Send(votingPlayer);
        }
    }

    private ChatHandler GetChatHandler() => ChatHandler.Of(title: RoleColor.Colorize(RoleName)).Player(MyPlayer).LeftAlign();

    private class ShopItem
    {
        public string Name;
        public Color Color;
        public int Cost;
        public Action Effect;

        public ShopItem(string name, Color color, int cost, Action effect)
        {
            Name = name;
            Color = color;
            Cost = cost;
            Effect = effect;
        }

        public string ToDisplay() => $"{Color.Colorize(Name)} ({PointColor.Colorize(Cost.ToString())})";
    }
}