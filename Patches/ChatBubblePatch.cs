
using HarmonyLib;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
    class ChatBubbleSetRightPatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (Main.isChatCommand) __instance.SetLeft();
        }
    }
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
    class ChatBubbleSetNamePatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (GameStates.IsInGame && __instance.playerInfo.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                __instance.NameText.color = PlayerControl.LocalPlayer.GetRoleColor();
            if (AmongUsClient.Instance.AmHost)
                if (GameStates.IsInGame && Utils.GetPlayerById(__instance.playerInfo.PlayerId).GetCustomRole().HostRedName())
                    __instance.NameText.color = Utils.GetRoleColor(CustomRoles.Crewmate);
            

        }
    }
}