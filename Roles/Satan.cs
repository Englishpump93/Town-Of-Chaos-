using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;
using AmongUs.GameOptions;
using TownOfHost.PrivateExtensions;

namespace TownOfHost
{
    public static class Satan
    {
        private static readonly int Id = 11001;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;
        private static CustomOption TimeLimit1;

        private static Dictionary<byte, float> SuicideTimer = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Satan, AmongUsExtensions.OptionType.Impostor);
           // KillCooldown = CustomOption.Create(Id + 10, Color.white, "SerialKillerCooldown", AmongUsExtensions.OptionType.Impostor, 20f, 2.5f, 100f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            TimeLimit1 = CustomOption.Create(Id + 11, Color.white, "SatanLimit", AmongUsExtensions.OptionType.Impostor, 10f, 5f, 180f, 5f, Options.CustomRoleSpawnChances[CustomRoles.Satan]);
        }
        public static void Init()
        {
            playerIdList = new();
            SuicideTimer = new();
        }
        public static void Add(byte serial)
        {
            playerIdList.Add(serial);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void ApplyGameOptions(NormalGameOptionsV07 opt) => opt.GetShapeshifterOptions().ShapeshifterCooldown = TimeLimit1.GetFloat();

        public static void OnCheckMurder(PlayerControl killer, bool isKilledSchrodingerCat = false)
        {
            if (killer.Is(CustomRoles.Satan))
            {
                if (isKilledSchrodingerCat)
                {
                    killer.RpcResetAbilityCooldown();
                    SuicideTimer[killer.PlayerId] = 0f;
                    return;
                }
                else
                {
                    killer.RpcResetAbilityCooldown();
                    SuicideTimer[killer.PlayerId] = 0f;
                    Main.AllPlayerKillCooldown[killer.PlayerId] = KillCooldown.GetFloat();
                    killer.CustomSyncSettings();
                }
            }
        }
        public static void OnReportDeadBody()
        {
            SuicideTimer.Clear();
        }
        public static void FixedUpdate(PlayerControl player)
        {
            if (!player.Is(CustomRoles.Satan)) return; //以下、シリアルキラーのみ実行

            if (GameStates.IsInTask && SuicideTimer.ContainsKey(player.PlayerId))
            {
                if (!player.IsAlive() | player.Data.IsDead)
                    SuicideTimer.Remove(player.PlayerId);
                else if (SuicideTimer[player.PlayerId] >= TimeLimit1.GetFloat())
                {
                    PlayerState.SetDeathReason(player.PlayerId, PlayerState.DeathReason.Suicide);//死因：自爆
                    player.RpcMurderPlayer(player, true);//自爆させる
                }
                else
                    SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;//時間をカウント
            }
        }
        public static void GetAbilityButtonText(HudManager __instance) => __instance.AbilityButton.OverrideText($"{GetString("SerialKillerSuicideButtonText")}");
        public static void AfterMeetingTasks()
        {
            foreach (var id in playerIdList)
            {
               
                
                    Utils.GetPlayerById(id)?.RpcResetAbilityCooldown();
                    SuicideTimer[id] = 0f;
                
            }
        }
    }
}
