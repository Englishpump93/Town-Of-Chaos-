using System.Text.RegularExpressions;
using System;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;
using AmongUs.Data;
using AmongUs.GameOptions;
using System.Reflection;
using System.Text.Json;
using TownOfHost.PrivateExtensions;
using TownOfHost.Roles;
using Assets.CoreScripts;
using Il2CppSystem.Collections;
using JetBrains.Annotations;
using TargetException = Il2CppSystem.Reflection.TargetException;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            //Logger.Info($"SystemTypes:{type}", "IsActive");
            var mapId = Main.NormalOptions.MapId;
            switch (type)
            {
                case SystemTypes.Electrical:
                    {
                        if (mapId == 5) return false;
                        var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                        return SwitchSystem != null && SwitchSystem.IsActive;
                    }
                case SystemTypes.Reactor:
                    {
                        if (mapId == 2) return false;
                        else if (mapId == 4)
                        {
                            return IsActive(SystemTypes.HeliSabotage);
                        }
                        else
                        {
                            var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                            return ReactorSystemType != null && ReactorSystemType.IsActive;
                        }
                    }
                case SystemTypes.Laboratory:
                    {
                        if (mapId != 2) return false;
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                case SystemTypes.LifeSupp:
                    {
                        if (mapId is 2 or 4) return false;
                        var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                        return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                    }
                case SystemTypes.Comms:
                    {
                        if (mapId is 1 or 5)//Mira & Fungle
                        {
                            var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                            return HqHudSystemType != null && HqHudSystemType.IsActive;
                        }
                        else
                        {
                            var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                            return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                        }

                    }
                case SystemTypes.HeliSabotage:
                    var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                    return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                default:
                    return false;
            }
        }
        public static string ReplaceCharWithSpace(string text, string replace)
        {
            string returned = "";
            string[] seperate = text.Split(replace);
            string last = seperate[seperate.Length];
            foreach (var t in seperate)
            {
                returned += t;
                if (t != last)
                    returned += " ";
            }
            return returned;
        }
        public static string ReplaceSpaceWithChar(string text, string replace)
        {
            string returned = "";
            string[] seperate = text.Split(" ");
            string last = seperate[seperate.Length];
            foreach (var t in seperate)
            {
                returned += t;
                if (t != last)
                    returned += replace;
            }
            return returned;
        }
        public static void SetVision(this NormalGameOptionsV07 opt, PlayerControl player, bool HasImpVision)
        {
            if (HasImpVision)
            {
                opt.CrewLightMod = opt.ImpostorLightMod;
                if (IsActive(SystemTypes.Electrical))
                    opt.CrewLightMod *= 5;
                return;
            }
            else
            {
                opt.ImpostorLightMod = opt.CrewLightMod;
                if (IsActive(SystemTypes.Electrical))
                    opt.ImpostorLightMod /= 5;
                return;
            }
        }
        public static string GetOnOff(bool value) => value ? "ON" : "OFF";
        public static int SetRoleCountToggle(int currentCount) => currentCount > 0 ? 0 : 1;
        public static bool ContainsStart(this string text)
        {
            if (text == "Start") return true;
            if (text == "start") return true;
            if (text.Contains("started")) return false;
            if (text.Contains("starter")) return false;
            if (text.Contains("Starting")) return false;
            if (text.Contains("starting")) return false;
            if (text.Contains("beginner")) return false;
            if (text.Contains("beginned")) return false;
            if (text.Contains("start")) return true;
            if (text.Contains("s t a r t")) return true;
            if (text.Contains("begin")) return true;
            return false;
        }

        public static void SetRoleCountToggle(CustomRoles role)
        {
            int count = Options.GetRoleCount(role);
            count = SetRoleCountToggle(count);
            Options.SetRoleCount(role, count);
        }
        public static string GetRoleName(CustomRoles role)
        {
            var CurrentLanguage = TranslationController.Instance.currentLanguage.languageID;
            var lang = CurrentLanguage;
            if (Main.ForceJapanese.Value && Main.JapaneseRoleName.Value)
                lang = SupportedLangs.Japanese;
            else if (CurrentLanguage == SupportedLangs.Japanese && !Main.JapaneseRoleName.Value)
                lang = SupportedLangs.English;
            return GetRoleName(role, lang);
        }
        public static string GetRoleName(CustomRoles role, SupportedLangs lang)
        {
            return GetString(Enum.GetName(typeof(CustomRoles), role), lang);
        }
        public static string GetDeathReason(PlayerState.DeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(PlayerState.DeathReason), status));
        }
        public static AttackEnum GetAttackEnum(CustomRoles role)
        {
            if (!Main.attackValues.TryGetValue(role, out var power)) power = AttackEnum.None;
            var attack = power;
            return attack;
        }
        public static void CallMeeting()
        {
            if (!PlayerControl.LocalPlayer.Data.IsDead)
            {
                PlayerControl.LocalPlayer.CmdReportDeadBody(null);
            }
            else
            {
                bool reported = false;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data.IsDead || pc.Data.Disconnected || reported) continue;
                    if (!reported)
                        pc.CmdReportDeadBody(null);
                }
            }
        }
        public static bool IsActiveDontOpenMeetingSabotage(out SystemTypes sabotage)
        {
            sabotage = SystemTypes.Admin;
            SystemTypes[] Sabotage = { SystemTypes.Electrical, SystemTypes.Comms,
            SystemTypes.Reactor, SystemTypes.Laboratory,
            SystemTypes.LifeSupp,  SystemTypes.HeliSabotage };

            foreach (SystemTypes type in Sabotage)
            {
                if (IsActive(type))
                {
                    sabotage = type;
                    return true;
                }
            }

            return false;
        }
        public static void BeginRoundReview()
        {
            SendMessage("NOTE: Any commands ran during this period will have their outputs after the review.");
            Main.MessageWait.Value = 4;
            var time = (Main.DeadPlayersThisRound.Count * 20) + 5;
            foreach (var player in Main.DeadPlayersThisRound)
            {
                var pc = GetPlayerById(player);
                SendMessage($"{pc.GetRealName(true)} died last round."); // NAME
                try
                {
                    SendMessage(pc.GetDeathMessage()); // Actual Death Reason
                }
                catch (Exception ex)
                {
                    Logger.SendInGame($"Error loading death reason.\n{ex}");
                    var deathReasonFound = PlayerState.deathReasons.TryGetValue(pc.PlayerId, out var deathReason);
                    var reason = deathReasonFound ? GetString("DeathReason." + deathReason.ToString()) : "No Death Reason Found";
                    SendMessage($"We could not determine their role. Their death reason is {reason}");
                }
                SendMessage($"We could not find a last will."); // NO LAST WILL
                if (Main.unreportableBodies.Contains(player)) // DONT DISPLAY ROLE IF CLEANED
                    SendMessage($"Their role is uninterpertable.");
                else
                    SendMessage($"{pc.GetRealName(true)} was a/an {GetRoleName(pc.GetCustomRole())}.");
            }
        }
        public static DefenseEnum GetDefenseEnum(CustomRoles role)
        {
            if (!Main.defenseValues.TryGetValue(role, out var power)) power = DefenseEnum.None;
            var defense = power;
            return defense;
        }
        public static Color GetRoleColor(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static Color GetHexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
        public static string GetRoleColorCode(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            return hexColor;
        }
        public static (string, Color) GetRoleText(PlayerControl player)
        {
            string RoleText = "Invalid Role";
            Color TextColor = Color.red;

            var cRole = player.GetCustomRole();
           
            RoleText = GetRoleName(cRole);

            return (RoleText, GetRoleColor(cRole));
        }
        public static string GetVitalTextDoc(byte player) =>
            PlayerState.isDead[player] | GetPlayerById(player).Data.IsDead ? GetString("DeathReason." + PlayerState.GetDeathReason(player)) : GetString("Alive");
        
        public static string GetVitalText(byte player) =>
            PlayerState.isDead[player] | GetPlayerById(player).Data.IsDead ? GetString("DeathReason." + PlayerState.GetDeathReason(player)) : GetString("Alive");
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRoles hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                case RoleTypes.Shapeshifter:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                default:
                    switch (hRole)
                    {
                        case CustomRoles.Crewmate:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                        case CustomRoles.HASFox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case CustomRoles.HASTroll:
                            text = "Troll";
                            color = Color.green;
                            break;
                    }
                    break;
            }
            return (text, color);
        }

        public static bool HasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
        {
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;

            var hasTasks = true;
            if (p.Disconnected) hasTasks = false;
            if (p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (Options.CurrentGameMode() == CustomGameMode.HideAndSeek)
            {
                if (p.IsDead && !Options.SplatoonOn.GetBool()) hasTasks = false;
                var hasRole = Main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var role);
                if (hasRole)
                {
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll or CustomRoles.Painter or CustomRoles.Janitor) hasTasks = false;
                }
            }
            else
            {
                var cRoleFound = Main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var cRole);
                if (CustomRolesHelper.IsCoven(p.GetCustomRole())) hasTasks = false;
                if (cRoleFound)
                {
                    if (cRole == CustomRoles.GM) hasTasks = false;
                    if (cRole == CustomRoles.Jester) hasTasks = false;
                    if (cRole == CustomRoles.MadGuardian && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.MadSnitch && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Opportunist) hasTasks = false;
                    if (cRole == CustomRoles.Sellout) hasTasks = true;
                    if (cRole == CustomRoles.Chancer) hasTasks = true;
                    if (cRole == CustomRoles.Survivor && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Sheriff) hasTasks = false;
                    if (cRole == CustomRoles.Escort) hasTasks = false;
                    if (cRole == CustomRoles.Crusader) hasTasks = false;
                    if (cRole == CustomRoles.CorruptedSheriff) hasTasks = false;
                    if (cRole == CustomRoles.Investigator) hasTasks = false;
                    if (cRole == CustomRoles.Amnesiac && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Amnesiac && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Madmate) hasTasks = false;
                    if (cRole == CustomRoles.SKMadmate) hasTasks = false;
                    if (cRole == CustomRoles.Terrorist && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Executioner) hasTasks = false;
                    if (cRole == CustomRoles.Impostor) hasTasks = false;
                    if (cRole == CustomRoles.PoisonMaster) hasTasks = false;
                    if (cRole == CustomRoles.Shapeshifter) hasTasks = false;
                    if (cRole == CustomRoles.AgiTater) hasTasks = false;
                    if (cRole == CustomRoles.Arsonist) hasTasks = false;
                    if (cRole == CustomRoles.Parasite) hasTasks = false;
                    if (cRole == CustomRoles.NeutWitch) hasTasks = false;
                    if (cRole == CustomRoles.SchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.CSchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.MSchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.EgoSchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.JSchrodingerCat) hasTasks = false;
                    if (cRole == CustomRoles.Egoist) hasTasks = false;
                    if (cRole == CustomRoles.Jackal) hasTasks = false;
                    if (cRole == CustomRoles.Sidekick) hasTasks = false;
                    if (cRole == CustomRoles.Juggernaut) hasTasks = false;
                    if (cRole == CustomRoles.PlagueBearer) hasTasks = false;
                    if (cRole == CustomRoles.Pestilence) hasTasks = false;
                    if (cRole == CustomRoles.Coven) hasTasks = false;
                    if (cRole == CustomRoles.Vulture) hasTasks = false;
                    if (cRole == CustomRoles.GuardianAngelTOU) hasTasks = false;
                    if (cRole == CustomRoles.Lawyer) hasTasks = true;
                    if (cRole == CustomRoles.Werewolf) hasTasks = false;
                    if (cRole == CustomRoles.TheGlitch) hasTasks = false;
                    if (cRole == CustomRoles.Hacker) hasTasks = false;
                    if (cRole == CustomRoles.Swapper) hasTasks = false;
                    if (cRole == CustomRoles.BloodKnight) hasTasks = false;
                    if (cRole == CustomRoles.Marksman) hasTasks = false;
                    if (cRole == CustomRoles.Pirate) hasTasks = false;
                    if (cRole == CustomRoles.Hitman) hasTasks = false;
                    if (cRole == CustomRoles.Dracula) hasTasks = false;
                    if (cRole == CustomRoles.Hustler) hasTasks = false;
                    if (cRole == CustomRoles.Magician && p.IsDead) hasTasks = false; //magician fix done
                    if (cRole == CustomRoles.Magician && ForRecompute) hasTasks = true;
                    if (cRole == CustomRoles.MGSchrodingerCat && p.IsDead) hasTasks = false; //magician fix done
                    if (cRole == CustomRoles.MGSchrodingerCat && ForRecompute) hasTasks = true;
                    if (cRole == CustomRoles.CPSchrodingerCat && p.IsDead) hasTasks = false; //Crewposter fix done
                    if (cRole == CustomRoles.CPSchrodingerCat && ForRecompute) hasTasks = true;
                    
                    if (cRole == CustomRoles.Reviver && ForRecompute) hasTasks = true;
                    
                    if (cRole == CustomRoles.CrewPostor && p.IsDead) hasTasks = false;
                    if (cRole == CustomRoles.CrewPostor && ForRecompute) hasTasks = true;
                    
                    if (cRole == CustomRoles.Phantom && p.IsDead) hasTasks = false;
                    if (cRole == CustomRoles.Phantom && ForRecompute) hasTasks = false;

                    if (cRole == CustomRoles.CovenWitch) hasTasks = false;
                    if (cRole == CustomRoles.HexMaster) hasTasks = false;
                    if (cRole == CustomRoles.PotionMaster) hasTasks = false;
                    if (cRole == CustomRoles.Medusa) hasTasks = false;
                    if (cRole == CustomRoles.Mimic) hasTasks = false;
                    if (cRole == CustomRoles.Conjuror) hasTasks = false;
                    if (cRole == CustomRoles.Necromancer) hasTasks = false;
                    if (cRole == CustomRoles.Poisoner) hasTasks = false;
                }
                var cSubRoleFound = Main.AllPlayerCustomSubRoles.TryGetValue(p.PlayerId, out var cSubRole);
                if (cSubRoleFound)
                {

                }
            }
            return hasTasks;
        }

        public static string GetProgressText(PlayerControl pc)
        {
            if (!Main.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
            var taskState = pc.GetPlayerTaskState();
            var Comms = false;
            if (taskState.hasTasks)
            {
                foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
                    if (task.TaskType == TaskTypes.FixComms)
                    {
                        Comms = true;
                        break;
                    }
            }
            return GetProgressText(pc.PlayerId, Comms);
        }
        public static string GetProgressText(byte playerId, bool comms = false)
        {
            if (!Main.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
            CustomRoles realRole = CustomRoles.eevee;
            if (!Main.AllPlayerCustomRoles.TryGetValue(playerId, out var role))
            {
                if (!Main.LastPlayerCustomRoles.TryGetValue(playerId, out var roled))
                    return Helpers.ColorString(Color.yellow, "(no role found)");
                else
                    realRole = roled;
            }
            else
            {
                realRole = role;
            }
            string ProgressText = "";
            bool checkTasks = false;
            switch (realRole)
            {
                case CustomRoles.Arsonist:
                    var doused = GetDousedPlayerCount(playerId);
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Arsonist), $"({doused.Item1}/{doused.Item2})");
                    break;
                case CustomRoles.HexMaster:
                    var hexed = GetHexedPlayerCount(playerId);
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Coven), $"({hexed.Item1}/{hexed.Item2})");
                    break;
                case CustomRoles.PlagueBearer:
                    var infected = GetInfectedPlayerCount(playerId);
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Pestilence), $"({infected.Item1}/{infected.Item2})");
                    break;
                case CustomRoles.Sheriff:
                    ProgressText += Sheriff.GetShotLimit(playerId);
                    break;
                case CustomRoles.Transporter:
                    ProgressText += Helpers.ColorString(GetRoleColor(CustomRoles.Transporter), $"({Main.TransportsLeft})");
                    checkTasks = true;
                    break;
                case CustomRoles.NeutWitch:
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.NeutWitch), $"({Options.NumOfWitchesPerRound.GetInt() - Main.WitchesThisRound})");
                    break;
                case CustomRoles.Survivor:
                    var stuff = Main.SurvivorStuff[playerId];
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Survivor), $"({stuff.Item1}/{Options.NumOfVests.GetInt()})");
                    break;
                case CustomRoles.Pirate:
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Pirate), $"({Guesser.PirateGuess[playerId]}/{Guesser.PirateGuessAmount.GetInt()})");
                    break;
                case CustomRoles.Veteran:
                    ProgressText += Helpers.ColorString(GetRoleColor(CustomRoles.Veteran), $"({Main.VetAlerts}/{Options.NumOfVets.GetInt()})");
                    checkTasks = true;
                    break;
                case CustomRoles.GuardianAngelTOU:
                    ProgressText += Helpers.ColorString(GetRoleColor(CustomRoles.GuardianAngelTOU), $"({Main.ProtectsSoFar}/{Options.NumOfProtects.GetInt()})");
                    break;
                case CustomRoles.Sniper:
                    ProgressText += $"{Sniper.GetBulletCount(playerId)}";
                    break;
                case CustomRoles.Vulture:
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Vulture), $"({Main.AteBodies}/{Options.BodiesAmount.GetInt()})");
                    break;
                case CustomRoles.Hacker:
                    ProgressText = Helpers.ColorString(GetRoleColor(CustomRoles.Hacker), $"({Main.HackerFixedSaboCount[playerId]}/{Options.SaboAmount.GetInt()})");
                    break;
                case CustomRoles.Postman:
                    ProgressText += $"{Postman.GetProgressText(playerId)}";
                    checkTasks = true;
                    break;
                case CustomRoles.Tasker:
                    checkTasks = true;
                    var taskState = PlayerState.taskState?[playerId];
                    if (taskState.CompletedTasksCount == taskState.AllTasksCount)
                        Main.KillingSpree.Add(playerId);
                    break;
                default:
                    if (realRole is CustomRoles.Jackal or CustomRoles.Hitman or CustomRoles.Amnesiac or CustomRoles.Hitman or CustomRoles.AgiTater) break;
                    checkTasks = true;
                    break;
            }
            if (checkTasks)
            {
                var taskState = PlayerState.taskState?[playerId];
                if (taskState.hasTasks)
                {
                    string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                    ProgressText += Helpers.ColorString(Color.yellow, $"({Completed}/{taskState.AllTasksCount})");
                    if (realRole == CustomRoles.Tasker && !GetPlayerById(playerId).Data.IsDead)
                    {
                        if (ProgressText == Helpers.ColorString(Color.yellow, $"({taskState.AllTasksCount}/{taskState.AllTasksCount})"))
                            Main.KillingSpree.Add(playerId);
                    }
                    else if (realRole == CustomRoles.Postman)
                    {
                        Postman.OnTaskComplete(playerId, taskState);
                    }
                    else if (realRole == CustomRoles.CrewPostor && !GetPlayerById(playerId).Data.IsDead)
                    {
                        int amount = Main.lastAmountOfTasks[playerId];

                        if (taskState.CompletedTasksCount != amount) // new task completed //
                        {
                            Main.lastAmountOfTasks[playerId] = taskState.CompletedTasksCount;
                            var cp = GetPlayerById(playerId);
                            if (!cp.Data.IsDead)
                            {
                                Vector2 cppos = cp.transform.position;//呪われた人の位置
                                Dictionary<PlayerControl, float> cpdistance = new();
                                float dis;
                                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                                {
                                    if (!p.Data.IsDead && p != cp && !p.Is(CustomRoles.CPSchrodingerCat))
                                    {
                                        dis = Vector2.Distance(cppos, p.transform.position);
                                        cpdistance.Add(p, dis);
                                        Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "CrewPostor");
                                    }
                                }
                                var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                                PlayerControl targetw = min.Key;
                                Logger.Info($"{targetw.GetNameWithRole()}was killed", "CrewPostor");
                                if (targetw.Is(CustomRoles.Pestilence))
                                    targetw.RpcMurderPlayerV2(cp);
                                else if (targetw.Is(CustomRoles.SchrodingerCat))
                                {
                                    targetw.RpcSetCustomRole(CustomRoles.CPSchrodingerCat);
                                    NameColorManager.Instance.RpcAdd(cp.PlayerId, targetw.PlayerId, $"{GetRoleColorCode(CustomRoles.SchrodingerCat)}");
                                    NotifyRoles();
                                    CustomSyncAllSettings();
                                }
                                else
                                    cp.RpcMurderPlayerV2(targetw);
                                cp.RpcGuardAndKill(cp);
                            }
                        }
                    }
                    else if (realRole == CustomRoles.Magician && !GetPlayerById(playerId).Data.IsDead)
                    {
                        int amount = Main.lastAmountOfTasks[playerId];

                        if (taskState.CompletedTasksCount != amount) // new task completed
                        {
                            Main.lastAmountOfTasks[playerId] = taskState.CompletedTasksCount;
                            var mg = GetPlayerById(playerId);
                            if (!mg.Data.IsDead)
                            {
                                Vector2 cppos = mg.transform.position; // Position of the cursed player
                                Dictionary<PlayerControl, float> cpdistance = new();
                                float dis;
                                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                                {
                                    if (!p.Data.IsDead && p != mg && !p.Is(CustomRoles.MGSchrodingerCat))
                                    {
                                        dis = Vector2.Distance(cppos, p.transform.position);
                                        cpdistance.Add(p, dis);
                                        Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Magician");
                                    }
                                }
                                var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault(); // Get the closest player
                                PlayerControl targetw = min.Key;
                                Logger.Info($"{targetw.GetNameWithRole()}was killed", "Magician");

                                if (targetw.Is(CustomRoles.Pestilence))
                                    targetw.RpcMurderPlayerV2(mg);
                                else if (targetw.Is(CustomRoles.SchrodingerCat))
                                {
                                    targetw.RpcSetCustomRole(CustomRoles.MGSchrodingerCat);
                                    NameColorManager.Instance.RpcAdd(mg.PlayerId, targetw.PlayerId, $"{GetRoleColorCode(CustomRoles.SchrodingerCat)}");
                                    NotifyRoles();
                                    CustomSyncAllSettings();
                                }
                                else if (!mg.Data.IsDead) // Check if the Magician is alive
                                {
                                    mg.RpcMurderPlayerV2(targetw);
                                    mg.RpcGuardAndKill(mg);
                                }
                            }
                        }
                    }
                    //reborn cp
                    else if (realRole == CustomRoles.Satan && !GetPlayerById(playerId).Data.IsDead)
                    {
                        int amount = Main.lastAmountOfTasks[playerId];

                        if (taskState.CompletedTasksCount != amount) // new task completed //
                        {
                            Main.lastAmountOfTasks[playerId] = taskState.CompletedTasksCount;
                            var mg = GetPlayerById(playerId);
                            if (!mg.Data.IsDead)
                            {
                                Vector2 cppos = mg.transform.position;//呪われた人の位置
                                Dictionary<PlayerControl, float> cpdistance = new();
                                float dis;
                                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                                {
                                    if (!p.Data.IsDead && p != mg && !p.Is(CustomRoles.RBSchrodingerCat))
                                    {
                                        dis = Vector2.Distance(cppos, p.transform.position);
                                        cpdistance.Add(p, dis);
                                        Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Satan");
                                    }
                                }
                                var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                                PlayerControl targetw = min.Key;
                                Logger.Info($"{targetw.GetNameWithRole()}was killed", "Satan");
                                if (targetw.Is(CustomRoles.Pestilence))
                                    targetw.RpcMurderPlayerV2(mg);
                                else if (targetw.Is(CustomRoles.SchrodingerCat))
                                {
                                    targetw.RpcSetCustomRole(CustomRoles.RBSchrodingerCat);
                                    NameColorManager.Instance.RpcAdd(mg.PlayerId, targetw.PlayerId, $"{GetRoleColorCode(CustomRoles.SchrodingerCat)}");
                                    NotifyRoles();
                                    CustomSyncAllSettings();
                                }
                                else
                                    mg.RpcMurderPlayerV2(targetw);
                                mg.RpcGuardAndKill(mg);
                            }
                        }
                    }
                    else if (realRole == CustomRoles.Phantom && !GetPlayerById(playerId).Data.IsDead)
                    {
                        int amount = Main.lastAmountOfTasks[playerId];
                        int remaining = taskState.AllTasksCount - taskState.CompletedTasksCount;

                        if (taskState.CompletedTasksCount != amount) // new task completed //
                        {
                            Main.lastAmountOfTasks[playerId] = taskState.CompletedTasksCount;
                            if (taskState.CompletedTasksCount == taskState.AllTasksCount)
                            {
                                // PHANTOM WINS //
                                var phantom = GetPlayerById(playerId);
                                phantom.RpcMurderPlayer(phantom, true);
                                PlayerState.SetDeathReason(playerId, PlayerState.DeathReason.Alive);
                                var endReason = TempData.LastDeathReason switch
                                {
                                    DeathReason.Exile => GameOverReason.ImpostorByVote,
                                    DeathReason.Kill => GameOverReason.ImpostorByKill,
                                    _ => GameOverReason.ImpostorByVote,
                                };
                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                                writer.Write((byte)CustomWinner.Phantom);
                                writer.Write(playerId);
                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                RPC.PhantomWin(playerId);
                                EndGameHelper.AssignWinner(playerId);
                                new LateTask(() =>
                                {
                                    GameManager.Instance.RpcEndGame(endReason, false);
                                }, 0.5f, "EndGameTaskForPhantom");
                            }
                            if (remaining <= Options.TasksRemainingForPhantomClicked.GetInt() && !Main.PhantomCanBeKilled)
                            {
                                Main.PhantomCanBeKilled = true;
                            }
                            if (remaining <= Options.TasksRemaningForPhantomAlert.GetInt() && !Main.PhantomAlert)
                            {
                                Main.PhantomAlert = true;
                            }
                        }
                    }
                }
            }
            if (role.IsImpostor() && role != CustomRoles.LastImpostor && GetPlayerById(playerId).IsLastImpostor())
            {
                ProgressText += $" <color={GetRoleColorCode(CustomRoles.Impostor)}>(Last)</color>";
            }
            if (GetPlayerById(playerId).CanMakeMadmate()) ProgressText += $" [{Options.CanMakeMadmateCount.GetInt() - Main.SKMadmateNowCount}]";

            if (Main.GuardianAngelTarget.Count != 0)
                foreach (var TargetGA in Main.GuardianAngelTarget)
                {
                    if (Options.TargetKnowsGA.GetBool())
                    {
                        if (playerId == TargetGA.Value)
                            ProgressText += $"<color={Utils.GetRoleColorCode(CustomRoles.GuardianAngelTOU)}>♦</color>";
                    }
                }
            //Client Knows Laywer
            if (Main.LawyerTarget.Count != 0)
                foreach (var TargetLW in Main.LawyerTarget)
                {
                    if (Options.TargetKnowsLawyer.GetBool())
                    {
                        if (playerId == TargetLW.Value)
                            ProgressText += $"<color={Utils.GetRoleColorCode(CustomRoles.Lawyer)}>♦</color>";
                    }
                }
            return ProgressText == "Invalid" ? "" : ProgressText;
        }
        public static void ShowActiveSettingsHelp()
        {
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":");
            if (Options.CurrentGameMode() == CustomGameMode.HideAndSeek)
            {
                if (!Options.SplatoonOn.GetBool())
                {
                    SendMessage(GetString("HideAndSeekInfo"));
                    if (CustomRoles.HASFox.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASFox) + GetString("HASFoxInfoLong")); }
                    if (CustomRoles.HASTroll.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASTroll) + GetString("HASTrollInfoLong")); }
                }
                else
                {
                    //SendMessage(GetString("HideAndSeekInfo"));
                    if (CustomRoles.Supporter.IsEnable()) { SendMessage(GetRoleName(CustomRoles.Supporter) + GetString("SupporterInfoLong")); }
                    if (CustomRoles.Janitor.IsEnable()) { SendMessage(GetRoleName(CustomRoles.Janitor) + GetString("JanitorInfoLong")); }
                }
            }
            else
            {
                if (Options.DisableDevices.GetBool()) { SendMessage(GetString("DisableDevicesInfo")); }
                if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo")); }
                if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo")); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo")); }
                if (Options.IsStandardHAS) { SendMessage(GetString("StandardHASInfo")); }
                if (Options.CamoComms.GetBool()) { SendMessage(GetString("CamoCommsInfo")); }
                if (Options.EnableGM.GetBool()) { SendMessage(GetRoleName(CustomRoles.GM) + GetString("GMInfoLong")); }
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    if (role.IsEnable() && !role.IsVanilla()) SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"));
                }
                if (Options.EnableLastImpostor.GetBool()) { SendMessage(GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong")); }
            }
            if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo")); }
        }
        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
            var text = "";
            if (Options.CurrentGameMode() == CustomGameMode.HideAndSeek)
            {
                text = GetString("Roles") + ":";
                if (CustomRoles.HASFox.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                text += Main.versionText;
                text += GetString("HideAndSeek");
            }
            else if (Options.CurrentGameMode() == CustomGameMode.ColorWars)
            {
                text = GetString("Roles") + ":";
                if (CustomRoles.HASFox.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                text += String.Format("\n\n{0}:{1}", "Current Game Mode", Options.GameMode.GetString());
                text += Main.versionText;
                text += GetString("ColorWars");
            }
            else if (Options.CurrentGameMode() == CustomGameMode.Splatoon)
            {
                text = GetString("Roles") + ":";
                if (CustomRoles.HASFox.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                text += String.Format("\n\n{0}:{1}", "Current Game Mode", Options.GameMode.GetString());
                text += Main.versionText;
                text += GetString("Splatoon");
            }
            else
            {
                ShowActiveRoles(PlayerId);
                text = GetString("Attributes") + ":";
                if (Options.EnableLastImpostor.GetBool())
                {
                    text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.LastImpostor), GetOnOff(Options.EnableLastImpostor.GetBool()));
                }
                if (Options.CamoComms.GetBool()) text += String.Format("\n{0}:{1}", GetString("CamoComms"), GetOnOff(Options.CamoComms.GetBool()));
                if (Options.CurrentGameMode() == CustomGameMode.Standard)
                {
                    
                    text += String.Format("\n{0}:{1}", "Min Neutral Killings", Options.MinNK.GetString());
                    text += String.Format("\n{0}:{1}", "Max Neutral Killings", Options.MaxNK.GetString());
                    text += String.Format("\n{0}:{1}", "Min Neutral Non-Killings", Options.MinNonNK.GetString());
                    text += String.Format("\n{0}:{1}", "Max Neutral Non-Killings", Options.MaxNonNK.GetString());
                    text += String.Format("\n{0}:{1}", "Impostors know the Roles of their Team", Options.ImpostorKnowsRolesOfTeam.GetString());
                    text += String.Format("\n{0}:{1}", "Coven knows the Roles of their Team", Options.CovenKnowsRolesOfTeam.GetString());
                }
                text += String.Format("\n\n{0}:{1}", "Current Game Mode", Options.GameMode.GetString());
                text += String.Format("\n{0}:{1}", "Players have Access to /color,/name, and /level", GetOnOff(Options.Customise.GetBool()));
                text += String.Format("\n{0}:{1}", "Roles look Similar to ToU", GetOnOff(Options.RolesLikeToU.GetBool()));
                text += Main.versionText;
                //Roles look Similar to ToU
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                /* foreach (var role in Options.CustomRoleCounts)
                 {
                     if (!role.Key.IsEnable()) continue;
                     bool isFirst = true;
                     foreach (var c in Options.CustomRoleSpawnChances[role.Key].Children)
                     {
                         if (isFirst) { isFirst = false; continue; }
                         text += $"\n{c.GetName(disableColor: true)}:{c.GetString()}";

                         //タスク上書き設定用の処理
                         if (c.Name == "doOverride" && c.GetBool() == true)
                         {
                             foreach (var d in c.Children)
                             {
                                 text += $"\n{d.GetName(disableColor: true)}:{d.GetString()}";
                             }
                         }
                         //メイヤーのポータブルボタン使用可能回数
                         if (c.Name == "MayorHasPortableButton" && c.GetBool() == true)
                         {
                             foreach (var d in c.Children)
                             {
                                 text += $"\n{d.GetName(disableColor: true)}:{d.GetString()}";
                             }
                         }
                         text = text.RemoveHtmlTags();
                     }
                 }*/
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (!role.Key.IsEnable()) continue;
                    text += $"\n【{GetRoleName(role.Key)}×{role.Key.GetCount()}】\n";
                    ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref text);
                    text = text.RemoveHtmlTags();
                }
                foreach (var opt in CustomOption.Options.Where(x => x.Enabled && x.Parent == null && x.Id >= 80000 && !x.IsHidden(Options.CurrentGameMode())))
                {
                    if (opt.Name == "KillFlashDuration")
                        text += $"\n【{opt.GetName(true)}: {opt.GetString()}】\n";
                    else
                        text += $"\n【{opt.GetName(true)}】\n";
                    ShowChildrenSettings(opt, ref text);
                    text = text.RemoveHtmlTags();
                }
                /*
                if (Options.EnableLastImpostor.GetBool()) text += String.Format("\n{0}:{1}", GetString("LastImpostorKillCooldown"), Options.LastImpostorKillCooldown.GetString());
                if (Options.DisableDevices.GetBool())
                {
                    if (Options.DisableDevices.GetBool()) text += String.Format("\n{0}:{1}", Options.DisableAdmin.GetName(disableColor: true), Options.WhichDisableAdmin.GetString());
                }
                if (Options.SyncButtonMode.GetBool()) text += String.Format("\n{0}:{1}", GetString("SyncedButtonCount"), Options.SyncedButtonCount.GetInt());
                if (Options.SabotageTimeControl.GetBool())
                {
                    if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 2) text += String.Format("\n{0}:{1}", GetString("PolusReactorTimeLimit"), Options.PolusReactorTimeLimit.GetString());
                    if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4) text += String.Format("\n{0}:{1}", GetString("AirshipReactorTimeLimit"), Options.AirshipReactorTimeLimit.GetString());
                }
                if (Options.VoteMode.GetBool())
                {
                    if (Options.GetWhenSkipVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", GetString("WhenSkipVote"), Options.WhenSkipVote.GetString());
                    if (Options.GetWhenNonVote() != VoteMode.Default) text += String.Format("\n{0}:{1}", GetString("WhenNonVote"), Options.WhenNonVote.GetString());
                    if ((Options.GetWhenNonVote() == VoteMode.Suicide || Options.GetWhenSkipVote() == VoteMode.Suicide) && CustomRoles.Terrorist.IsEnable()) text += String.Format("\n{0}:{1}", GetString("CanTerroristSuicideWin"), Options.CanTerroristSuicideWin.GetBool());
                }*/
                if (CustomRoles.Engineer.IsEnable() | CustomRoles.Scientist.IsEnable() | CustomRoles.GuardianAngel.IsEnable() | CustomRoles.Shapeshifter.IsEnable())
                {
                    text += "\n\nVanilla Role Settings:";
                    // GameOptionsManager.Instance.CurrentGameOptions
                    if (CustomRoles.Engineer.IsEnable())
                    {
                        text += String.Format("\n{0}:{1}", "Vent Use Cooldown", GameOptionsManager.Instance.currentNormalGameOptions.GetEngineerOptions().EngineerCooldown);
                        text += String.Format("\n{0}:{1}", "Max Time In Vents", GameOptionsManager.Instance.currentNormalGameOptions.GetEngineerOptions().EngineerInVentMaxTime);
                    }
                    if (CustomRoles.Shapeshifter.IsEnable())
                    {
                        text += String.Format("\n{0}:{1}", "Shapeshift Cooldown", GameOptionsManager.Instance.currentNormalGameOptions.GetShapeshifterOptions().ShapeshifterCooldown);
                        text += String.Format("\n{0}:{1}", "Shapeshift Duration", GameOptionsManager.Instance.currentNormalGameOptions.GetShapeshifterOptions().ShapeshifterDuration);
                        text += String.Format("\n{0}:{1}", "Leave Shapeshifting Evidence", GetOnOff(GameOptionsManager.Instance.currentNormalGameOptions.GetShapeshifterOptions().ShapeshifterLeaveSkin));
                    }
                    if (CustomRoles.Scientist.IsEnable())
                    {
                        text += String.Format("\n{0}:{1}", "Vitals Display Cooldown", GameOptionsManager.Instance.currentNormalGameOptions.GetScientistOptions().ScientistCooldown);
                        text += String.Format("\n{0}:{1}", "Battery Charge", GameOptionsManager.Instance.currentNormalGameOptions.GetScientistOptions().ScientistBatteryCharge);
                    }
                    if (CustomRoles.GuardianAngel.IsEnable())
                    {
                        text += String.Format("\n{0}:{1}", "Protect Cooldown", GameOptionsManager.Instance.currentNormalGameOptions.GetGuardianAngelOptions().GuardianAngelCooldown);
                        text += String.Format("\n{0}:{1}", "Protection Duration", GameOptionsManager.Instance.currentNormalGameOptions.GetGuardianAngelOptions().ProtectionDurationSeconds);
                        text += String.Format("\n{0}:{1}", "Protect Visible to Impostors", GetOnOff(GameOptionsManager.Instance.currentNormalGameOptions.GetGuardianAngelOptions().ImpostorsCanSeeProtect));
                    }
                }
                text += String.Format("\n{0}:{1}", GetString("LadderDeath"), GetOnOff(Options.LadderDeath.GetBool()));

            }
            /* if (Options.LadderDeath.GetBool())
             {
                 text += String.Format("\n{0}:{1}", GetString("LadderDeath"), GetOnOff(Options.LadderDeath.GetBool()));
                 text += String.Format("\n{0}:{1}", GetString("LadderDeathChance"), Options.LadderDeathChance.GetString());
             }
             if (Options.IsStandardHAS) text += String.Format("\n{0}:{1}", GetString("StandardHAS"), GetOnOff(Options.StandardHAS.GetBool()));
             if (Options.NoGameEnd.GetBool()) text += String.Format("\n{0}:{1}", GetString("NoGameEnd"), GetOnOff(Options.NoGameEnd.GetBool()));
             if (Options.DisableTaskWin.GetBool()) text += String.Format("\n{0}:{1}", GetString("DisableTaskWin"), GetOnOff(Options.DisableTaskWin.GetBool()));
            */
            SendMessage(text, PlayerId);
        }
        public static void killercount(PlayerControl callingPlayer)
        {

            // Count living players by role
            int impostors = 0;
            int neutrals = 0;
            int kneutrals = 0;
            int crewmates = 0;

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (!p.Data.IsDead)
                {
                    if (p.IsImpostor())
                    {
                        impostors++;
                    }
                    else if (p.IsNeutral())
                    {
                        neutrals++;
                    }
                    else if (p.IskNeutral())
                    {
                        kneutrals++;
                    }
                    else if (p.IsCrewmate())
                    {
                        crewmates++;
                    }

                }
            }

            // Build message
            string message = $"<color=#f21b1b>Impostors: {impostors}</color>,\r\n<color=#f7b61e>Neutral non killers: {neutrals}</color>,\r\n<color=#1ef77c>Neutral Killers: {kneutrals}</color>,\r\n<color=#1ee1f7>Crewmates: {crewmates}</color>";

            // Send message
            SendMessage(message, callingPlayer.PlayerId);

        }
        public static void ShowDeathCauses(PlayerControl player)
        {
            if (player.Is(CustomRoles.Doctor))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.IsDead)
                    {
                        sb.AppendLine($"{pc.GetRealName()}: {GetVitalText(pc.PlayerId)}");
                    }
                }
                SendMessage(sb.ToString(), player.PlayerId);
            }
            if (player.Is(CustomRoles.Paramedic))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.IsDead)
                    {
                        sb.AppendLine($"{pc.GetNameWithRole()}: {GetVitalText(pc.PlayerId)}");
                    }
                }
                SendMessage(sb.ToString(), player.PlayerId);
            }
            else
            {
                SendMessage("You do not have the ability to see death causes.", player.PlayerId);
            }
        }
        public static void ShowDeathReason(PlayerControl player)
        {

            if (player.Data.IsDead)
            {


                byte killedId = player.PlayerId;

                // Lookup killer id
                byte killerId = Main.whoKilledWho[killedId];

                // Get killer player
                PlayerControl killer = Utils.GetPlayerById(killerId);

                // Build message  
                string killerName = killer.Data.PlayerName;
                string reason = player.GetDeathReason();

                string message = $"You were killed by {killerName}. Your death reason: {reason}";

                SendMessage(message, player.PlayerId);

            }
            else
            {
                SendMessage("You must be dead to use this command!", player.PlayerId);
            }

        }
        public static void SendHostMessage2(string message, byte senderId, byte PlayerId = byte.MaxValue)
        {

            // Construct text using full message
            var text = "<color=#ED4EED>乂</color><color=#D178E0>乂</color><color=#B5A1D2>乂</color><color=#FF2222>MESSAGE FROM HOST</color><color=#B5A1D2>乂</color><color=#D178E0>乂</color><color=#ED4EED>乂</color>\r\n<color=#22F37D> " + message;

            // Remove any splitting or taking first word

            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (role.IsVanilla()) continue;
            }

            SendMessage(text, PlayerId);

        }

        public static void SendNonHostMessage(string message, string message1 = "", byte PlayerId = byte.MaxValue)
        {
            string text;



            if (message1 != "")
            {// THIS IS A HOST ONLY COMMAND
                text = "<color=#D178E0>乂</color><color=#B5A1D2>乂</color><color=#ED4EED>T</color><color=#D178E0>H</color><color=#B5A1D2>I</color><color=#ED4EED>S</color><color=#D178E0> I</color><color=#B5A1D2>S </color><color=#ED4EED>A </color><color=#D178E0>H</color><color=#B5A1D2>O</color><color=#ED4EED>S</color><color=#D178E0>T </color><color=#B5A1D2>O</color><color=#ED4EED>N</color><color=#D178E0>L</color><color=#B5A1D2>Y</color><color=#ED4EED> C</color><color=#D178E0>O</color><color=#B5A1D2>M</color><color=#ED4EED>M</color><color=#D178E0>A</color><color=#B5A1D2>N</color><color=#ED4EED>D </color><color=#D178E0>乂</color><color=#B5A1D2>乂</color>";
            }
            else
            {
                text = "<color=#D178E0>乂</color><color=#B5A1D2>乂</color><color=#ED4EED>T</color><color=#D178E0>H</color><color=#B5A1D2>I</color><color=#ED4EED>S</color><color=#D178E0> I</color><color=#B5A1D2>S </color><color=#ED4EED>A </color><color=#D178E0>H</color><color=#B5A1D2>O</color><color=#ED4EED>S</color><color=#D178E0>T </color><color=#B5A1D2>O</color><color=#ED4EED>N</color><color=#D178E0>L</color><color=#B5A1D2>Y</color><color=#ED4EED> C</color><color=#D178E0>O</color><color=#B5A1D2>M</color><color=#ED4EED>M</color><color=#D178E0>A</color><color=#B5A1D2>N</color><color=#ED4EED>D </color><color=#D178E0>乂</color><color=#B5A1D2>乂</color>";

            }
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                // if (role.RoleCannotBeInList()) continue;
                if (role.IsVanilla()) continue;

            }
            SendMessage(text, PlayerId);
        }
        public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            var text = GetString("Roles") + ":";
            text += "\nFor Percentages, \nPlease type /percentages.";
            text += string.Format("\n{0}:{1}", GetRoleName(CustomRoles.GM), GetOnOff(Options.EnableGM.GetBool()));
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                if (role.IsEnable()) text += string.Format("\n{0}x{1}", GetRoleName(role), role.GetCount());
            }
            SendMessage(text, PlayerId);
        }
        public static void ShowPercentages(byte PlayerId = byte.MaxValue)
        {
            var text = GetString("Percentages") + ":";
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                // if (role.RoleCannotBeInList()) continue;
                if (role.IsVanilla()) continue;
                if (role.IsEnable()) text += string.Format("\n{0}:{1}x{2}", GetRoleName(role), $"{PercentageChecker.CheckPercentage(role.ToString(), PlayerId, role: role)}%", role.GetCount());
                
            }
            SendMessage(text, PlayerId);
        }
        public static void BlockCommand(int times, byte PlayerId = byte.MaxValue)
        {
            var og = Main.MessageWait.Value;
            Main.MessageWait.Value = 0.25f;
            for (var i = 0; i < times; i++)
            {
                SendMessage(".\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.\n.", PlayerId);
                if (i == times | i == times - 1)
                    Main.MessageWait.Value = og;
            }
            SendMessage("This command has been blocked.", PlayerId);
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastroles"), PlayerId);
                return;
            }
            var text = GetString("LastResult") + ":";
            Dictionary<byte, CustomRoles> cloneRoles = new(Main.LastPlayerCustomRoles);
            text += $"\n{SetEverythingUpPatch.LastWinsText}\n";
            int otherWinners = 0;
            int otherNonWinners = 0;
            foreach (var id in Main.winnerList)
            {
                try
                {
                    text += $"\n★ " + SummaryTexts(id);
                    cloneRoles.Remove(id);
                }
                catch
                {
                    Logger.Error("Error loading last roles for Winner.", "Show Last Result (Winner)");
                    //text += "\n★ Error getting this person's info. (winner)";
                    otherWinners += 1;
                    cloneRoles.Remove(id);
                }
            }
            foreach (var kvp in cloneRoles)
            {
                try
                {
                    var id = kvp.Key;
                    text += $"\n　 " + SummaryTexts(id);
                }
                catch
                {
                    Logger.Error("Error loading last roles for a Non-Winner.", "Show Last Result");
                    //text += "\n　 Error getting this person's info. (non-winner)";
                    otherNonWinners += 1;
                }
            }

            if (otherWinners != 0 || otherNonWinners != 0)
            {
                if (otherWinners != 0)
                {
                    if (otherWinners == 1)
                    {
                        text += "\n★ +1 Other Winner (Error)";
                    }
                    else
                    {
                        text += $"\n★ +{otherWinners} Other Winners (Error)";
                    }
                }
                if (otherNonWinners != 0)
                {
                    if (otherNonWinners == 1)
                    {
                        text += "\n　 +1 Other Non-Winner (Error)";
                    }
                    else
                    {
                        text += $"\n　 +{otherNonWinners} Other Non-Winners (Error)";
                    }
                }
                text += "\n";
            }
            text += "\n　 Last Voted Player: " + Main.LastVotedPlayer;
            SendMessage(text, PlayerId);
        }

        public static void LastResult(byte PlayerId = byte.MaxValue)
        {
            var text = GetString("LastResult") + ":";
            Dictionary<byte, CustomRoles> cloneRoles = new(Main.AllPlayerCustomRoles);
            text += $"\n{SetEverythingUpPatch.LastWinsText}\n";
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                text += $"\n　 " + SummaryTexts(id);
            }
            SendMessage(text, PlayerId);
        }

        public static void ShowChildrenSettings(CustomOption option, ref string text, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "DisableSkeldDevices" && !Options.IsActiveSkeld) continue;
                if (opt.Value.Name == "DisableMiraHQDevices" && !Options.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "DisablePolusDevices" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "DisableAirshipDevices" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "PolusReactorTimeLimit" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "AirshipReactorTimeLimit" && !Options.IsActiveAirship) continue;
                if (deep > 0)
                {
                    text += string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0)));
                    text += opt.Index == option.Children.Count ? "┗ " : "┣ ";
                }
                text += $"{opt.Value.GetName(true)}: {opt.Value.GetString()}\n";
                if (opt.Value.Enabled) ShowChildrenSettings(opt.Value, ref text, deep + 1);
            }
        }


        public static string GetShowLastSubRolesText(byte id, bool disableColor = false, bool IntroCutscene = false)
        {
            if (!IntroCutscene)
            {
                var cSubRoleFound = Main.LastPlayerCustomSubRoles.TryGetValue(id, out var cSubRole);
                if (!cSubRoleFound || cSubRole == CustomRoles.NoSubRoleAssigned) return "";
                return disableColor ? " + " + GetRoleName(cSubRole) : Helpers.ColorString(Color.white, " (") + Helpers.ColorString(GetRoleColor(cSubRole), GetRoleName(cSubRole) + Helpers.ColorString(Color.white, ")"));
            }
            else
            {
                var cSubRoleFound = Main.AllPlayerCustomSubRoles.TryGetValue(id, out var cSubRole);
                if (!cSubRoleFound || cSubRole == CustomRoles.NoSubRoleAssigned) return "";
                return disableColor ? " + " + GetRoleName(cSubRole) : Helpers.ColorString(Color.white, " (") + Helpers.ColorString(GetRoleColor(cSubRole), GetRoleName(cSubRole) + Helpers.ColorString(Color.white, ")"));
            }
        }
        public static string SubRoleIntro(byte id, bool disableColor = false)
        {
            var cSubRoleFound = Main.AllPlayerCustomSubRoles.TryGetValue(id, out var cSubRole);
            if (!cSubRoleFound || cSubRole == CustomRoles.NoSubRoleAssigned) return "";
            return disableColor ? " + " + GetRoleName(cSubRole) : Helpers.ColorString(Color.white, " (") + Helpers.ColorString(GetRoleColor(cSubRole), GetRoleName(cSubRole) + Helpers.ColorString(Color.white, ")"));
        }

        public static void ShowHelp()
        {
            SendMessage(
                GetString("CommandList")
                + $"\n/winner - {GetString("Command.winner")}"
                + $"\n/lastresult - {GetString("Command.lastresult")}"
                + $"\n/rename - {GetString("Command.rename")}"
                + $"\n/now - {GetString("Command.now")}"
                + $"\n/h now - {GetString("Command.h_now")}"
                + $"\n/h roles {GetString("Command.h_roles")}"
                + $"\n/h attributes {GetString("Command.h_attributes")}"
                + $"\n/h modes {GetString("Command.h_modes")}"
                + $"\n/dump - {GetString("Command.dump")}"
                );

        }

        public static Dictionary<List<byte>, List<byte>> GetPsychicStuff(PlayerControl seer)
        {
            Dictionary<List<byte>, List<byte>> Dictionary = new();
            int numOfPsychicBad = Mathf.RoundToInt(UnityEngine.Random.RandomRange(0, 3));
            //numOfPsychicBad = Mathf.RoundToInt(numOfPsychicBad);
            if (numOfPsychicBad > 3) // failsafe
                numOfPsychicBad = 3;
            List<byte> goodids = new();
            List<byte> badids = new();
            Dictionary<byte, bool> isGood = new();
            if (!seer.Data.IsDead)
            {
                List<PlayerControl> badPlayers = new();
                List<PlayerControl> goodPlayers = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.IsDead || pc.Data.Disconnected || pc == seer || pc == null) continue;
                    isGood.Add(pc.PlayerId, true);
                    var role = pc.GetCustomRole();
                    if (Options.ExeTargetShowsEvil.GetBool())
                        if (Main.ExecutionerTarget.ContainsValue(pc.PlayerId))
                        {
                            badPlayers.Add(pc);
                            isGood[pc.PlayerId] = false;
                            continue;
                        }
                    switch (role)
                    {
                        case CustomRoles.GuardianAngelTOU:
                            if (!Options.GAdependsOnTaregtRole.GetBool()) break;
                            Main.GuardianAngelTarget.TryGetValue(pc.PlayerId, out var protectId);
                            if (!Utils.GetPlayerById(protectId).GetCustomRole().IsCrewmate())
                                badPlayers.Add(pc);
                            break;
                        //Lawyer Target And Options
                        case CustomRoles.Lawyer:
                            Main.LawyerTarget.TryGetValue(pc.PlayerId, out var lwPId);
                            if (!Utils.GetPlayerById(lwPId).GetCustomRole().IsCrewmate())
                                badPlayers.Add(pc);
                            break;

                    }
                    switch (role.GetRoleType())
                    {
                        case RoleType.Crewmate:
                            if (!Options.CkshowEvil.GetBool()) break;
                            if (role is CustomRoles.Sheriff or CustomRoles.Veteran or CustomRoles.Sellout or CustomRoles.Chancer or CustomRoles.Bodyguard or CustomRoles.Reviver or CustomRoles.Crusader or CustomRoles.Child or CustomRoles.Bastion or CustomRoles.Demolitionist or CustomRoles.Kamikaze or CustomRoles.NiceGuesser) badPlayers.Add(pc);
                            break;
                        case RoleType.Impostor:
                            badPlayers.Add(pc);
                            isGood[pc.PlayerId] = false;
                            break;
                        case RoleType.Neutral:
                            if (role.IsNeutralKilling()) badPlayers.Add(pc);
                            if (Options.NBshowEvil.GetBool())
                                if (role is CustomRoles.Opportunist or CustomRoles.Satan or CustomRoles.Lawyer or CustomRoles.Survivor or CustomRoles.GuardianAngelTOU or CustomRoles.Amnesiac or CustomRoles.SchrodingerCat) badPlayers.Add(pc);
                            if (Options.NEshowEvil.GetBool())
                                if (role is CustomRoles.Jester or CustomRoles.Terrorist or CustomRoles.Executioner or CustomRoles.Swapper or CustomRoles.Hacker or CustomRoles.Vulture) badPlayers.Add(pc);
                            break;
                        case RoleType.Madmate:
                            if (!Options.MadmatesAreEvil.GetBool()) break;
                            badPlayers.Add(pc);
                            isGood[pc.PlayerId] = false;
                            break;
                    }
                    if (isGood[pc.PlayerId]) goodPlayers.Add(pc);
                }
                if (numOfPsychicBad > badPlayers.Count) numOfPsychicBad = badPlayers.Count;
                int goodPeople = 3 - numOfPsychicBad;
                for (var i = 0; i < numOfPsychicBad; i++)
                {
                    var rando = new System.Random();
                    var player = badPlayers[rando.Next(0, badPlayers.Count)];
                    badPlayers.Remove(player);
                    badids.Add(player.PlayerId);
                }
                if (goodPeople != 0)
                    for (var i = 0; i < goodPeople; i++)
                    {
                        var rando = new System.Random();
                        var player = goodPlayers[rando.Next(0, goodPlayers.Count)];
                        goodPlayers.Remove(player);
                        goodids.Add(player.PlayerId);
                    }
            }
            Dictionary.Add(badids, goodids);
            return Dictionary;
        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var taskState = GetPlayerById(Terrorist.PlayerId).GetPlayerTaskState();
            if (taskState.IsTaskFinished && Main.DeadPlayersThisRound.Contains(Terrorist.PlayerId) && (!PlayerState.IsSuicide(Terrorist.PlayerId) || Options.CanTerroristSuicideWin.GetBool())) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Terrorist))
                    {
                        if (PlayerState.GetDeathReason(pc.PlayerId) == PlayerState.DeathReason.Vote)
                        {
                            //追放された場合は生存扱い
                            PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.etc);
                            //生存扱いのためSetDeadは必要なし
                        }
                        else
                        {
                            //キルされた場合は自爆扱い
                            PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        }
                    }
                    else if (!pc.Data.IsDead && !pc.Is(CustomRoles.Pestilence))
                    {
                        //生存者は爆死
                        pc.RpcMurderPlayer(pc, true);
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Bombed);
                        PlayerState.SetDead(pc.PlayerId);
                    }
                }
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                writer.Write((byte)CustomWinner.Terrorist);
                writer.Write(Terrorist.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPC.TerroristWin(Terrorist.PlayerId);
                EndGameHelper.AssignWinner(Terrorist.PlayerId);
            }
        }
        public static void ChildWin(GameData.PlayerInfo Child)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.Child))
                {
                    if (PlayerState.GetDeathReason(pc.PlayerId) == PlayerState.DeathReason.Vote)
                    {
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Screamed);
                    }
                    else
                    {
                        //キルされた場合は自爆扱い
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Screamed);
                    }
                }
                else if (!pc.Data.IsDead)
                {
                    if (!pc.Is(CustomRoles.Pestilence))
                    {
                        pc.RpcMurderPlayer(pc, true);
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.EarDamage);
                        PlayerState.SetDead(pc.PlayerId);
                    }
                }
            }
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
            writer.Write((byte)CustomWinner.Child);
            writer.Write(Child.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPC.ChildWin(Child.PlayerId);
            EndGameHelper.AssignWinner(Child.PlayerId);
        }
        public static void SendMessage(string text, byte sendTo = byte.MaxValue)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Main.MessagesToSend.Add((text, sendTo));
        }
        public static void RemoveChat(byte id)
        {
            Main.RemoveChat[id] = true;
            new LateTask(() => Main.RemoveChat[id] = false, 0.15f, $"Don't Remove Chat Anymore for {GetPlayerById(id).GetRealName(true)}");
        }
        public static void ApplySuffix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = DataManager.Player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId);
            }
            else
            {
                switch (Options.GetSuffixMode())
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += "\r\n<color=" + Main.modColor + ">TOH: TORv" + Main.PluginVersion + "</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += $"\r\n{GetString("SuffixMode.Streaming")}";
                        break;
                    case SuffixModes.Recording:
                        name += $"\r\n{GetString("SuffixMode.Recording")}";
                        break;
                    case SuffixModes.Dev:
                        if (!Main.devIsHost) break;
                        string fontSize = "1.5";
                        string dev = $"<size={fontSize}>Dev</size>";
                        string rname = name;
                        name = dev + "\r\n" + rname;
                        break;
                }
            }
            if (name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
        }
        public static PlayerControl GetPlayerById(int PlayerId)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }
        public static DeadBody GetDeadBodyById(int PlayerId)
        {
            var deadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            foreach (var body in deadBodies)
                if (body.ParentId == PlayerId)
                    return body;
            return null;
        }
        public static void DispersePlayers(PlayerControl shapeshifter)
        {
            var vents = UnityEngine.Object.FindObjectsOfType<Vent>();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data.Disconnected || pc.Data.IsDead || shapeshifter.PlayerId == pc.PlayerId || vents.Count == 0) continue; //dont disperse shifter
                var rando = new System.Random();
                var vent = vents[rando.Next(0, vents.Count)];
                TP(pc.NetTransform, new Vector2(vent.transform.position.x, vent.transform.position.y + 0.3636f));
            }
        }

        public static void BlackOut(this NormalGameOptionsV07 opt, bool IsBlackOut)
        {
            opt.ImpostorLightMod = Main.RealOptionsData.AsNormalOptions()!.ImpostorLightMod;
            opt.CrewLightMod = Main.RealOptionsData.AsNormalOptions()!.CrewLightMod;
            if (IsBlackOut)
            {
                opt.ImpostorLightMod = 0.0f;
                opt.CrewLightMod = 0.0f;
            }
            return;
        }
        public static void FlashColor(Color color, float duration = 1f)
        {
            var hud = DestroyableSingleton<HudManager>.Instance;
            if (hud.FullScreen == null) return;
            var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
            if (obj == null)
            {
                obj = GameObject.Instantiate(hud.FullScreen.gameObject, hud.transform);
                obj.name = "FlashColor_FullScreen";
            }
            hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
            {
                obj.SetActive(t != 1f);
                obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
            })));
        }

        public static void TP(CustomNetworkTransform nt, Vector2 location)
        {
            if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
            //nt.WriteVector2(location, writer);
            NetHelpers.WriteVector2(location, writer);
            writer.Write(nt.lastSequenceId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void NotifyRoles(bool isMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false, bool startOfMeeting = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (PlayerControl.AllPlayerControls == null) return;

            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            TownOfHost.Logger.Info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました", "NotifyRoles");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            //Snitch警告表示のON/OFF
            bool ShowSnitchWarning = false;
            if (CustomRoles.Snitch.IsEnable())
            {
                foreach (var snitch in PlayerControl.AllPlayerControls)
                {
                    if (snitch.Is(CustomRoles.Snitch) && !snitch.Data.IsDead && !snitch.Data.Disconnected)
                    {
                        var taskState = snitch.GetPlayerTaskState();
                        if (taskState.DoExpose)
                        {
                            ShowSnitchWarning = true;
                            break;
                        }
                    }
                }
            }

            var seerList = PlayerControl.AllPlayerControls;
            if (SpecifySeer != null)
            {
                seerList = new();
                seerList.Add(SpecifySeer);
            }
            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in seerList)
            {
                if (seer.IsModClient()) continue;
                if (seer.Data.Disconnected) continue;
                if (seer == null) return;
                string fontSize = "1.0";
                if (isMeeting && (seer.GetClient().PlatformData.Platform.ToString() == "Playstation" || seer.GetClient().PlatformData.Platform.ToString() == "Switch")) fontSize = "70%";
                TownOfHost.Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START", "NotifyRoles");
                //Loop1-bottleのSTART-END間でKeyNotFoundException
                //seerが落ちているときに何もしない

                //タスクなど進行状況を含むテキスト
                string SelfTaskText = GetProgressText(seer);

                //名前の後ろに付けるマーカー
                string SelfMark = "";

                //インポスター/キル可能な第三陣営に対するSnitch警告
                var canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //LocalPlayerがインポスター
                    (Options.SnitchCanFindNeutralKiller.GetBool() && seer.IsNeutralKiller());//or エゴイスト

                if (canFindSnitchRole && ShowSnitchWarning && !isMeeting)
                {
                    var arrows = "";
                    foreach (var arrow in Main.targetArrows)
                    {
                        if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                        {
                            //自分用の矢印で対象が死んでない時
                            arrows += arrow.Value;
                        }
                    }
                    SelfMark += $"<color={GetRoleColorCode(CustomRoles.Snitch)}>★{arrows}</color>";
                }

                if (!seer.Is(CustomRoles.Phantom) && Main.PhantomAlert)
                {
                    var arrows = "";
                    foreach (var arrow in Main.targetArrows)
                    {
                        if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                        {
                            //自分用の矢印で対象が死んでない時
                            arrows += arrow.Value;
                        }
                    }
                    SelfMark += $"<color={GetRoleColorCode(CustomRoles.Phantom)}>★{arrows}</color>";
                }

                if (seer.Is(CustomRoles.Phantom))
                {
                    if (Main.PhantomAlert) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Phantom)}>★★</color>";
                    else if (Main.PhantomCanBeKilled) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Phantom)}>★</color>";
                }
                List<byte> goodids = new();
                List<byte> badids = new();
                if (seer.Is(CustomRoles.Psychic) && startOfMeeting)
                {
                    var psychic = GetPsychicStuff(seer);
                    foreach (var stuff in psychic)
                    {
                        goodids = stuff.Value;
                        badids = stuff.Key;
                    }
                }

                //ハートマークを付ける(自分に)
                if (seer.Is(CustomRoles.LoversRecode)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.LoversRecode)}>♡</color>";

                //show modifier on name try this for non modded this worked
                if (seer.Is(CustomRoles.Bait)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Bait)}> Bait </color>";
                if (seer.Is(CustomRoles.Torch)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Torch)}> Torch </color>";
                if (seer.Is(CustomRoles.Bewilder)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Bewilder)}> Bewilder </color>";
                if (seer.Is(CustomRoles.Diseased)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Diseased)}> Diseased </color>";
                if (seer.Is(CustomRoles.Flash)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Flash)}> Flash </color>";
                if (seer.Is(CustomRoles.Escalation)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Escalation)}> Escalation </color>";
                if (seer.Is(CustomRoles.TieBreaker)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.TieBreaker)}> TieBreaker </color>";
                if (seer.Is(CustomRoles.Oblivious)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Oblivious)}> Oblivious </color>";
                if (seer.Is(CustomRoles.Sleuth)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Sleuth)}> Sleuth </color>";
                if (seer.Is(CustomRoles.Watcher)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Watcher)}> Watcher </color>";
                if (seer.Is(CustomRoles.Obvious)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Obvious)}> Obvious </color>";
                if (seer.Is(CustomRoles.Mayor)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Mayor)}> Mayor </color>";
                if (seer.Is(CustomRoles.Doctor)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Doctor)}> Doctor </color>";
                if (seer.Is(CustomRoles.DoubleShot)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.DoubleShot)}> DoubleShot</color>";
                if (seer.Is(CustomRoles.Demolitionist)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Demolitionist)}> Demolitionist </color>";
                if (seer.Is(CustomRoles.Trapper)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Trapper)}> Trapper </color>";
                if (seer.Is(CustomRoles.Veteran)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Veteran)}> Veteran </color>";
                if (seer.Is(CustomRoles.Transporter)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Transporter)}> Transporter </color>";
                if (seer.Is(CustomRoles.Bastion)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.Bastion)}> Bastion </color>";
                if (seer.Is(CustomRoles.PUMPkinsPotion)) SelfMark += $"<color={GetRoleColorCode(CustomRoles.PUMPkinsPotion)}> PumpkinsPotion </color>";
                //呪われている場合
                if (Main.SpelledPlayer.Find(x => x.PlayerId == seer.PlayerId) != null && isMeeting)
                    SelfMark += "<color=#ff0000>†</color>";
                if (Main.SilencedPlayer.Find(x => x.PlayerId == seer.PlayerId) != null && isMeeting)
                    SelfMark += "<color=#ff0000> (S)</color>";

                if (seer.Is(CustomRoles.Survivor) && !Main.SurvivorStuff.ContainsKey(seer.PlayerId))
                {
                    Main.SurvivorStuff.Add(seer.PlayerId, (0, false, false, false, false));
                }
                if (Sniper.IsEnable())
                {
                    //銃声が聞こえるかチェック
                    SelfMark += Sniper.GetShotNotify(seer.PlayerId);
                }
                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";

                if (seer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(seer) != null)
                {
                    string BountyTargetName = BountyHunter.GetTarget(seer).GetRealName(isMeeting);
                    SelfSuffix = $"<size={fontSize}>Target:{BountyTargetName}</size>";
                }
                if (seer.Is(CustomRoles.Postman) && Postman.target != null)
                {
                    string BountyTargetName = Postman.target.GetRealName(isMeeting);
                    SelfSuffix = $"<size={fontSize}>Target:{BountyTargetName}</size>";
                }
                if (seer.Is(CustomRoles.FireWorks))
                {
                    string stateText = FireWorks.GetStateText(seer);
                    SelfSuffix = $"{stateText}";
                }
                if (seer.Is(CustomRoles.Witch))
                {
                    SelfSuffix = seer.IsSpellMode() ? "Mode:" + GetString("WitchModeSpell") : "Mode:" + GetString("WitchModeKill");
                }
                if (seer.Is(CustomRoles.Cleaner))
                {
                    SelfSuffix = "Can Clean: ";
                    SelfSuffix += Main.CleanerCanClean[seer.PlayerId] ? "Yes" : "No";
                }
                if (seer.Is(CustomRoles.HexMaster))
                {
                    SelfSuffix = seer.IsHexMode() ? "Mode:" + "Hexing" : "Mode:" + "Killing";
                }
                if (seer.Is(CustomRoles.Werewolf))
                {
                    var ModeLang = Main.IsRampaged ? "True" : "False";
                    var ReadyLang = Main.RampageReady ? "True" : "False";
                    SelfSuffix = "Is Rampaging: " + ModeLang;
                    SelfSuffix += "\nRampage Ready: " + ReadyLang;
                }
                if (seer.Is(CustomRoles.Medusa))
                {
                    var ModeLang = Main.IsGazing ? "True" : "False";
                    var ReadyLang = Main.GazeReady ? "True" : "False";
                    SelfSuffix = "Gazing: " + ModeLang;
                    SelfSuffix += "\nGaze Ready: " + ReadyLang;
                }
                if (seer.Is(CustomRoles.Veteran))
                {
                    var ModeLang = Main.VetIsAlerted ? "True" : "False";
                    var ReadyLang = Main.VetCanAlert ? "True" : "False";
                    SelfSuffix = "Alerted: " + ModeLang;
                    SelfSuffix += "\nCan Alert: " + ReadyLang;
                }
                if (seer.Is(CustomRoles.Transporter))
                {
                    var ModeLang = Main.CanTransport ? "Yes" : "No";
                    SelfSuffix = "Can Transport: " + ModeLang;
                }
                if (seer.Is(CustomRoles.Escapist))
                {
                    SelfSuffix = "Current Mode: " + Escapist.GetEscapistState(seer);
                }
                if (seer.Is(CustomRoles.Bomber))
                {
                    PlayerControl bombedPlayer = Utils.GetPlayerById(Bomber.CurrentBombedPlayer);
                    if (bombedPlayer != null)
                    {
                        SelfSuffix = $"Current Bombed Player: {bombedPlayer.GetRealName(isMeeting)}";
                    }
                }
                if (seer.Is(CustomRoles.Swooper))
                {
                    var ModeLang = Main.IsInvis ? "Yes" : "No";
                    var ReadyLang = Main.CanGoInvis ? "Yes" : "No";
                    SelfSuffix = "Swooping: " + ModeLang;
                    SelfSuffix += "\nSwoop Ready: " + ReadyLang;
                }
                if (seer.Is(CustomRoles.Wizard))
                {
                    var ModeLang = Main.IsInviswizard ? "Yes" : "No";
                    var ReadyLang = Main.CanGoInviswizard ? "Yes" : "No";
                    SelfSuffix = "Abracadabra: " + ModeLang;
                    SelfSuffix += "\nAbracadabra Ready: " + ReadyLang;
                }
                if (seer.Is(CustomRoles.PUMPkinsPotion))
                {
                    var ModeLang = Main.IsInvispumpkin ? "Yes" : "No";
                    var ReadyLang = Main.CanGoInvispumpkin ? "Yes" : "No";
                    SelfSuffix = "PUMPkined: " + ModeLang;
                    SelfSuffix += "\nPotion Ready: " + ReadyLang;
                }
                if (seer.Is(CustomRoles.TheGlitch))
                {
                    var ModeLang = Main.IsHackMode ? "Hack" : "Kill";
                    SelfSuffix = "Glitch Current Mode: " + ModeLang;
                }

                //他人用の変数定義
                bool SeerKnowsImpostors = false; //trueの時、インポスターの名前が赤色に見える
                bool SeerKnowsCoven = false; //trueの時、インポスターの名前が赤色に見える

                //タスクを終えたSnitchがインポスター/キル可能な第三陣営の方角を確認できる
                if (seer.Is(CustomRoles.Snitch))
                {
                    var TaskState = seer.GetPlayerTaskState();
                    if (TaskState.IsTaskFinished)
                    {
                        SeerKnowsImpostors = true;
                        if (Options.SnitchCanFindCoven.GetBool())
                            SeerKnowsCoven = true;
                        //ミーティング以外では矢印表示
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                //自分用の矢印で対象が死んでない時
                                if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                                    SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.Is(CustomRoles.Postman))
                {
                    if (Postman.target != null)
                    {
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                //自分用の矢印で対象が死んでない時
                                if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                                    SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.Is(CustomRoles.Vulture))
                {
                    var TaskState = Options.VultureArrow.GetBool();
                    if (TaskState)
                    {
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                if (Main.DeadPlayersThisRound.Contains(arrow.Key.Item2))
                                    if (arrow.Key.Item1 == seer.PlayerId && PlayerState.isDead[arrow.Key.Item2])
                                        SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.Is(CustomRoles.Medium))
                {
                    var TaskState = Options.MediumArrow.GetBool();
                    if (TaskState)
                    {
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                if (Main.DeadPlayersThisRound.Contains(arrow.Key.Item2))
                                    if (arrow.Key.Item1 == seer.PlayerId && PlayerState.isDead[arrow.Key.Item2])
                                        SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }
                if (seer.Is(CustomRoles.Reviver))
                {
                    var TaskState = Options.ReviverArrow.GetBool();
                    if (TaskState)
                    {
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                if (Main.DeadPlayersThisRound.Contains(arrow.Key.Item2))
                                    if (arrow.Key.Item1 == seer.PlayerId && PlayerState.isDead[arrow.Key.Item2])
                                        SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.Is(CustomRoles.Amnesiac))
                {
                    var TaskState = Options.AmnesiacArrow.GetBool();
                    if (TaskState)
                    {
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                if (Main.DeadPlayersThisRound.Contains(arrow.Key.Item2))
                                    if (arrow.Key.Item1 == seer.PlayerId && PlayerState.isDead[arrow.Key.Item2])
                                        SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.GetCustomRole().IsCoven())
                {
                    SeerKnowsCoven = true;
                }
                if (seer.Is(CustomRoles.MadSnitch))
                {
                    var TaskState = seer.GetPlayerTaskState();
                    if (TaskState.IsTaskFinished)
                        SeerKnowsImpostors = true;
                }
                if (seer.Is(CustomRoles.CorruptedSheriff))
                    SeerKnowsImpostors = true;

                foreach (var target in PlayerControl.AllPlayerControls)
                {
                    //targetがseer自身の場合は何もしない
                    if (target == seer || target.Data.Disconnected) continue;
                    if (target == null) continue;
                    if (target.Is(CustomRoles.Phantom)) continue;
                    if (target.Is(CustomRoles.Phantom) && Main.PhantomAlert)
                    {
                        if (!isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                if (arrow.Key.Item1 == seer.PlayerId && !PlayerState.isDead[arrow.Key.Item2])
                                    SelfSuffix += arrow.Value;
                            }
                        }
                    }
                }

                if (seer.PlayerId == AgiTater.CurrentBombedPlayer && AgiTater.IsEnable())
                {
                    SelfSuffix = Helpers.ColorString(GetRoleColor(CustomRoles.AgiTater), $"<size={fontSize}>Pass the bomb to another player!</size>");
                }


                //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                if (SelfSuffix != "")
                    SelfSuffix = Helpers.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), SelfSuffix);
                if (isMeeting) SelfSuffix = "";
                if (Options.RolesLikeToU.GetBool() && !isMeeting)
                {
                    string SeerRealName = seer.GetRealName(isMeeting);

                    string SelfRoleName = $"{Helpers.ColorString(seer.GetRoleColor(), seer.GetRoleName())}{SelfTaskText}";
                    string SelfName = $"{Helpers.ColorString(seer.GetRoleColor(), SeerRealName)}{SelfMark}";
                    if (Main.KilledDemo.Contains(seer.PlayerId))
                        SelfName += $"<size={fontSize}>\r\n{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Demolitionist), "You killed Demolitionist!")}</size>";
                    if (Main.KilledKami.Contains(seer.PlayerId))
                        SelfName += $"<size={fontSize}>\r\n{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), "You killed Kamikaze Rip!")}</size>";
                    if (seer.Is(CustomRoles.Arsonist) && seer.IsDouseDone())
                        SelfName = $"</size>\r\n{Helpers.ColorString(seer.GetRoleColor(), GetString("EnterVentToWin"))}";
                    SelfName = SelfName + "\r\n" + SelfRoleName;
                    SelfName += SelfSuffix == "" ? "" : "\r\n " + SelfSuffix;
                    if (!isMeeting) SelfName += "\r\n";
                    
                    //適用
                    seer.RpcSetNamePrivate(SelfName, true, force: NoCache);
                }
                else
                {
                    string SeerRealName = seer.GetRealName(isMeeting);

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    string SelfRoleName = $"<size={fontSize}>{Helpers.ColorString(seer.GetRoleColor(), seer.GetRoleName())}{SelfTaskText}</size>";
                    string SelfName = $"{Helpers.ColorString(seer.GetRoleColor(), SeerRealName)}{SelfMark}";
                    if (Main.KilledDemo.Contains(seer.PlayerId))
                        SelfName += $"</size>\r\n{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Demolitionist), "You killed Demolitionist!")}";
                    if (Main.KilledKami.Contains(seer.PlayerId))
                        SelfName += $"</size>\r\n{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), "You killed Kamikaze!")}";
                    // else SelfName = $"{Helpers.ColorString(seer.GetRoleColor(), SeerRealName)}{SelfMark}";
                    if (seer.Is(CustomRoles.Arsonist) && seer.IsDouseDone())
                        SelfName = $"</size>\r\n{Helpers.ColorString(seer.GetRoleColor(), GetString("EnterVentToWin"))}";
                    SelfName = SelfRoleName + "\r\n" + SelfName;
                    SelfName += SelfSuffix == "" ? "" : "\r\n " + SelfSuffix;
                    if (!isMeeting) SelfName += "\r\n";

                    //適用
                    seer.RpcSetNamePrivate(SelfName, true, force: NoCache);
                }

                if (seer.Is(CustomRoles.Survivor) && !Main.SurvivorStuff.ContainsKey(seer.PlayerId))
                {
                    Main.SurvivorStuff.Add(seer.PlayerId, (0, false, false, false, false));
                }

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || SeerKnowsImpostors //seerがインポスターを知っている状態
                    || SeerKnowsCoven
                    || seer.GetCustomRole().IsImpostor() //seerがインポスター
                    || seer.Is(CustomRoles.EgoSchrodingerCat) //seerがエゴイストのシュレディンガーの猫
                    || seer.Is(CustomRoles.JSchrodingerCat) //seerがJackal陣営のシュレディンガーの猫
                    || seer.GetCustomRole().IsJackalTeam()
                    || NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.Is(CustomRoles.Arsonist)
                    || seer.Is(CustomRoles.LoversRecode)
                    || seer.Is(CustomRoles.Bomber)
                    || Main.SpelledPlayer.Count > 0
                    || Main.SilencedPlayer.Count > 0
                    || seer.Is(CustomRoles.GuardianAngelTOU)
                    || seer.Is(CustomRoles.Lawyer)
                    || seer.Is(CustomRoles.Executioner)
                    || seer.Is(CustomRoles.Swapper)
                    || seer.Is(CustomRoles.Doctor) //seerがドクター
                    || seer.Is(CustomRoles.Paramedic)
                    || seer.Is(CustomRoles.Puppeteer)
                    || seer.Is(CustomRoles.NeutWitch)
                    || seer.Is(CustomRoles.HexMaster)
                    || seer.Is(CustomRoles.BountyHunter)
                    || seer.Is(CustomRoles.Postman)
                    || seer.Is(CustomRoles.Investigator)
                    || Bomber.CurrentBombedPlayer != 255
                    || Main.rolesRevealedNextMeeting.Count != 0
                    || Main.rolesRevealedNextMeeting1.Count != 0
                    || Main.PhantomAlert
                    || seer.Is(CustomRoles.Dracula)
                    || seer.Is(CustomRoles.Hustler)
                    // || (IsActive(SystemTypes.Comms) && Options.CamoComms.GetBool())
                    //|| Main.KilledDemo.Contains(seer.PlayerId)
                    || seer.Is(CustomRoles.PlagueBearer)
                    || seer.Is(CustomRoles.YingYanger)
                    //|| seer.GetCustomSubRole().GetModifierType() != ModifierType.None
                    //|| IsActive(SystemTypes.Electrical)
                    || Camouflague.IsActive
                    || NoCache
                    || ForceLoop
                )
                {
                    /*if (Camouflague.IsActive && !Camouflague.InMeeting && !Camouflague.did && Options.CamoComms.GetBool())
                    {
                        Camouflague.did = true;
                        Camouflague.MeetingCause();
                    }*/

                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer || target.Data.Disconnected) continue;
                        if (target == null) continue;
                        TownOfHost.Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START", "NotifyRoles");

                        //他人のタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                        string TargetTaskText = "";
                        if (seer.Data.IsDead && GameStates.IsMeeting && Options.GhostCanSeeOtherRoles.GetBool())
                            TargetTaskText = $"{GetProgressText(target)}";

                        //名前の後ろに付けるマーカー
                        string TargetMark = "";
                        //呪われている人
                        if (Main.SpelledPlayer.Find(x => x.PlayerId == target.PlayerId) != null && isMeeting | seer.Data.IsDead)
                            TargetMark += "<color=#ff0000>†</color>";
                        if (Main.SilencedPlayer.Find(x => x.PlayerId == target.PlayerId) != null && isMeeting)
                            TargetMark += "<color=#ff0000> (S)</color>";
                        if (target.Is(CustomRoles.Phantom) && Main.PhantomAlert)
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Phantom)}>★</color>";
                        }
                        //タスク完了直前のSnitchにマークを表示
                        canFindSnitchRole = seer.Data.IsDead || seer.GetCustomRole().IsImpostor() || //Seerがインポスター
                            (Options.SnitchCanFindNeutralKiller.GetBool() && seer.IsNeutralKiller());//or エゴイスト

                        if (target.Is(CustomRoles.Snitch) && canFindSnitchRole)
                        {
                            var taskState = target.GetPlayerTaskState();
                            if (taskState.DoExpose)
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Snitch)}>★</color>";
                        }
                        string TeamText = "";

                        if (seer.GetCustomRole().IsImpostor() && !isMeeting)
                        {
                            if (Options.ImpostorKnowsRolesOfTeam.GetBool())
                            {
                                if (!CustomRoles.Egoist.IsEnable())
                                {
                                    //so we gotta make it so they can see the team. of their impostor
                                    if (target.GetCustomRole().IsImpostor())
                                    {
                                        if (!seer.Data.IsDead && !seer.Data.Disconnected)
                                        {
                                            // TeamText += "\r\n";
                                            if (!Options.RolesLikeToU.GetBool())
                                                TeamText += $"<size={fontSize}>{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}</size>\r\n";
                                            else
                                                TeamText = $"\r\n{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}";
                                        }
                                    }
                                }
                                else
                                {
                                    if (!Egoist.ImpostorsKnowEgo.GetBool())
                                    {
                                        //so we gotta make it so they can see the team. of their impostor
                                        if (target.GetCustomRole().IsImpostor())
                                        {
                                            if (!seer.Data.IsDead && !seer.Data.Disconnected)
                                            {
                                                // TeamText += "\r\n";
                                                if (!Options.RolesLikeToU.GetBool())
                                                    TeamText += $"<size={fontSize}>{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}</size>\r\n";
                                                else
                                                    TeamText = $"\r\n{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}";
                                            }
                                        }
                                    }
                                }
                            }

                        }
                        if (seer.GetCustomRole().IsCoven() && !isMeeting)
                        {
                            if (Options.CovenKnowsRolesOfTeam.GetBool())
                            {
                                if (target.GetCustomRole().IsCoven())
                                {
                                    if (!seer.Data.IsDead)
                                    {
                                        // TeamText += "\r\n";
                                        if (!Options.RolesLikeToU.GetBool())
                                            TeamText += $"<size={fontSize}>{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}</size>\r\n";
                                        else
                                            TeamText = $"\r\n{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}";
                                    }
                                }
                            }
                        }
                        //ハートマークを付ける(相手に)
                        if (seer.Is(CustomRoles.LoversRecode) && target.Is(CustomRoles.LoversRecode))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.LoversRecode)}>♡</color>";
                        }
                        //霊界からラバーズ視認
                        else if (seer.Data.IsDead && !seer.Is(CustomRoles.LoversRecode) && target.Is(CustomRoles.LoversRecode))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.LoversRecode)}>♡</color>";
                        }
                        // try this to see if non modded players can see it this didnt work 
                        if (seer.Is(CustomRoles.Bait) && target.Is(CustomRoles.Bait))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Bait)}> Bait</color>";
                        }
                        if (seer.Is(CustomRoles.Torch) && target.Is(CustomRoles.Torch))
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Torch)}> Torch</color>";
                        }

                        /*if (!seer.Is(CustomRoles.LoversRecode) && seer.GetCustomSubRole().GetModifierType() != ModifierType.None)
                        {
                            TargetMark += $"<color={GetRoleColorCode(CustomRoles.Yellow)}> " + seer.GetSubRoleName() + "</color>";
                        }*/

                        if (seer.Is(CustomRoles.Arsonist))//seerがアーソニストの時
                        {
                            if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                            }
                            if (
                                Main.ArsonistTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && //seerがオイルを塗っている途中(現在進行)
                                ar_kvp.Item1 == target //オイルを塗っている対象がtarget
                            )
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>△</color>";
                            }
                        }

                        if (seer.Data.IsDead && CustomRoles.Arsonist.IsEnable())
                        {
                            foreach (var pair in Main.isDoused)
                            {
                                if (pair.Key.Item2 == target.PlayerId && pair.Value)
                                {
                                    TargetMark += $"<color={GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                                }
                            }
                        }

                        if (Bomber.DoesExist() && seer.GetCustomRole().IsImpostor() | seer.Data.IsDead)
                        {
                            if (Bomber.AllImpostorsSeeBombedPlayer.GetBool() | seer.Data.IsDead)
                            {
                                if (target.PlayerId == Bomber.CurrentBombedPlayer) //seerがtargetに既にオイルを塗っている(完了)
                                {
                                    TargetMark += $"<color={GetRoleColorCode(CustomRoles.Bomber)}>▲</color>";
                                }
                                if (target.PlayerId == Bomber.CurrentDouseTarget)
                                {
                                    TargetMark += $"<color={GetRoleColorCode(CustomRoles.Bomber)}>△</color>";
                                }
                            }
                            else if (seer.Is(CustomRoles.Bomber))
                            {
                                if (target.PlayerId == Bomber.CurrentBombedPlayer)
                                {
                                    TargetMark += $"<color={GetRoleColorCode(CustomRoles.Bomber)}>▲</color>";
                                }
                                if (
                                    Bomber.BomberTimer.TryGetValue(seer.PlayerId, out var ar_kvp) &&
                                    ar_kvp.Item1 == target
                                )
                                {
                                    TargetMark += $"<color={GetRoleColorCode(CustomRoles.Bomber)}>△</color>";
                                }
                            }
                        }

                        if (seer.Is(CustomRoles.HexMaster))
                        {
                            if (seer.IsHexedPlayer(target))
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Coven)}>†</color>";
                        }
                        if (seer.Is(CustomRoles.PlagueBearer))//seerがアーソニストの時
                        {
                            if (seer.IsInfectedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Pestilence)}>◆</color>";
                            }
                            if (
                                Main.PlagueBearerTimer.TryGetValue(seer.PlayerId, out var ar_kvp) && //seerがオイルを塗っている途中(現在進行)
                                ar_kvp.Item1 == target //オイルを塗っている対象がtarget
                            )
                            {
                                TargetMark += $"<color={GetRoleColorCode(CustomRoles.Pestilence)}>△</color>";
                            }
                        }
                        if (seer.Data.IsDead && CustomRoles.PlagueBearer.IsEnable())
                        {
                            foreach (var pair in Main.isInfected)
                            {
                                if (pair.Key.Item2 == target.PlayerId && pair.Value)
                                {
                                    TargetMark += $"<color={GetRoleColorCode(CustomRoles.Pestilence)}>◆</color>";
                                }
                            }
                        }
                        if (seer.Is(CustomRoles.Puppeteer) | seer.Data.IsDead &&
                        Main.PuppeteerList.ContainsValue(seer.PlayerId) | seer.Data.IsDead &&
                        Main.PuppeteerList.ContainsKey(target.PlayerId))
                            TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>◆</color>";

                        if (seer.Is(CustomRoles.NeutWitch) | seer.Data.IsDead &&
                    Main.WitchList.ContainsValue(seer.PlayerId) | seer.Data.IsDead &&
                    Main.WitchList.ContainsKey(target.PlayerId))
                            TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.NeutWitch)}>◆</color>";

                        if (seer.Is(CustomRoles.CovenWitch) | seer.Data.IsDead &&
                        Main.WitchedList.ContainsValue(seer.PlayerId) | seer.Data.IsDead &&
                        Main.WitchedList.ContainsKey(target.PlayerId))
                            TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.CovenWitch)}>◆</color>";

                        
                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            string TargetRoleText = "";
                        if (seer.Data.IsDead && GameStates.IsMeeting && Options.GhostCanSeeOtherRoles.GetBool() || Main.rolesRevealedNextMeeting.Contains(target.PlayerId) && startOfMeeting)
                                if (!Options.RolesLikeToU.GetBool())
                                TargetRoleText = $"<size={fontSize}>{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}{TargetTaskText}</size>\r\n";
                            else
                                TargetRoleText = $"\r\n{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}{TargetTaskText}";

                        if (target.Is(CustomRoles.GM))
                            TargetRoleText = $"<size={fontSize}>{Helpers.ColorString(target.GetRoleColor(), target.GetRoleName())}</size>\r\n";

                        //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                        string TargetPlayerName = target.GetRealName(isMeeting);

                        if (seer.Is(CustomRoles.Psychic) && startOfMeeting)
                        {
                            foreach (var id in goodids)
                            {
                                if (target.PlayerId == id)
                                    TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName);
                            }
                            foreach (var id in badids)
                            {
                                if (target.PlayerId == id)
                                    TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName);
                            }
                        }

                        //ターゲットのプレイヤー名の色を書き換えます。
                        if (SeerKnowsImpostors) //Seerがインポスターが誰かわかる状態
                        {
                            //スニッチはオプション有効なら第三陣営のキル可能役職も見れる
                            if (seer.Is(CustomRoles.CorruptedSheriff))
                            {
                                var foundCheck = target.GetCustomRole().IsImpostor();
                                if (foundCheck)
                                    TargetPlayerName = Helpers.ColorString(target.GetRoleColor(), TargetPlayerName);
                            }
                            else
                            {
                                var snitchOption = seer.Is(CustomRoles.Snitch) && Options.SnitchCanFindNeutralKiller.GetBool();
                                var foundCheck = target.GetCustomRole().IsImpostor() || (snitchOption && target.IsNeutralKiller());
                                if (foundCheck)
                                    TargetPlayerName = Helpers.ColorString(target.GetRoleColor(), TargetPlayerName);
                            }
                        }
                        else if (SeerKnowsCoven)
                        {
                            var isCoven = seer.GetCustomRole().IsCoven();
                            var foundCheck = target.GetCustomRole().IsCoven();
                            if (isCoven)
                                if (foundCheck)
                                    TargetPlayerName = Helpers.ColorString(target.GetRoleColor(), TargetPlayerName);
                        }
                        else if (seer.GetCustomRole().IsImpostor() && target.Is(CustomRoles.Egoist) && Egoist.ImpostorsKnowEgo.GetBool())
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Egoist), TargetPlayerName);
                        else if (seer.GetCustomRole().IsImpostor() && target.Is(CustomRoles.CorruptedSheriff))
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName);
                        else if ((seer.Is(CustomRoles.EgoSchrodingerCat) && target.Is(CustomRoles.Egoist)) || //エゴ猫 --> エゴイスト
                                 (seer.GetCustomRole().IsJackalTeam() && target.GetCustomRole().IsJackalTeam() && Options.CurrentGameMode() == CustomGameMode.Standard)) // J猫 --> ジャッカル
                            TargetPlayerName = Helpers.ColorString(target.GetRoleColor(), TargetPlayerName);
                        else if (Utils.IsActive(SystemTypes.Electrical) && target.Is(CustomRoles.Mare) && !isMeeting && Main.MareHasRedName)
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName); //targetの赤色で表示
                        else if (Utils.IsActive(SystemTypes.Electrical) && target.Is(CustomRoles.Kamikaze) && !isMeeting && Main.KamiHasRedName)
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Kamikaze), TargetPlayerName); //targetの赤色で表示
                        else
                        {
                            //NameColorManager準拠の処理
                            var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                            TargetPlayerName = ncd.OpenTag + TargetPlayerName + ncd.CloseTag;
                        }
                        foreach (var ExecutionerTarget in Main.ExecutionerTarget)
                        {
                            if ((seer.PlayerId == ExecutionerTarget.Key || seer.Data.IsDead) && //seerがKey or Dead
                            target.PlayerId == ExecutionerTarget.Value)
                                TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Target), TargetPlayerName);
                        }

                        if (target.Is(CustomRoles.Mare) && isMeeting)
                        {
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Crewmate), TargetPlayerName);
                        }
                        if (target.Is(CustomRoles.Kamikaze) && isMeeting)
                        {
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Crewmate), TargetPlayerName);
                        }

                        foreach (var GATarget in Main.GuardianAngelTarget)
                        {
                            if ((seer.PlayerId == GATarget.Key || seer.Data.IsDead) && //seerがKey or Dead
                            target.PlayerId == GATarget.Value) //targetがValue
                                TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.GuardianAngelTOU)}>♦</color>";
                        }
                        //Lawyer Target
                        foreach (var LawyerTarget in Main.LawyerTarget)
                        {
                            if ((seer.PlayerId == LawyerTarget.Key || seer.Data.IsDead) && //seer
                            target.PlayerId == LawyerTarget.Value) //target
                                TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Lawyer)}>♦</color>";
                        }
                        if (seer.Data.IsDead && GameStates.IsMeeting && Options.GhostCanSeeOtherRoles.GetBool() || Main.rolesRevealedNextMeeting.Contains(target.PlayerId) && startOfMeeting)
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(target.GetCustomRole()), TargetPlayerName);
                        if (seer.Is(CustomRoles.HexMaster) && isMeeting)
                        {
                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                if (pc == null ||
                                    pc.Data.IsDead ||
                                    pc.Data.Disconnected ||
                                    pc.PlayerId == seer.PlayerId
                                ) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

                                if (Main.isHexed.TryGetValue((seer.PlayerId, pc.PlayerId), out var isDoused) && isDoused)
                                    Utils.SendMessage("You have been hexed by the Hex Master!", pc.PlayerId);
                            }
                        }
                        if (target.Is(CustomRoles.Child) && Options.ChildKnown.GetBool())
                            TargetMark += Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), " (C)");
                        if (seer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(seer) != null)
                        {
                            var bounty = BountyHunter.GetTarget(seer);
                            if (seer.Data.IsDead)
                            {
                                if (target == bounty) TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Target)}>◆</color>";
                            }
                            else
                            {
                                if (target == bounty) TargetPlayerName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Target), TargetPlayerName);
                            }
                        }

                        if (seer.Is(CustomRoles.Postman) | seer.Data.IsDead && Postman.target != null)
                        {
                            if (seer.Data.IsDead)
                            {
                                if (target == Postman.target) TargetMark += $"<color={Utils.GetRoleColorCode(CustomRoles.Postman)}>◆</color>";
                            }
                            else
                            {
                                if (target == Postman.target) TargetPlayerName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Target), TargetPlayerName);
                            }
                        }

                        if (seer.Is(CustomRoles.Investigator))
                        {
                            if (Investigator.hasSeered.ContainsKey((target.PlayerId)))
                            {
                                if (Investigator.hasSeered[target.PlayerId] == true)
                                {
                                    // Investigator has Seered Player.
                                    if (target.Is(CustomRoles.CorruptedSheriff))
                                    {
                                        if (Investigator.CSheriffSwitches.GetBool())
                                        {
                                            TargetPlayerName =
                                                Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Impostor),
                                                    TargetPlayerName);
                                        }
                                        else
                                        {
                                            if (Investigator.SeeredCSheriff)
                                                TargetPlayerName = Helpers.ColorString(
                                                    Utils.GetRoleColor(CustomRoles.Impostor), TargetPlayerName);
                                            else
                                                TargetPlayerName = Helpers.ColorString(
                                                    Utils.GetRoleColor(CustomRoles.TheGlitch), TargetPlayerName);
                                        }
                                    }
                                    else
                                    {
                                        if (Investigator.IsRed(target))
                                        {
                                            if (target.GetCustomRole().IsCoven())
                                            {
                                                if (Investigator.CovenIsPurple.GetBool())
                                                    TargetPlayerName =
                                                        Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Coven),
                                                            TargetPlayerName); //targetの名前をエゴイスト色で表示
                                                else
                                                    TargetPlayerName =
                                                        Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Impostor),
                                                            TargetPlayerName); //targetの名前をエゴイスト色で表示
                                            }
                                            else
                                                TargetPlayerName = Helpers.ColorString(
                                                    Utils.GetRoleColor(CustomRoles.Impostor), TargetPlayerName);
                                        }
                                        else
                                        {
                                            TargetPlayerName =
                                                Helpers.ColorString(Utils.GetRoleColor(CustomRoles.TheGlitch),
                                                    TargetPlayerName); //targetの名前をエゴイスト色で表示
                                        }
                                    }
                                }
                            }
                        }
                        if (seer.Is(CustomRoles.YingYanger) | seer.Data.IsDead && Main.ColliderPlayers.Contains(target.PlayerId))
                        {
                            TargetPlayerName = Helpers.ColorString(GetRoleColor(CustomRoles.Target), TargetPlayerName);
                        }

                        string TargetDeathReason = "";
                        if (seer.Is(CustomRoles.Doctor) && GameStates.IsMeeting && target.Data.IsDead && !seer.Data.IsDead)
                            TargetDeathReason = $"({Helpers.ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalTextDoc(target.PlayerId))})";
                        if (seer.Is(CustomRoles.Paramedic) && target.Data.IsDead && !seer.Data.IsDead)
                            TargetDeathReason = $"({Helpers.ColorString(GetRoleColor(CustomRoles.Paramedic), GetVitalText(target.PlayerId))})";
                        //全てのテキストを合成します。
                        string TargetName = "";
                        if (!Options.RolesLikeToU.GetBool())
                            TargetName = $"{TargetRoleText}{TeamText}{TargetPlayerName}{TargetDeathReason}{TargetMark}";
                        else
                            TargetName = $"{TargetPlayerName}{TeamText}{TargetRoleText}{TargetDeathReason}{TargetMark}";

                        if (AgiTater.IsEnable() && seer.Data.IsDead && target.PlayerId == AgiTater.CurrentBombedPlayer && !isMeeting)
                        {
                            TargetMark = Helpers.ColorString(GetRoleColor(CustomRoles.AgiTater), $"\n<size={fontSize}>Pass the bomb to another player!</size>");
                        }

                        //適用
                        target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);
                        //target.RpcSetNamePlatePrivate("");

                        Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":END", "NotifyRoles");
                    }
                }
                Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":END", "NotifyRoles");
            }
            Main.witchMeeting = false;
        }
        public static void CheckSurvivorVest(PlayerControl survivor, PlayerControl killer, bool suicide = true)
        {
            foreach (var ar in Main.SurvivorStuff)
            {
                if (ar.Key != survivor.PlayerId) break;
                var stuff = Main.SurvivorStuff[survivor.PlayerId];
                if (stuff.Item2)
                {
                    //killer.RpcGuardAndKill(killer);
                    killer.RpcGuardAndKill(survivor);
                }
                else
                {
                    if (!suicide)
                        killer.RpcMurderPlayerV2(survivor);
                    else
                        survivor.RpcMurderPlayerV2(survivor);
                }
            }
        }
        public static void RpcTeleport(this PlayerControl player, Vector2 location, bool isRandomSpawn = false, bool sendInfoInLogs = true)
        {
            if (sendInfoInLogs)
            {
                Logger.Info($" {player.GetNameWithRole().RemoveHtmlTags()} => {location}", "RpcTeleport");
                Logger.Info($" Player Id: {player.PlayerId}", "RpcTeleport");
            }

            // Don't check player status during random spawn
            if (!isRandomSpawn)
            {
                var cancelTeleport = false;

                if (player.inVent
                    || player.MyPhysics.Animations.IsPlayingEnterVentAnimation())
                {
                    Logger.Info($"Target: ({player.GetNameWithRole().RemoveHtmlTags()}) in vent", "RpcTeleport");
                    cancelTeleport = true;
                }

                else if (player.onLadder
                    || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    Logger.Warn($"Teleporting canceled - Target: ({player.GetNameWithRole().RemoveHtmlTags()}) is in on Ladder", "RpcTeleport");
                    cancelTeleport = true;
                }

                else if (player.inMovingPlat)
                {
                    Logger.Warn($"Teleporting canceled - Target: ({player.GetNameWithRole().RemoveHtmlTags()}) use moving platform (Airship/Fungle)", "RpcTeleport");
                    cancelTeleport = true;
                }


            }

            var playerNetTransform = player.NetTransform;
            var numHost = (ushort)(playerNetTransform.lastSequenceId + 6);
            var numLocalClient = (ushort)(playerNetTransform.lastSequenceId + 48);
            var numGlobal = (ushort)(playerNetTransform.lastSequenceId + 100);

            // Host side
            if (AmongUsClient.Instance.AmHost)
            {
                playerNetTransform.SnapTo(location, numHost);
            }

            var sender = CustomRpcSender.Create("TeleportPlayer");
            {
                // Local Teleport For Client
                if (PlayerControl.LocalPlayer.PlayerId != player.PlayerId)
                {
                    sender.AutoStartRpc(playerNetTransform.NetId, (byte)RpcCalls.SnapTo, targetClientId: player.GetClientId());
                    {
                        NetHelpers.WriteVector2(location, sender.stream);
                        sender.Write(numLocalClient);
                    }
                    sender.EndRpc();
                }

                // Global Teleport
                sender.AutoStartRpc(playerNetTransform.NetId, (byte)RpcCalls.SnapTo);
                {
                    NetHelpers.WriteVector2(location, sender.stream);
                    sender.Write(numGlobal);
                }
                sender.EndRpc();
            }
            sender.SendMessage();
        }

        public static bool IsProtectedByMedic(PlayerControl player)
        {
            if (Main.CurrentTarget.ContainsValue(player.PlayerId))
            {
                foreach (var key in Main.CurrentTarget)
                {
                    if (key.Value != player.PlayerId) continue;
                    var protector = Utils.GetPlayerById(key.Key);
                    switch (player.GetCustomRole())
                    {
                        case CustomRoles.Medic:
                            return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        public static PlayerControl? GetProtector(PlayerControl player)
        {
            if (Main.CurrentTarget.ContainsValue(player.PlayerId))
            {
                foreach (var key in Main.CurrentTarget)
                {
                    if (key.Value != player.PlayerId) continue;
                    var protector = Utils.GetPlayerById(key.Key);
                    return protector;
                }

                return null;
            }
            else
            {
                return null;
            }
        }

        public static bool IsProtectedByCrusader(PlayerControl player)
        {
            if (Main.CurrentTarget.ContainsValue(player.PlayerId))
            {
                foreach (var key in Main.CurrentTarget)
                {
                    if (key.Value != player.PlayerId) continue;
                    var protector = Utils.GetPlayerById(key.Key);
                    switch (player.GetCustomRole())
                    {
                        case CustomRoles.Crusader:
                            return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }
        public static Texture2D LoadTextureFromResources(string path)
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                ImageConversion.LoadImage(texture, ms.ToArray(), false);
                return texture;
            }
            catch
            {
                Logger.Error($"读入Texture失败：{path}", "LoadImage");
            }
            return null;
        }

        public static Dictionary<string, Sprite> CachedSprites = new();
        public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
        {
            try
            {
                if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
                Texture2D texture = LoadTextureFromResources(path);
                sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
                return CachedSprites[path + pixelsPerUnit] = sprite;
            }
            catch
            {
                Logger.Error($"读入Texture失败：{path}", "LoadImage");
            }
            return null;
        }
        public static bool IsProtectedByBodyGuard(PlayerControl player)
        {
            if (Main.CurrentTarget.ContainsValue(player.PlayerId))
            {
                foreach (var key in Main.CurrentTarget)
                {
                    if (key.Value != player.PlayerId) continue;
                    var protector = Utils.GetPlayerById(key.Key);
                    switch (player.GetCustomRole())
                    {
                        case CustomRoles.Bodyguard:
                            return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }
        public static void CustomSyncAllSettings()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.CustomSyncSettings();
            }
        }
        public static void AfterMeetingTasks()
        {
            BountyHunter.AfterMeetingTasks();
            SerialKiller.AfterMeetingTasks();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var role = pc.GetCustomRole();
                if (!pc.Data.IsDead)
                {
                    if (role == CustomRoles.Magician)
                    {
                        // Check if the Magician is dead
                        if (pc.Data.IsDead)
                        {
                            // Skip the rest of the code for the Magician if they are dead
                            continue;
                        }

                        Main.lastAmountOfTasks[pc.PlayerId] = 0;
                        // ...
                    }

                    if (role.IsImpostor() || role.IsCoven() || role.IsNeutralKilling() || role == CustomRoles.Investigator)
                        pc.RpcGuardAndKill(pc);
                }
                if (GameOptionsManager.Instance.currentNormalGameOptions.MapId != 4) // other than Airship
                {
                    if (pc.Is(CustomRoles.Camouflager))
                    {
                        //main.AirshipMeetingTimer.Add(pc.PlayerId , 0f);
                        Main.AllPlayerKillCooldown[pc.PlayerId] *= 2;
                    }
                }
                if (pc.Is(CustomRoles.Assassin) || pc.Is(CustomRoles.NiceGuesser) || pc.Is(CustomRoles.Pirate))
                {
                    Guesser.IsSkillUsed[pc.PlayerId] = false;
                }
            }
        }

        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
        }
        public static void CountAliveImpostors()
        {
            int AliveImpostorCount = 0;
            int AllImpostorCount = 0;
            List<PlayerControl> AllImpostors = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                CustomRoles pc_role = pc.GetCustomRole();
                if (pc_role.IsImpostor() && !pc.Data.IsDead) AliveImpostorCount++;
                if (pc_role.IsImpostor()) AllImpostors.Add(pc);
                if (pc_role.IsImpostor() || pc_role == CustomRoles.Egoist) AllImpostorCount++;
            }
            TownOfHost.Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
            Main.AliveImpostorCount = AliveImpostorCount;
            Main.AllImpostorCount = AllImpostorCount;
            Main.Impostors = new();
            Main.Impostors = AllImpostors;
            if (Options.EnableLastImpostor.GetBool() && AliveImpostorCount == 1)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.IsLastImpostor() && pc.Is(CustomRoles.Impostor))
                    {
                        pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                        break;
                    }
                }
                NotifyRoles(isMeeting: GameStates.IsMeeting);
                CustomSyncAllSettings();
            }
        }
        public static string GetAllRoleName(byte playerId)
        {
            return GetPlayerById(playerId)?.GetAllRoleName() ?? "";
        }
        public static string GetNameWithRole(byte playerId)
        {
            return GetPlayerById(playerId)?.GetNameWithRole() ?? "";
        }
        public static string GetNameWithRole(this GameData.PlayerInfo player)
        {
            return GetPlayerById(player.PlayerId)?.GetNameWithRole() ?? "";
        }
        public static string GetVoteName(byte num)
        {
            string name = "invalid";
            var player = GetPlayerById(num);
            if (num < 15 && player != null) name = player?.GetNameWithRole();
            if (num == 253) name = "Skip";
            if (num == 254) name = "None";
            if (num == 255) name = "Dead";
            return name;
        }
        public static byte GetVoteID(byte num)
        {
            byte name = 0;
            var player = GetPlayerById(num);
            if (num < 15 && player != null) name = player.PlayerId;
            return name;
        }
        public static string PadRightV2(this object text, int num)
        {
            int bc = 0;
            var t = text.ToString();
            foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
            return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
        }
        public static void DumpLog()
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string filename = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TownOfHost-v{Main.PluginVersion}-{t}.log";
            FileInfo file = new(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            file.CopyTo(@filename);
            System.Diagnostics.Process.Start(@$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
            if (PlayerControl.LocalPlayer != null)
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
        }
        public static (int, int) GetDousedPlayerCount(byte playerId)
        {
            int doused = 0, all = 0; //学校で習った書き方
                                     //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null ||
                    pc.Data.IsDead ||
                    pc.Data.Disconnected ||
                    pc.Is(CustomRoles.Phantom) ||
                    pc.PlayerId == playerId
                ) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

                all++;
                if (Main.isDoused.TryGetValue((playerId, pc.PlayerId), out var isDoused) && isDoused)
                    //塗れている場合
                    doused++;
            }

            return (doused, all);
        }
        public static (string, Color) GetRoleText(byte seerId, byte targetId, bool pure = false)
        {
            string RoleText = "Invalid Role";
            Color RoleColor;

            var seerMainRole = Main.AllPlayerCustomRoles[seerId];
            var seerSubRoles = Main.AllPlayerCustomSubRoles[seerId];

            var targetMainRole = Main.AllPlayerCustomRoles[targetId];
            var targetSubRoles = Main.AllPlayerCustomSubRoles[targetId];

          //  var self = seerId == targetId || Main.AllPlayerCustomRoles[seerId];

            RoleText = GetRoleName(targetMainRole);
            RoleColor = GetRoleColor(targetMainRole);

            

          //  if (Options.NameDisplayAddons.GetBool() && !pure)
            //    foreach (var subRole in targetSubRoles.Where(x => x is not CustomRoles.LastImpostor and not CustomRoles.Madmate and not CustomRoles.Charmed and not CustomRoles.Lovers))
              //      RoleText = Helpers.ColorString(GetRoleColor(subRole), GetString("Prefix." + subRole.ToString())) + RoleText;


            

            return (RoleText, RoleColor);
        }
        public static (int, int) GetHexedPlayerCount(byte playerId)
        {
            int hexed = 0, all = 0; //学校で習った書き方
                                    //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null ||
                    pc.Data.IsDead ||
                    pc.Data.Disconnected ||
                    pc.Is(CustomRoles.Phantom) ||
                    //!pc.GetCustomRole().IsCoven() ||
                    pc.PlayerId == playerId
                ) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

                if (!pc.GetCustomRole().IsCoven())
                    all++;
                if (Main.isHexed.TryGetValue((playerId, pc.PlayerId), out var isHexed) && isHexed)
                    //塗れている場合
                    hexed++;
            }

            return (hexed, all);
        }
        public static List<PlayerControl> GetDousedPlayer(byte playerId)
        {
            List<PlayerControl> doused = null;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null ||
                    pc.Data.IsDead ||
                    pc.Is(CustomRoles.Phantom) ||
                    pc.Data.Disconnected ||
                    pc.PlayerId == playerId
                ) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

                //all++;
                if (Main.isDoused.TryGetValue((playerId, pc.PlayerId), out var isDoused) && isDoused)
                    doused.Add(GetPlayerById(pc.PlayerId));
            }

            return doused;
        }
        public static (int, int) GetInfectedPlayerCount(byte playerId)
        {
            int infected = 0, all = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null ||
                    pc.Data.IsDead ||
                    pc.Is(CustomRoles.Phantom) ||
                    pc.Data.Disconnected ||
                    pc.PlayerId == playerId
                ) continue;

                all++;
                if (Main.isInfected.TryGetValue((playerId, pc.PlayerId), out var isInfected) && isInfected)
                    infected++;
            }

            return (infected, all);
        }
        public static string SummaryTexts(byte id, bool disableColor = false)
        {
            string summary = $"{Helpers.ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id])} {Helpers.ColorString(GetRoleColor(Main.LastPlayerCustomRoles[id]), GetRoleName(Main.LastPlayerCustomRoles[id]))}{GetShowLastSubRolesText(id)} {GetProgressText(id)} {GetVitalText(id)}";
            var killCountFound = Main.KillCount.TryGetValue(id, out var killAmt);
            if (killCountFound && killAmt != 0)
                summary += $" [Kill Count: {killAmt}]";
            return disableColor ? summary.RemoveHtmlTags() : Regex.Replace(summary, " ", "");
        }
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
        public static bool CanMafiaKill()
        {
            if (Main.AllPlayerCustomRoles == null) return false;
            // Number of Living Impostors excluding mafia
            int LivingImpostorsNum = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data.Disconnected) continue;
                var role = pc.GetCustomRole();
                if (!pc.Data.IsDead && role != CustomRoles.Mafia && role.IsImpostor()) LivingImpostorsNum++;
            }

            return LivingImpostorsNum <= 0;
        }
    }
}
