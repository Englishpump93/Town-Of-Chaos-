using HarmonyLib;
using UnityEngine;
using static Il2CppSystem.Uri;
using static TownOfHost.Translator;

namespace TownOfHost
{
    //From The Other Roles source
    //https://github.com/Eisbison/TheOtherRoles/blob/main/TheOtherRoles/Patches/CredentialsPatch.cs
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    class PingTrackerPatch
    {
        private static GameObject modStamp;
        static void Prefix(PingTracker __instance)
        {
            if (modStamp == null)
            {
                modStamp = new GameObject("ModStamp");
                var rend = modStamp.AddComponent<SpriteRenderer>();
                rend.color = new Color(1, 1, 1, 0.5f);
                modStamp.transform.parent = __instance.transform.parent;
                modStamp.transform.localScale *= 0.6f;
            }
            float offset = (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started) ? 0.75f : 0f;
            modStamp.transform.position = HudManager.Instance.MapButton.transform.position + Vector3.down * offset;
        }

        static void Postfix(PingTracker __instance)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;
            __instance.text.text += Main.credentialsText;
            if (Options.NoGameEnd.GetBool()) __instance.text.text += $"\r\n" + Helpers.ColorString(Color.red, GetString("NoGameEnd"));
            if (Options.IsStandardHAS) __instance.text.text += $"\r\n" + Helpers.ColorString(Color.yellow, GetString("StandardHAS"));
            if (Options.CurrentGameMode() == CustomGameMode.HideAndSeek) __instance.text.text += $"\r\n" + Helpers.ColorString(Color.red, GetString("HideAndSeek"));
            if (Options.EnableGM.GetBool()) __instance.text.text += $"\r\n" + Helpers.ColorString(Color.red, $"{GetString("GM")} Is On");
            if (Main.CachedDevMode) __instance.text.text += "\r\n" + Helpers.ColorString(Color.green, "DEV MODE");
            if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                __instance.gameObject.GetComponent<AspectPosition>().DistanceFromEdge = PlayerControl.LocalPlayer.Data.IsDead ? new Vector3(2.0f, 0.0f, 0f) : new Vector3(1.2f, 0.0f, 0f);
            else
            {
                var offset_x = 1.2f; //右端からのオフセット
                if (HudManager.InstanceExists && HudManager._instance.Chat.chatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
                __instance.gameObject.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(2.7f, 0.0f, 0f);
                if (Options.IsStandardHAS && !CustomRoles.Sheriff.IsEnable() && !CustomRoles.SerialKiller.IsEnable() && CustomRoles.Egoist.IsEnable()) __instance.text.text += $"\r\n" + Helpers.ColorString(Color.red, GetString("Warning.EgoistCannotWin"));
            }
        }
        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        class VersionShowerPatch
        {
            private static TMPro.TextMeshPro ErrorText;
            static void Postfix(VersionShower __instance)
            {
                Main.credentialsText = $"\r\n<color={Main.modColor}>Town Of Chaos</color>\r\n<color={Main.modColor}>v{Main.PluginVersion}b" + (Main.CachedDevMode ? Main.FullDevVersion : "") + $"</color>\r\nSource Code by: Discussions\r\nEdited By PUMPkin, 단풍잎";
                Main.versionText = $"\r\nTown Of Chaos v{Main.PluginVersion}b" + (Main.CachedDevMode ? Main.FullDevVersion : "") + $"\r\nSource Code by: Discussions\r\nEdited By PUMPkin, 단풍잎";
                if (Main.NewYears)
                {
                    Main.credentialsText += "\r\nHAPPY NEW YEAR!";
                    Main.versionText += "\r\nHAPPY NEW YEAR!";
                }
                if (ThisAssembly.Git.Branch != "main")
                {
                    Main.credentialsText += $"\r\n<color={Main.modColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
                    Main.versionText += $"\r\n{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})";
                }
                var credentials = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(__instance.text);
                credentials.text = Main.credentialsText;
                credentials.alignment = TMPro.TextAlignmentOptions.Right;
                credentials.transform.position = new Vector3(1f, 2.65f, -2f);

                if (Main.hasArgumentException && !Main.ExceptionMessageIsShown)
                {
                    Main.ExceptionMessageIsShown = true;
                    ErrorText = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(__instance.text);
                    ErrorText.transform.position = new Vector3(0, 0.20f, 0);
                    ErrorText.alignment = TMPro.TextAlignmentOptions.Center;
                    ErrorText.text = $"エラー:Lang系DictionaryにKeyの重複が発生しています!\r\n{Main.ExceptionMessage}";
                    ErrorText.color = Color.red;
                }
            }
        }
        [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
        class AwakePatch
        {
            public static void Prefix(ModManager __instance)
            {
                __instance.ShowModStamp();
                LateTask.Update(Time.deltaTime);
                CheckMurderPatch.Update();
            }
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        class LogoPatch
        {
            public static GameObject amongUsLogo;
            public static GameObject PlayLocalButton;
            public static GameObject PlayOnlineButton;
            public static GameObject HowToPlayButton;
            public static GameObject FreePlayButton;
            public static GameObject BottomButtons;
            static void Postfix(PingTracker __instance)
            {
                if ((PlayLocalButton = GameObject.Find("PlayLocalButton")) != null)
                {
                    PlayLocalButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    PlayLocalButton.transform.position = new Vector3(-0.76f, -1.6f, 0f);
                }

                if ((PlayOnlineButton = GameObject.Find("PlayOnlineButton")) != null)
                {
                    PlayOnlineButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    PlayOnlineButton.transform.position = new Vector3(0.725f, -1.6f, 0f);
                }

                if ((HowToPlayButton = GameObject.Find("HowToPlayButton")) != null)
                {
                    HowToPlayButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    HowToPlayButton.transform.position = new Vector3(-2.225f, -1.675f, 0f);
                }

                if ((FreePlayButton = GameObject.Find("FreePlayButton")) != null)
                {
                    FreePlayButton.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    FreePlayButton.transform.position = new Vector3(2.1941f, -1.675f, 0f);
                }

                var amongUsLogo = GameObject.Find("bannerLogo_AmongUs");
                if (amongUsLogo != null)
                {
                    amongUsLogo.transform.localScale *= 0.4f;
                    amongUsLogo.transform.position += Vector3.up * 0.25f;
                }

                var CustomBG = new GameObject("TOC3");
                CustomBG.transform.position = new Vector3((float)+2.0, (float)-0.1, 520f);
                //tohLogo.transform.localScale *= 1.2f;
                var renderer = CustomBG.AddComponent<SpriteRenderer>();
                renderer.sprite = Helpers.LoadSpriteFromResources("TownOfHost.Resources.TOC3.png", 179f);
            }

        }
    }
}