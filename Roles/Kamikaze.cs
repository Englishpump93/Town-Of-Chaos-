using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.PrivateExtensions;

namespace TownOfHost
{
    public static class Kamikaze
    {
        private static readonly int Id = 23001;
        public static List<byte> playerIdList = new();

        private static CustomOption SpeedInLightsOut;
        public static CustomOption KamiNameCooldownAfterLights;
        public static CustomOption KamiNameCooldownAfterMeeting; 
        public static CustomOption KamiSuicideTime;


        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Kamikaze, AmongUsExtensions.OptionType.Crewmate);
            KamiSuicideTime = CustomOption.Create(Id + 14, Color.white, "KamiSuicideTime", AmongUsExtensions.OptionType.Crewmate, 2f, 1f, 2, 1, Options.CustomRoleSpawnChances[CustomRoles.Kamikaze]);
            SpeedInLightsOut = CustomOption.Create(Id + 10, Color.white, "MareSpeedInLightsOut", AmongUsExtensions.OptionType.Impostor, 2f, 0.25f, 3f, 0.25f, Options.CustomRoleSpawnChances[CustomRoles.Kamikaze]);
            KamiNameCooldownAfterLights = CustomOption.Create(Id + 12, Color.white, "RedNameCooldownAfterLights", AmongUsExtensions.OptionType.Impostor, 5f, 0, 30f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Kamikaze]);
            KamiNameCooldownAfterMeeting = CustomOption.Create(Id + 13, Color.white, "RedNameCooldownAfterMeeting", AmongUsExtensions.OptionType.Impostor, 15f, 0, 60f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Kamikaze]);
 
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte kami)
        {
            playerIdList.Add(kami);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void ApplyGameOptions(NormalGameOptionsV07 opt, byte playerId)
        {
            Main.AllPlayerSpeed[playerId] = Main.RealOptionsData.AsNormalOptions()!.PlayerSpeedMod;
            if (Utils.IsActive(SystemTypes.Electrical))//もし停電発生した場合
                Main.AllPlayerSpeed[playerId] = SpeedInLightsOut.GetFloat();//Mareの速度を設定した値にする
        }

        public static void OnCheckMurder(PlayerControl killer)
        {
        }
        public static void FixedUpdate(PlayerControl player)
        {
        }
    }
}