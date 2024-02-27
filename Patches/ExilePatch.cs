using System;
using HarmonyLib;
using System.Collections.Generic;
using Hazel;
using AmongUs.Data;

namespace TownOfHost
{
    class ExileControllerWrapUpPatch
    {
        public static GameData.PlayerInfo AntiBlackout_LastExiled;
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
                }
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
                }
            }
        }
        static void WrapUpPostfix(GameData.PlayerInfo exiled)
        {
            if (AntiBlackout.OverrideExiledPlayer)
            {
                exiled = AntiBlackout_LastExiled;
            }

            Main.witchMeeting = false;
            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            AntiBlackout.RestoreIsDead(doSend: false);
            if (exiled != null)
            {
                exiled.IsDead = true;
                PlayerState.SetDeathReason(exiled.PlayerId, PlayerState.DeathReason.Vote);
                if (Main.showEjections)
                {
                    //exiled.Object.Data.PlayerName = Main.LastVotedPlayer;
                    //exiled.Object.name = Main.LastVotedPlayer;
                    exiled.Object.RpcSetName(Main.LastVotedPlayer);
                }
                var role = exiled.GetCustomRole();
                // Main.DeadPlayersThisRound.Add(exiled.PlayerId);
                if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                    writer.Write((byte)CustomWinner.Jester);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPC.JesterExiled(exiled.PlayerId);
                    EndGameHelper.AssignWinner(exiled.PlayerId);
                    DecidedWinner = true;
                }
                if (role == CustomRoles.Child && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                    writer.Write((byte)CustomWinner.Child);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    //RPC.ChildWin(exiled.PlayerId);
                    Utils.ChildWin(exiled);
                    EndGameHelper.AssignWinner(exiled.PlayerId);
                    DecidedWinner = true;
                }
                if (role is CustomRoles.Oracle or CustomRoles.Bodyguard or CustomRoles.Medic && AmongUsClient.Instance.AmHost)
                {
                    if (Main.CurrentTarget[exiled.PlayerId] != 255)
                    {
                        Main.rolesRevealedNextMeeting.Add(Main.CurrentTarget[exiled.PlayerId]);
                        RPC.RpcAddOracleTarget(Main.CurrentTarget[exiled.PlayerId]);
                    }
                    Main.CurrentTarget.Remove(exiled.PlayerId);
                }
                

                if (Main.CurrentTarget.ContainsValue(exiled.PlayerId))
                {
                    byte Protector = 0x73;
                    Main.CurrentTarget.Do(x =>
                    {
                        if (x.Value == exiled.PlayerId)
                            Protector = x.Key;
                    });
                    if (Protector != 0x73)
                    {
                        Main.CurrentTarget.Remove(Protector);
                        Main.HasTarget[Protector] = false;
                    }
                }
                if (role == CustomRoles.Jackal && AmongUsClient.Instance.AmHost)
                {
                    Main.JackalDied = true;
                    if (Options.SidekickGetsPromoted.GetBool())
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc.Is(CustomRoles.Sidekick))
                                pc.RpcSetCustomRole(CustomRoles.Jackal);
                        }
                    }
                }
                if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                {
                    Utils.CheckTerroristWin(exiled);
                    DecidedWinner = true;
                }
                if (!exiled.Object.Is(CustomRoles.HexMaster) && exiled.Object.IsHexedDone() && AmongUsClient.Instance.AmHost)
                {
                    DecidedWinner = true;
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (!pc.Data.IsDead && !pc.GetCustomRole().IsCoven())
                        {
                            if (!pc.Is(CustomRoles.Pestilence))
                            {
                                pc.RpcMurderPlayer(pc, true);
                                PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Bombed);
                                PlayerState.SetDead(pc.PlayerId);
                            }
                        }
                    }
                }
            }
            if (AmongUsClient.Instance.AmHost)
                foreach (var kvp in Main.ExecutionerTarget)
                {
                    var executioner = Utils.GetPlayerById(kvp.Key);
                    if (executioner == null || executioner.Data.IsDead || executioner.Data.Disconnected) continue;
                    if (kvp.Value == exiled.PlayerId && !DecidedWinner)
                    {
                        Main.ExeCanChangeRoles = false;
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                        if (executioner.Is(CustomRoles.Executioner))
                            writer.Write((byte)CustomWinner.Executioner);
                        else
                            writer.Write((byte)CustomWinner.Swapper);
                        writer.Write(kvp.Key);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);

                        if (executioner.Is(CustomRoles.Executioner))
                            RPC.ExecutionerWin(kvp.Key);
                        else
                            RPC.SwapperWin(kvp.Key);
                    }
                }
            if (exiled.Object.Is(CustomRoles.TimeThief))
                exiled.Object.ResetVotingTime();
            if (exiled.Object.Is(CustomRoles.SchrodingerCat) && Options.SchrodingerCatExiledTeamChanges.GetBool())
                exiled.Object.ExiledSchrodingerCatTeamChange();

            Main.VetIsAlerted = false;
            Manipulator.ResetSabotagedMeeting();
            Main.HexesThisRound = 0;

            if (Main.currentWinner != CustomWinner.Terrorist) PlayerState.SetDead(exiled.PlayerId);
            {
                if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
                    Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;
                Main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
                Main.SilencedPlayer.Clear();
                Main.IsHackMode = false;
                Main.IsInvis = false;
                Main.IsInviswizard = false;
                Main.IsInvispumpkin = false;
                Main.CanGoInvis = false;
                Main.CanGoInviswizard = false;
                Main.CanGoInvispumpkin = false;
                Main.DoingYingYang = true;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    pc.ResetKillCooldown(meeting: true);
                    if (Options.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor))
                        pc.RpcResetAbilityCooldown();
                    if (pc.Is(CustomRoles.Veteran))
                        pc.RpcResetAbilityCooldown();
                    if (pc.Is(CustomRoles.Warlock))
                    {
                        Main.CursedPlayers[pc.PlayerId] = null;
                        Main.isCurseAndKill[pc.PlayerId] = false;
                    }
                }
            }

            if (!DecidedWinner && AmongUsClient.Instance.AmHost)
            {
                foreach (var kvp in Main.ExecutionerTarget)
                {
                    try
                    {
                        var swapper = Utils.GetPlayerById(kvp.Key);
                        if (swapper == null || swapper.Data.IsDead || swapper.Data.Disconnected) continue;
                        if (swapper.Is(CustomRoles.Swapper) && !DecidedWinner)
                        {
                            List<PlayerControl> targetList = new();
                            var rand = new System.Random();
                            foreach (var target in PlayerControl.AllPlayerControls)
                            {
                                if (swapper.PlayerId == target.PlayerId) continue;
                                if (!Options.ExecutionerCanTargetImpostor.GetBool() && target.GetCustomRole().IsImpostor() | target.GetCustomRole().IsMadmate()) continue;
                                if (target.GetCustomRole().IsNeutral()) continue;
                                if (target.GetCustomRole().IsCoven()) continue;
                                if (target.Is(CustomRoles.Phantom)) continue;
                                if (target.Is(CustomRoles.GM)) continue;
                                if (Main.ExecutionerTarget.ContainsValue(target.PlayerId)) continue;
                                if (target == null || target.Data.IsDead || target.Data.Disconnected) continue;

                                targetList.Add(target);
                            }

                            var Target = targetList[rand.Next(targetList.Count)];
                            if (Main.ExecutionerTarget.ContainsKey(swapper.PlayerId))
                            {
                                Main.ExecutionerTarget.Remove(swapper.PlayerId);
                                RPC.RemoveExecutionerKey(swapper.PlayerId);
                            }
                            Main.ExecutionerTarget.Add(swapper.PlayerId, Target.PlayerId);
                            RPC.SendExecutionerTarget(swapper.PlayerId, Target.PlayerId);
                            Logger.Info($"{swapper.GetNameWithRole()}:{Target.GetNameWithRole()}", "Swapper");
                            Utils.NotifyRoles(false, swapper);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.SendMessage("Error occured while changing Swapper Target.\n" + ex);
                    }
                }
            }
            FallFromLadder.Reset();
            Utils.CountAliveImpostors();
            Utils.AfterMeetingTasks();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
        }

        // 4 - 1

        static void WrapUpFinalizer(GameData.PlayerInfo exiled)
        {
            //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
            if (AmongUsClient.Instance.AmHost)
            {
                new LateTask(() =>
                {
                    exiled = AntiBlackout_LastExiled;
                    AntiBlackout.SendGameData();
                    if (AntiBlackout.OverrideExiledPlayer && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                        exiled != null && //exiledがnullでない
                        exiled.Object != null) //exiled.Objectがnullでない
                    {
                        exiled.Object.RpcExileV2();
                    }
                }, 0.5f, "Restore IsDead Task");
                if (Postman.PostmanWins)
                {
                    // POSTMAN WINS //
                    var postman = Postman.GetPostman();
                    var endReason = TempData.LastDeathReason switch
                    {
                        DeathReason.Exile => GameOverReason.ImpostorByVote,
                        DeathReason.Kill => GameOverReason.ImpostorByKill,
                        _ => GameOverReason.ImpostorByVote,
                    };
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null) continue;
                        if (player.Data.IsDead || player.Data.Disconnected) continue;
                        if (!player.Is(CustomRoles.Pestilence) && !player.Is(CustomRoles.Postman))
                        {
                            player.RpcMurderPlayer(player, true);
                            PlayerState.SetDeathReason(player.PlayerId, PlayerState.DeathReason.Bombed);
                        }
                    }
                    _ = new LateTask(() =>
                    {
                        var statistics = new CheckGameEndPatch.PlayerStatistics();
                        if (statistics.TotalAlive == 1)
                        {
                            MessageWriter endgame = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                            endgame.Write((byte)CustomWinner.Postman);
                            endgame.Write((byte)postman.PlayerId);
                            AmongUsClient.Instance.FinishRpcImmediately(endgame);
                            RPC.PostmanWins(postman.PlayerId);
                            EndGameHelper.AssignWinner(postman.PlayerId);
                            _ = new LateTask(() =>
                            {
                                GameManager.Instance.RpcEndGame(endReason, false);
                            }, 0.5f, "EndGameTaskForPhantom");
                        }
                        else
                        {
                            Logger.Info("Someone is still alive so Postman did not win.", "Postman Win Error");
                        }
                    }, 0.25f, "EndGameTaskForPostman");
                }
                Main.IsRampaged = false;
                Main.IsRoundOne = false;
                Main.IsInvis = false;
                Main.IsInviswizard = false;
                Main.IsInvispumpkin = false;
                Main.CanGoInvis = false;
                Main.CanGoInviswizard = false;
                Main.CanGoInvispumpkin = false;
                Main.IsRoundOneGA = false;
                Main.unvotablePlayers.Clear();
                Main.unvotablePlayers = new();
                Main.IsGazing = false;
                Main.GazeReady = false;
                Main.WitchesThisRound = 0;
                Main.bkProtected = false;
                Main.VetIsAlerted = false;
                Main.VetCanAlert = false;
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVetAlertState, Hazel.SendOption.Reliable, -1);
                writer.Write(Main.VetCanAlert);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                Main.CanTransport = false;
                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTransportState, Hazel.SendOption.Reliable, -1);
                writer2.Write(Main.CanTransport);
                AmongUsClient.Instance.FinishRpcImmediately(writer2);
                if (Options.TosOptions.GetBool() && Options.RoundReview.GetBool())
                    Main.MessageWait.Value = 1;
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                        Main.MareHasRedName = true;
                }, Mare.RedNameCooldownAfterMeeting.GetFloat(), "Mare Red Name Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                    Main.KamiHasRedName = true;
                }, Kamikaze.KamiNameCooldownAfterMeeting.GetFloat(), "Mare Red Name Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                        Main.RampageReady = true;
                    Utils.NotifyRoles();
                }, Options.RampageCD.GetFloat(), "Werewolf Rampage Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                        Main.CanGoInvis = true;
                    Utils.NotifyRoles();
                },
                Options.SwooperCooldown.GetFloat(), "Swooper Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                        Main.CanGoInviswizard = true;
                    Utils.NotifyRoles();
                },
                Options.WizardCooldown.GetFloat(), "Wizard Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                        Main.CanGoInvispumpkin = true;
                    Utils.NotifyRoles();
                },
                Options.PUMPkinsPotionCooldown.GetFloat(), "PUMPkins Potion Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                    {
                        Main.VetCanAlert = true;
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVetAlertState, Hazel.SendOption.Reliable, -1);
                        writer2.Write(Main.VetCanAlert);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                        Utils.NotifyRoles();
                    }
                },
                Options.VetCD.GetFloat(), "Veteran Alert Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                    {
                        Main.CanTransport = true;
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTransportState, Hazel.SendOption.Reliable, -1);
                        writer2.Write(Main.CanTransport);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                        Utils.NotifyRoles();
                    }
                }, Options.TransportCooldown.GetFloat(), "Transporter Transport Cooldown (After Meeting)");
                new LateTask(() =>
                {
                    if (!GameStates.IsMeeting)
                        Main.GazeReady = true;
                }, Options.StoneCD.GetFloat(), "Gaze Cooldown");
                foreach (var x in Main.AfterMeetingDeathPlayers)
                {
                    var player = Utils.GetPlayerById(x.Key);
                    Logger.Info($"{player.GetNameWithRole()}を{x.Value}", "AfterMeetingDeath");
                    PlayerState.SetDeathReason(x.Key, x.Value);
                    PlayerState.SetDead(x.Key);
                    player?.RpcExileV2();

                    if (player.Is(CustomRoles.TimeThief) && x.Value == PlayerState.DeathReason.LoversSuicide)
                        player?.ResetVotingTime();
                }
                Main.AfterMeetingDeathPlayers.Clear();
                Main.DeadPlayersThisRound.Clear();
                Main.MercCanSuicide = true;
                Main.satanCanSuicide = true;
                if (Main.rolesRevealedNextMeeting.Count != 0)
                {
                    Main.rolesRevealedNextMeeting.Clear();
                    RPC.RpcClearOracleTargets();
                    
                }
                

                if (Options.SheriffCorrupted.GetBool())
                {
                    if (!Sheriff.csheriff)
                    {
                        int IsAlive = 0;
                        int numCovenAlive = 0;
                        int numImpsAlive = 0;
                        int numNKalive = 0;
                        List<PlayerControl> couldBeTraitors = new();
                        List<byte> couldBeTraitorsid = new();
                        var rando = new Random();
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (!pc.Data.Disconnected)
                                if (!pc.Data.IsDead)
                                {
                                    IsAlive++;
                                    if (pc.GetCustomRole().IsNeutralKilling() && !Options.TraitorCanSpawnIfNK.GetBool())
                                        numNKalive++;
                                    if (pc.GetCustomRole().IsCoven() && !Options.TraitorCanSpawnIfCoven.GetBool())
                                        numCovenAlive++;
                                    if (pc.Is(CustomRoles.Sheriff) || pc.Is(CustomRoles.Investigator) || pc.Is(CustomRoles.Hitman))
                                        couldBeTraitors.Add(pc);
                                    if (pc.Is(CustomRoles.Sheriff) || pc.Is(CustomRoles.Investigator) || pc.Is(CustomRoles.Hitman))
                                        couldBeTraitorsid.Add(pc.PlayerId);
                                    if (pc.GetCustomRole().IsImpostor())
                                        numImpsAlive++;
                                }
                        }

                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (!pc.Data.Disconnected)
                                if (!pc.Data.IsDead)
                                {
                                    if (!pc.IsModClient()) continue;
                                    if (!pc.GetCustomRole().IsCrewmate()) continue;
                                    if (!couldBeTraitorsid.Contains(pc.PlayerId))
                                    {
                                        couldBeTraitors.Add(pc);
                                        couldBeTraitorsid.Add(pc.PlayerId);
                                    }
                                }
                        }

                        Sheriff.seer = couldBeTraitors[rando.Next(0, couldBeTraitors.Count)];

                        //foreach (var pva in __instance.playerStates)
                        if (IsAlive >= Options.PlayersForTraitor.GetFloat() && Sheriff.seer != null)
                        {
                            if (numCovenAlive == 0 && numNKalive == 0 && numCovenAlive == 0 && numImpsAlive - 1 <= 0)
                            {
                                Sheriff.seer.RpcSetCustomRole(CustomRoles.CorruptedSheriff);
                                Sheriff.seer.CustomSyncSettings();
                                Sheriff.csheriff = true;
                                RPC.SetTraitor(Sheriff.seer.PlayerId);
                            }
                        }
                    }
                }
                if (Options.TosOptions.GetBool() && Options.GameProgression.GetBool())
                {
                    new LateTask(() =>
                    {
                        Utils.CallMeeting();
                    }, 39, "Game Progression Meeting");
                }
            }
            SoundManager.Instance.ChangeMusicVolume(DataManager.Settings.Audio.MusicVolume);
            Logger.Info("タスクフェイズ開始", "Phase");
        }
    }
}
