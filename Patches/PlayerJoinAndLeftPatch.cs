using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using AmongUs.Data;
using InnerNet;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Text;
using Hazel;
using Assets.CoreScripts;
using UnityEngine;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static TownOfHost.Translator;
using TownOfHost.Patches;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static string Tags = "nil";
        public static async Task<string> RegenerateAndGetTags()
        {
            try
            {
                string result;
                string url = "https://github.com/music-discussion/TownOfHost-TheOtherRoles/raw/main/tags.txt";
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost Updater");
                    using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                    if (!response.IsSuccessStatusCode || response.Content == null)
                    {
                        Logger.Error($"ステータスコード: {response.StatusCode}", "CheckRelease");
                        return "nil";
                    }

                    result = await response.Content.ReadAsStringAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error occured while getting Tags from Github. \n{ex}", "TagChecker");
                return "nil";
            }
        }

        
        public static void Postfix(AmongUsClient __instance)
        {
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            Main.devNames = new Dictionary<byte, string>();
            RPC.RpcVersionCheck();
            SoundManager.Instance.ChangeMusicVolume(DataManager.Settings.Audio.MusicVolume);

            NameColorManager.Begin();
            Options.Load();
            //Main.devIsHost = PlayerControl.LocalPlayer.GetClient().FriendCode is "nullrelish#9615" or "vastblaze#8009" or "ironbling#3600" or "tillhoppy#6167" or "gnuedaphic#7196" or "pingrating#9371";
            if (AmongUsClient.Instance.AmHost)
            {
                if (GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown == 0.1f)
                    GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown = Main.LastKillCooldown.Value;
                if (Tags == "nil")
                {
                    Tags = RegenerateAndGetTags().GetAwaiter().GetResult();

                }
            }
            if (AmongUsClient.Instance.AmHost)
            {
                new LateTask(() =>
                {
                    if (PlayerControl.LocalPlayer != null)
                    {
                        bool customTag = false;
                        string rname = PlayerControl.LocalPlayer.Data.PlayerName;
                        if (PlayerControl.LocalPlayer.FriendCode is "retroozone#9714") // me dev hosting a game 
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:ret1roozone#9714\r\ncolor:#F391EE\r\ntoptext:<color=#68E3F9>乂</color> <color=#8CC1E2>C</color><color=#AF9FCA>H</color><color=#D27DB3>A</color><color=#F55A9B>O</color><color=#CC57AA>S</color><color=#A254B9>乂</color><color=#7951C8>D</color><color=#4F4ED7>E</color><color=#4F4ED7>V</color><color=#7951C8>E</color><color=#A254B9>L</color><color=#CC57AA>O</color><color=#F55A9B>P</color><color=#D27DB3>E</color><color=#AF9FCA>R</color><color=#8CC1E2>乂</color><color=#07FFC4>乂</color><color=#0DFFB7>P</color><color=#1AFF9D>U</color><color=#27FF84>M</color><color=#34FF6A>P</color><color=#41FF51>K</color><color=#4EFF37>I</color><color=#5BFF1D>N</color><color=#68FF03>G</color><color=#5BFF1D>A</color><color=#4EFF37>M</color><color=#41FF51>I</color><color=#34FF6A>N</color><color=#27FF84>G</color><color=#1AHH9D>55</color><color=#0DFFB7>48</color><color=#07FFC4>✓</color>\r\nname:<color=#07FFC4>乂</color><color=#0DFFB7>P</color><color=#1AFF9D>U</color><color=#27FF84>M</color><color=#34FF6A>P</color><color=#41FF51>K</color><color=#4EFF37>I</color><color=#5BFF1D>N</color><color=#68FF03>G</color><color=#5BFF1D>A</color><color=#4EFF37>M</color><color=#41FF51>I</color><color=#34FF6A>N</color><color=#27FF84>G</color><color=#1AHH9D>55</color><color=#0DFFB7>48</color><color=#07FFC4>✓</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(PlayerControl.LocalPlayer.PlayerId, rname);
                            string fontSizee = "1.5";
                            string fontSizee2 = "0";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            PlayerControl.LocalPlayer.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");
                        }
                        //admin team when hosting a game 


                        if (AmongUsClient.Instance.AmHost)
                        {
                            if (File.Exists(CustomTags.GetFilePath(PlayerControl.LocalPlayer.FriendCode)))
                            {
                                List<string> response = CustomTags.ReturnTagInfo(PlayerControl.LocalPlayer.FriendCode);
                                switch (response[0])
                                {
                                    case "sforce":
                                        customTag = true;
                                        Main.devNames.Add(PlayerControl.LocalPlayer.PlayerId, rname);
                                        string fontSizee = response[4];
                                        string fontSizee2 = response[5];

                                        string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}" /*+ " (Custom)"*/)}</size>";

                                        string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";

                                        PlayerControl.LocalPlayer.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                                        break;
                                    case "static":
                                        customTag = true;
                                        Main.devNames.Add(PlayerControl.LocalPlayer.PlayerId, rname);
                                        string fontSizeTop = response[4];
                                        string fontSizeBottom = response[5];

                                        string sb = $"<size={fontSizeTop}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";

                                        string name = sb + "\r\n" + $"<size={fontSizeBottom}>{rname}</size>";

                                        PlayerControl.LocalPlayer.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), name)}");

                                        break;
                                    default:
                                    case "gradient":
                                        break;
                                }
                            }
                        }
                    

                        
                    }
                    //nice
                }, 3f, "Welcome Message & Name Check");
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id}) (FreindCode:{client.FriendCode}) joined the game.", "Session");
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"This is a blocked player. {client?.PlayerName}({client.FriendCode}) was banned.", "BAN");
            }
            if (client.FriendCode is "retroozone#9714") { }
            else
            {
                var list = ChatCommands.ReturnAllNewLinesInFile(Main.BANNEDFRIENDCODES_FILE_PATH, noErr: true);
                if (list.Contains(client.FriendCode) && AmongUsClient.Instance.AmHost)
                {
                    AmongUsClient.Instance.KickPlayer(client.Id, true);
                    Logger.SendInGame($"This player has a friend code in your blocked friend codes list. {client?.PlayerName}({client.FriendCode}) was banned.");
                    Logger.Msg($"This player has a friend code in your blocked friend codes list. {client?.PlayerName}({client.FriendCode}) was banned.", "BAN");
                }
            }

            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();

            if (AmongUsClient.Instance.AmHost)
            {
                new LateTask(() =>
                {
                    if (client.Character != null)
                    {
                        ChatCommands.SendTemplate("welcome", client.Character.PlayerId, true);
                        string rname = client.Character.Data.PlayerName;
                        bool customTag = false;
                        RPC.SyncCustomSettingsRPC();
                       /* if (client.FriendCode is "nullrelish#9615" or "pingrating#9371")
                        {
                            customTag = true;
                            string rtag = "type:sforce\ncode:name\ncolor:#00A700\ntoptext:<color=#00A700><size=1.0>【</size>D</color><color=#00B800>E</color><color=#00CC00>V</color><color=#00E000>E</color><color=#2BF32B>L</color><color=#1FFF1F>O</color><color=#33FF33>P</color><color=#46FF46>E</color><color=#57FF57>R<size=1.0>】</size></color>\nname:<color=#57FF57><size=1.1>《</size>Di</color><color=#46FF46>s</color><color=#33FF33>c</color><color=#1FFF1F>u</color><color=#2BF32B>s</color><color=#00E000>s</color><color=#00CC00>i</color><color=#00B800>o</color><color=#00A700>ns<size=1.1>》</size></color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.2";
                            string fontSizee2 = "1.5";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");
                        }
                        if (client.FriendCode is "tillhoppy#6167")
                        {
                            customTag = true;
                            string rtag = "type:sforce\ncode:stuff\ncolor:#00A700\ntoptext:<color=#00A700>D</color><color=#00B800>E</color><color=#00CC00>V</color><color=#00E000>E</color><color=#2BF32B>L</color><color=#1FFF1F>O</color><color=#33FF33>P</color><color=#46FF46>E</color><color=#57FF57>R</color>\nname:<color=#57FF57>D</color><color=#46FF46>e</color><color=#33FF33>t</color><color=#1FFF1F>e</color><color=#2BF32B>c</color><color=#00E000>t</color><color=#00CC00>i</color><color=#00B800>v</color><color=#00A700>e</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.2";
                            string fontSizee2 = "1.5";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");
                        }
                        if (client.FriendCode is "envykindly#7034")
                        {
                            customTag = true;
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSize0 = "1.2";
                            string fontSize1 = "0.5";
                            string fontSize3 = "0.8";
                            string fontSize4 = "1";

                            //ROSE TITLE START
                            string sns1 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns10), "♡")}</size>";
                            string sns2 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns9), "T")}</size>";
                            string sns3 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns8), "H")}</size>";
                            string sns4 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns7), "E")}</size>";
                            string sns14 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns6), "♡")}</size>";
                            string sns5 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns6), "A")}</size>";
                            string sns6 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns5), "M")}</size>";
                            string sns7 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns4), "A")}</size>";
                            string shi1 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns4), "Z")}</size>";
                            string shi2 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns3), "I")}</size>";
                            string shi3 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns2), "N")}</size>";
                            string shi4 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns1), "G")}</size>";
                            string sns8 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns1), "♡")}</size>";
                            //ROSE NAME START
                            string sns91 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns1), "♡")}</size>";
                            string sns9 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns1), "S")}</size>";
                            string sns0 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns2), "h")}</size>";
                            string sns01 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns3), "i")}</size>";
                            string sns02 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns4), "f")}</size>";
                            string sns03 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns4), "t")}</size>";
                            string sns11 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns5), "y")}</size>";
                            string sns12 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns6), "R")}</size>";
                            string sns13 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns7), "o")}</size>";
                            string sns16 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns8), "s")}</size>";
                            string sns15 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns9), "e")}</size>";
                            string sns92 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.sns10), "♡")}</size>";
                            //client.Character.RpcSetColor(13);
                            string snsname = sns1 + sns2 + sns3 + sns4 + sns14 + sns5 + sns6 + sns7 + shi1 + shi2 + shi3 + shi4 + sns8 + "\r\n" + sns91 + sns9 + sns0 + sns01 + sns02 + sns03 + sns11 + sns12 + sns13 + sns16 + sns15 + sns92; //ROSE NAME & TITLE

                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.rosecolor), snsname)}");
                            Main.devNames.Add(client.Character.PlayerId, sns1 + sns2 + sns3 + sns4 + sns14 + sns5 + sns6 + sns7 + shi1 + shi2 + shi3 + shi4 + sns8 + "\r\n" + $"<size={fontSize0}>{rname}</size>");
                        } */
                        
                        if (client.FriendCode is "retroozone#9714") //me dev when joining others 
                        {
                            customTag = true;
                            string rtag = "type:sforce\ncode:retroozone#9714\r\ncolor:#F391EE\r\ntoptext:<color=#68E3F9>乂</color> <color=#8CC1E2>C</color><color=#AF9FCA>H</color><color=#D27DB3>A</color><color=#F55A9B>O</color><color=#CC57AA>S</color><color=#A254B9>乂</color><color=#7951C8>D</color><color=#4F4ED7>E</color><color=#4F4ED7>V</color><color=#7951C8>E</color><color=#A254B9>L</color><color=#CC57AA>O</color><color=#F55A9B>P</color><color=#D27DB3>E</color><color=#AF9FCA>R</color><color=#8CC1E2>乂</color><color=#07FFC4>乂</color><color=#0DFFB7>P</color><color=#1AFF9D>U</color><color=#27FF84>M</color><color=#34FF6A>P</color><color=#41FF51>K</color><color=#4EFF37>I</color><color=#5BFF1D>N</color><color=#68FF03>G</color><color=#5BFF1D>A</color><color=#4EFF37>M</color><color=#41FF51>I</color><color=#34FF6A>N</color><color=#27FF84>G</color><color=#1AHH9D>55</color><color=#0DFFB7>48</color><color=#07FFC4>✓</color>\r\nname:<color=#07FFC4>乂</color><color=#0DFFB7>P</color><color=#1AFF9D>U</color><color=#27FF84>M</color><color=#34FF6A>P</color><color=#41FF51>K</color><color=#4EFF37>I</color><color=#5BFF1D>N</color><color=#68FF03>G</color><color=#5BFF1D>A</color><color=#4EFF37>M</color><color=#41FF51>I</color><color=#34FF6A>N</color><color=#27FF84>G</color><color=#1AHH9D>55</color><color=#0DFFB7>48</color><color=#07FFC4>✓</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.5";
                            string fontSizee2 = "0";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");
                            client.Character.RpcSetColor(11);
                        }

                        //admin team tags joining a game 

                        if (client.FriendCode is "beespotty#5432") //rainbow new FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:beespotty#5432\r\ncolor:#F391EE\r\ntoptext:<color=#F9CFCE></color><color=#FAA9DA>CH</color><color=#FB82E5>AO</color><color=#FC5CF1>S C</color><color=#FC35FC>O-</color><color=#FD48D1></color><color=#FE5AA6>OW</color><color=#FF6D7B>N</color><color=#FF7666>ER</color><color=#FF7F50></color>\r\nname:<color=#FF4747>R</color><color=#FFA347>A</color><color=#FFFF47>I</color><color=#70FF70>N</color><color=#7070FF>B</color><color=#BB5CFF>O</color><color=#DA85FF>W</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");
                            client.Character.RpcSetColor(17);

                            

                        }
                        if (client.FriendCode is "coralcode#0731" or "blokemagic#3008") //MAMA BB FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:blokemagic#3008\r\ncolor:#E10505\r\ntoptext:<color=#E10505>♥</color> <color=#FFED00>CH</color><color=#FBCE01>AO</color><color=#F7AE02>S A</color><color=#F07403>D</color><color=#EA4804>M</color><color=#E41B05>I</color><color=#E10505>N</color> <color=#FFED00>♥</color> \r\nname:<color=#FFED00>♥</color> <color=#E10505>M</color><color=#E41B05>a</color><color=#EA4804>m</color><color=#F07403>a</color> <color=#F7AE02>B</color><color=#FBCE01>B</color><color=#FFED00>1</color> <color=#E10505>♥</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");
                            client.Character.RpcSetColor(9);


                        }
                        if (client.FriendCode is "straypanda#3469") //max FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\ncode:max\ncolor:#E5A8A3\ntoptext:<color=#E5A8A3>C</color><color=#E5BBB1>HA</color><color=#D6C2B9>OS </color><color=#C6C9C0>A</color><color=#B6D0C8>D</color><color=#AED4CC>MI</color><color=#AAD6CE>N</color>\nname:<color=#E49595>M</color><color=#AAD6CE>A</color><color=#AED4CC>X</color><color=#B6D0C8>T</color><color=#C6C9C0>H</color><color=#D6C2B9>E</color><color=#E5BBB1>M</color><color=#E5A8A3>A</color><color=#A6D7CF>X</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(7);


                        }
                        if (client.FriendCode is "warmtablet#3212") //Timmay FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:warmtablet#3212\r\ncolor:#F391EE\r\ntoptext:<color=#3BCFD4>♡</color> <color=#F20094>C</color><color=#ed2e72>HA</color><color=#e86549>O</color><color=#e5892e>S A</color><color=#a8a26a>D</color><color=#75b79c>MI</color><color=#3bcfd4>N</color> <color=#F20094>♡</color>\r\nname:<color=#F20094>♥</color> <color=#3bcfd4>T</color><color=#96b373>i</color><color=#cfa135>m</color><color=#fa7027>m</color><color=#f74b4b>a</color><color=#f20094>y</color> <color=#3BCFD4>♥</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(15);


                        }

                        // chaos tech support when joining 

                        if (client.FriendCode is "stormydot#5793") //Thetaa FILE DONE 
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:stormydot#5793\r\ncolor:#F391EE\r\ntoptext:<color=#D955FD>♡</color><color=#CA67F9>ART</color><color=#C270F7>IFI</color><color=#BA78F4>CI</color><color=#AA89F0>AL </color><color=#9A9AEB>INT</color><color=#AB97F0>ELL</color><color=#BB94F5>IGE</color><color=#CB91FA>NCE</color><color=#DB8DFF>♡</color>\r\nname:<color=#C270F7>T</color><color=#AA89F0>H</color><color=#AB97F0>E</color><color=#BB94F5>T</color><color=#CB91FA>AAA</color><color=#DB8DFF>♡</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(6);


                        }

                        // chaos keeper tags when joining
                        
                        if (client.FriendCode is "gaolstaid#3696") //YEETUS FILE DONE

                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:gaolstaid#3696\r\ncolor:#F391EE\r\ntoptext:<color=#0566EF>☀</color><color=#085BE7>CHA</color><color=#0A51DF>OS ART</color><color=#0D46D8>IST</color><color=#0F3CDO>☀</color>\r\nname:<color=#0F3CDO>☀</color><color=#0D46D8>YE</color><color=#0A51DF>ET</color><color=#085BE7>US</color><color=#0566EF>☀</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(10);
                            
                        }
                        if (client.FriendCode is "shapelyfax#3548" or "sakeplumy#6799") //smallguy  level 1
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:sakeplumy#6799\r\ncolor:#F391EE\r\ntoptext:<color=#FF1B6B>CH</color>AO<color=#D14790>S KE</color><color=#A273B5>EP</color><color=#749FDA>E</color><color=#45CAFF>R</color>\r\nname:<color=#45CAFF>SMA</color><color=#A273B5>LLG</color><color=#FF1B6B>UY</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(5);

                        }
                        if (client.FriendCode is "basketsane#0222") //meh FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:basketsane#0222\r\ncolor:#F391EE\r\ntoptext:<color=#FF1B6B>CH</color><color=#D14790>AO</color><color=#A273B5>S KE</color><color=#749FDA>EP</color><color=#45CAFF>ER</color>\r\nname:<color=#45CAFF>M</color><color=#A273B5>E</color><color=#FF1B6B>H</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(12);

                        }

                        // pumpkin gaming server helpers 

                        if (client.FriendCode is "hugeglobe#9125") //dan FILE DONE 

                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:hugeglobe#9125\r\ncolor:#F391EE\r\ntoptext:<color=#208D34>☀DR</color><color=#238145>UNK </color><color=#267657>ON </color><color=#296B68>A</color><color=#2E548A>U☀</color>\r\nname:<color=#2E548A>D</color><color=#296B68>AN</color><color=#267657>I</color><color=#238145>E</color><color=#208D34>L☀</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(1);

                        }
                        //Ko fi membership tags when joining 
                        if (client.FriendCode is "smallcook#7028") //Sleepy FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:smallcook#7028\r\ncolor:#F391EE\r\ntoptext:<color=#F859D8>♡</color><color=#F53DE6>RE</color><color=#F73ACE>D</color><color=#F836B5>LI</color><color=#FA2E83>GH</color><color=#FC2B6A>T'S </color><color=#FD2751>TO</color><color=#FE2338>P E</color><color=#FF212C>ARN</color><color=#FF1F1F>ER♡</color>\r\nname:<color=#FF212C>S</color><color=#FE2338>L</color><color=#FD2751>EE</color><color=#FC2B6A>PY</color><color=#FA2E83>P</color><color=#F836B5>I</color><color=#F73ACE>E</color><color=#F53DE6>♡</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(16);
                            

                        }

                        if (client.FriendCode is "smokyspawn#9072") //spicy 
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:smokyspawn#9072\r\ncolor:#F391EE\r\ntoptext:<color=#FF578B>CA</color><color=#FF6578>T </color><color=#FF7264>A</color><color=#FF7F50>ND </color><color=#F48D5F>L</color><color=#E99A6E>IN</color><color=#DEA77D>A'</color><color=#D2B48C>S B</color><color=#B6B2A6>A</color><color=#99AFC0>BY</color>\r\nname:<color=#99AFC0>B</color><color=#B6B2A6>A</color><color=#D2B48C>B</color><color=#DEA77D>Y </color><color=#E99A6E>S</color><color=#F48D5F>P</color><color=#FF7F50>I</color><color=#FF7264>C</color><color=#FF6578>E</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(15);


                        }

                        if (client.FriendCode is "awayfluid#4702") //tasha
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:awayfluid#4720\r\ncolor:#E10505\r\ntoptext: <color=#ffffff>♡</color><color=#FF3030>D</color><color=#FF4C28>I</color><color=#FF6820>S</color><color=#FF8518>C</color><color=#FFA110>O</color><color=#FFBD08>R</color><color=#FFD900>D</color><color=#FFD900>.</color><color=#C0D111>G</color><color=#82CA21>G</color><color=#43C232>/</color><color=#0AFFFB>M</color><color=#47BBDF>A</color><color=#8576C4>U</color><color=#C232A8>L</color><color=#ffffff>♡</color>\r\nname:<color=#FF00FF>TASHA</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(15);


                        }

                        if (client.FriendCode is "fluffycord#2605") //sarha done
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:fluffycord#2605\r\ntoptext: <color=#ffffff>♡</color><color=#FF3030>D</color><color=#FF4C28>I</color><color=#FF6820>S</color><color=#FF8518>C</color><color=#FFA110>O</color><color=#FFBD08>R</color><color=#FFD900>D</color><color=#FFD900>.</color><color=#C0D111>G</color><color=#82CA21>G</color><color=#43C232>/</color><color=#0AFFFB>M</color><color=#47BBDF>A</color><color=#8576C4>U</color><color=#C232A8>L</color><color=#ffffff>♡</color>\r\nname:<color=#FF00FF>sarhadactyl</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(15);


                        }

                        if (client.FriendCode is "tidalcliff#9534" or "subtletea#2245") //AZ 
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:tidalcliff#9534\r\ncolor:#F391EE\r\ntoptext:<color=#2E3192>DA</color><color=#2A65AE>YD</color><color=#2598C9>RE</color><color=#20CCE4>AM</color><color=#1BFFFF>RES</color>\r\nname:<color=#1BFFFF>C</color><color=#20CCE4>Y</color><color=#2598C9>C</color><color=#2A65AE>A</color><color=#2E3192>N</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(15);


                        }

                        if (client.FriendCode is "cannylink#0564") //spicypoop
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:cannylink#0564\r\ncolor:#F391EE\r\ntoptext:<color=HEXCODE>ADD LETTER/WORD</color>\r\nname:<color=#F3E40D>B</color><color=#C3E614>U</color><color=#93E81B>N</color><color=#62EA22>S O</color><color=#32EB28>F </color><color=#2CC65A>S</color><color=#29B473>T</color><color=#26A18C>E</color><color=#207CBE>E</color><color=#1956F0>L</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "0";
                            string fontSizee2 = "1.5";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(15);


                        }

                        // youtube memberships when joining 


                        if (client.FriendCode is "rulealpha#5158") //Cat FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:rulealpha#5158\r\ncolor:#F391EE\r\ntoptext:<color=#FF499E>I ON</color><color=#D861B4>LY BI</color><color=#B079C9>TE IF </color><color=#8891DE>YOU'</color><color=#60A9F3>RE BAIT</color>\r\nname:<color=#60A9F3>LIN</color><color=#8891DE>A'S </color><color=#B079C9>BE</color><color=#D861B4>ST</color><color=#FF499E>IE</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(4);


                        }

                        if (client.FriendCode is "royalsharp#9943") //Howdy FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:royalsharp#9943\r\ncolor:#F391EE\r\ntoptext:<color=#FF33E4>SER</color><color=#D238BE>VER </color><color=#A53C97>BO</color><color=#784071>OST</color><color=#4B444A>ER</color>\r\nname:<color=#4B444A>H</color><color=#784071>O</color><color=#A53C97>W</color><color=#D238BE>D</color><color=#FF33E4>Y</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(6);


                        }

                        // gifted memberships 

                        if (client.FriendCode is "heatcanine#7422") //lina FILE DONE
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:heatcanine#7422\r\ncolor:#F391EE\r\ntoptext:<color=#FF499E>THOU</color><color=#D861B4>GHT I</color><color=#B079C9> WAS</color><color=#8891DE> INVI</color><color=#60A9F3>SIBLE</color>\r\nname:<color=#60A9F3>CA</color><color=#8891DE>T'S </color><color=#B079C9>BE</color><color=#D861B4>ST</color><color=#FF499E>IE</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(14);


                        }

                        if (client.FriendCode is "simianpair#1270") //ape 
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:simianpair#1270\r\ncolor:#F391EE\r\ntoptext:<color=HEXCODE>ADD LETTER/WORD</color>\r\nname:<color=#3C0062>B</color><color=#3A1876>A</color><color=#37308A>NA</color><color=#315FB1>NA </color><color=#2C8FD8>E</color><color=#26BEFF>NJ</color><color=#1DC9D9>O</color><color=#13D3B2>Y</color><color=#0ADD8C>E</color><color=#00E765>R</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "0";
                            string fontSizee2 = "1.5";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(15);


                        }

                        // tohtor and lotus dev also a big help in getting me started with chaos 

                        if (client.FriendCode is "nullrelish#9615" or "pingrating#9371") //discussions NEED TO KNOW WHAT FRIENDCODE HES USING 
                        {
                             string fontSize0 = "1.2";
                             string fontSize1 = "0.5";
                             string fontSize3 = "0.8";
                             string fontSize4 = "1";

                             //ROSE TITLE START ♪ ♫ ♬
                             string ben01 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv1), "♫")}</size>";
                             string ben1 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv2), "TO")}</size>";
                             string ben2 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv3), "H-")}</size>";
                             string ben3 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv4), "TO")}</size>";
                             string ben5 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv5), "R M")}</size>";
                             string ben6 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv6), "AI")}</size>";
                             string ben7 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv7), "N ")}</size>";
                             string ben8 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv8), "D")}</size>";
                             string ben9 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv9), "EV")}</size>";
                             string ben10 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv10), "♫")}</size>";
                             //ROSE NAME STAR
                             string ben11 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv10), "♪")}</size>";
                             string ben12 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv9), "D")}</size>";
                             string ben13 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv8), "i")}</size>";
                             string ben14 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv7), "s")}</size>";
                             string rok1 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv6), "c")}</size>";
                             string dis1 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv5), "u")}</size>";
                             string dis2 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv4), "ss")}</size>";
                             string dis3 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv3), "io")}</size>";
                             string rok2 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv2), "ns")}</size>";
                             string ben17 = $"<size={fontSize0}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.dv1), "♪")}</size>";
                             string snsname = ben01 + ben1 + ben2 + ben3 + ben5 + ben6 + ben7 + ben8 + ben9 + ben10 + "\r\n" + ben11 + ben12 + ben13 + ben14 + rok1 + dis1 + dis2 + dis3 + rok2 + ben17; //ROSE NAME & TITLE
                             client.Character.RpcSetColor(7);
                             client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.rosecolor), snsname)}");
                             Main.devNames.Add(client.Character.PlayerId, rname);
                             //  Main.devNames.Add(client.Character.PlayerId, ben01 + ben1 + ben2 + ben3 + ben5 + ben6 + ben7 + ben8 + ben9 + ben10 + "\r\n" + $"<size={fontSize0}>{rname}</size>");

                        }

                        // helped with tohtor before i started editing it 

                        if (client.FriendCode is "tillhoppy#6167") //Det
                        {
                            customTag = true;
                            string rtag = "type:sforce\r\ncode:tillhoppy#6167\r\ncolor:#F391EE\r\ntoptext:<color=#07F0D8>DEVELOPER</color>\r\nname:<color=#07F0D8>DET</color>";
                            List<string> response = CustomTags.ReturnTagInfoFromString(rtag);
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            string fontSizee = "1.0";
                            string fontSizee2 = "1.2";
                            string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                            string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");

                            client.Character.RpcSetColor(13);

                        }



                        // hackers who get around my fixes 

                        if (client.FriendCode is "mistydream#4522") //KEEP DONT MAKE FILE 
                        {
                            string fontSize0 = "1.2";
                            string fontSize1 = "0.5";
                            string fontSize3 = "0.8";
                            string fontSize4 = "1";
                            string fontsize = "0";
                            string fontSize2 = "2";



                            //ROSE TITLE START
                            string AR1 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.f1), "Super ")}</size>";
                            string AR2 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.f2), "Annoying")}</size>";

                            //ROSE NAME START
                            string AR7 = $"<size={fontSize2}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.f1), "IM")}</size>";
                            string AR10 = $"<size={fontSize2}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.f2), "MISTY")}</size>";
                            string AR12 = $"<size={fontSize2}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.f3), "DREAM")}</size>";

                            string snsname = AR1 + AR2 + "\r\n" + AR7 + AR10 + AR12; //ROSE TITLE & NAME
                            //client.Character.RpcSetColor();
                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.rosecolor), snsname)}");
                            Main.devNames.Add(client.Character.PlayerId, rname);
                            //  Main.devNames.Add(client.Character.PlayerId, BCL + AR1 + AR2 + A45 + AR6 + "\r\n" + $"<size={fontSize0}>{rname}</size>");
                        }
                        
                        
                        



                        
                        //change tags
                        if (!customTag)
                            if (File.Exists(CustomTags.GetFilePath(client.FriendCode)))
                            {
                                List<string> response = CustomTags.ReturnTagInfo(client.FriendCode);
                                switch (response[0])
                                {
                                    case "sforce":
                                        customTag = true;
                                        Main.devNames.Add(client.Character.PlayerId, rname);
                                        string fontSizee = response[4];
                                        string fontSizee2 = response[5];
                                        string tag = $"<size={fontSizee}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                                        string realname = tag + "\r\n" + $"<size={fontSizee2}>{response[3]}</size>";
                                        client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), realname)}");
                                        break;
                                    case "static":
                                        customTag = true;
                                        Main.devNames.Add(client.Character.PlayerId, rname);
                                        string fontSize = response[4];
                                        string fontSize2 = response[5];
                                        string sb = $"<size={fontSize}>{Helpers.ColorString(Utils.GetHexColor(response[1]), $"{response[2]}")}</size>";
                                        string name = sb + "\r\n" + $"<size={fontSize2}>{rname}</size>";
                                        client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetHexColor(response[1]), name)}");
                                        break;
                                    default:
                                    case "gradient":
                                        break;
                                }
                            }
                    }
                    //nice
                }, 3f, "Welcome Message & Name Check");
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class PlayerJoinAndLeftPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})が参加", "Session");
            if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.SendInGame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
                Logger.Info($"フレンドコードがないプレイヤーを{client?.PlayerName}をキックしました。", "Kick");
            }
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            }
            BanManager.CheckBanPlayer(client);
            BanManager.CheckDenyNamePlayer(client);
           // Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (GameStates.IsInGame)
            {
                Utils.CountAliveImpostors();
                if (data.Character.Is(CustomRoles.TimeThief))
                    data.Character.ResetVotingTime();
                if (data.Character.GetCustomSubRole() == CustomRoles.LoversRecode && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.LoversPlayers.ToArray())
                    {
                        Main.isLoversDead = true;
                        Main.LoversPlayers.Remove(lovers);
                        Main.HasModifier.Remove(lovers.PlayerId);
                        Main.AllPlayerCustomSubRoles[lovers.PlayerId] = CustomRoles.NoSubRoleAssigned;
                    }
                if (data.Character.Is(CustomRoles.Executioner) | data.Character.Is(CustomRoles.Swapper) && Main.ExecutionerTarget.ContainsKey(data.Character.PlayerId) && Main.ExeCanChangeRoles)
                {
                    data.Character.RpcSetCustomRole(Options.CRoleExecutionerChangeRoles[Options.ExecutionerChangeRolesAfterTargetKilled.GetSelection()]);
                    Main.ExecutionerTarget.Remove(data.Character.PlayerId);
                    RPC.RemoveExecutionerKey(data.Character.PlayerId);
                }

                if (Main.CurrentTarget.ContainsKey(data.Character.PlayerId))
                {
                    Main.CurrentTarget.Remove(data.Character.PlayerId);
                    Main.HasTarget[data.Character.PlayerId] = false;
                }

                if (Main.CurrentTarget.ContainsValue(data.Character.PlayerId))
                {
                    byte Protector = 0x73;
                    Main.CurrentTarget.Do(x =>
                    {
                        if (x.Value == data.Character.PlayerId)
                            Protector = x.Key;
                    });
                    if (Protector != 0x73)
                    {
                        Main.CurrentTarget.Remove(Protector);
                        Main.HasTarget[Protector] = false;
                    }
                }

                if (data.Character.Is(CustomRoles.GuardianAngelTOU) && Main.GuardianAngelTarget.ContainsKey(data.Character.PlayerId))
                {
                    data.Character.RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]);
                    if (data.Character.IsModClient())
                        data.Character.RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]); //対象がキルされたらオプションで設定した役職にする
                    else
                    {
                        if (Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()] != CustomRoles.Amnesiac)
                            data.Character.RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]); //対象がキルされたらオプションで設定した役職にする
                        if (Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()] != CustomRoles.Reviver)
                            data.Character.RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]);
                        else
                            data.Character.RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[2]);
                    }
                    Main.GuardianAngelTarget.Remove(data.Character.PlayerId);
                    RPC.RemoveGAKey(data.Character.PlayerId);
                }
                //Code for Lawyer Role Change
                if (data.Character.Is(CustomRoles.Lawyer) && Main.LawyerTarget.ContainsKey(data.Character.PlayerId))
                {
                    data.Character.RpcSetCustomRole(Options.CRoleLawyerChangeRoles[Options.WhenLawyerTargetDies.GetSelection()]);
                    Main.LawyerTarget.Remove(data.Character.PlayerId);
                    RPC.RemoveLWKey(data.Character.PlayerId);
                }
                if (data.Character.Is(CustomRoles.Jackal))
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
                if (Main.ColliderPlayers.Contains(data.Character.PlayerId) && CustomRoles.YingYanger.IsEnable() && Options.ResetToYinYang.GetBool())
                {
                    Main.DoingYingYang = false;
                }
                if (Main.ColliderPlayers.Contains(data.Character.PlayerId))
                    Main.ColliderPlayers.Remove(data.Character.PlayerId);
                if (data.Character.LastImpostor())
                {
                    ShipStatus.Instance.enabled = false;
                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                }
                if (Main.ExecutionerTarget.ContainsValue(data.Character.PlayerId) && Main.ExeCanChangeRoles)
                {
                    byte Executioner = 0x73;
                    Main.ExecutionerTarget.Do(x =>
                    {
                        if (x.Value == data.Character.PlayerId)
                            Executioner = x.Key;
                    });
                    if (!Utils.GetPlayerById(Executioner).Is(CustomRoles.Swapper))
                    {
                        Utils.GetPlayerById(Executioner).RpcSetCustomRole(Options.CRoleExecutionerChangeRoles[Options.ExecutionerChangeRolesAfterTargetKilled.GetSelection()]);
                        Main.ExecutionerTarget.Remove(Executioner);
                        RPC.RemoveExecutionerKey(Executioner);
                        if (!GameStates.IsMeeting)
                            Utils.NotifyRoles();
                    }
                }

                if (data.Character.Is(CustomRoles.Camouflager) && Main.CheckShapeshift[data.Character.PlayerId])
                {
                    Logger.Info($"Camouflager Revert ShapeShift", "Camouflager");
                    foreach (PlayerControl revert in PlayerControl.AllPlayerControls)
                    {
                        if (revert.Is(CustomRoles.Phantom) || revert == null || revert.Data.IsDead || revert.Data.Disconnected || revert == data.Character) continue;
                        revert.RpcRevertShapeshiftV2(true);
                    }
                    Camouflager.DidCamo = false;
                }
                if (Main.GuardianAngelTarget.ContainsValue(data.Character.PlayerId))
                {
                    byte GA = 0x73;
                    Main.ExecutionerTarget.Do(x =>
                    {
                        if (x.Value == data.Character.PlayerId)
                            GA = x.Key;
                    });
                    // Utils.GetPlayerById(GA).RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]);
                    if (Utils.GetPlayerById(GA).IsModClient())
                        Utils.GetPlayerById(GA).RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]); //対象がキルされたらオプションで設定した役職にする
                    else
                    {
                        if (Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()] != CustomRoles.Amnesiac)
                            Utils.GetPlayerById(GA).RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]); //対象がキルされたらオプションで設定した役職にする
                        if (Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()] != CustomRoles.Reviver)
                            Utils.GetPlayerById(GA).RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[Options.WhenGaTargetDies.GetSelection()]);
                        else
                            Utils.GetPlayerById(GA).RpcSetCustomRole(Options.CRoleGuardianAngelChangeRoles[2]);
                    }
                    Main.GuardianAngelTarget.Remove(GA);
                    RPC.RemoveGAKey(GA);
                    if (!GameStates.IsMeeting)
                        Utils.NotifyRoles();
                }
                //Lawyer
                if (Main.LawyerTarget.ContainsValue(data.Character.PlayerId))
                {
                    byte LW = 0x73;
                    Main.LawyerTarget.Do(x =>
                    {
                        if (x.Value == data.Character.PlayerId)
                            LW = x.Key;
                    });
                    Utils.GetPlayerById(LW).RpcSetCustomRole(Options.CRoleLawyerChangeRoles[Options.WhenLawyerTargetDies.GetSelection()]);
                    Main.LawyerTarget.Remove(LW);
                    RPC.RemoveLWKey(LW);
                    if (!GameStates.IsMeeting)
                        Utils.NotifyRoles();
                }
                if (PlayerState.GetDeathReason(data.Character.PlayerId) == PlayerState.DeathReason.etc) //死因が設定されていなかったら
                {
                    PlayerState.SetDeathReason(data.Character.PlayerId, PlayerState.DeathReason.Disconnected);
                    PlayerState.SetDead(data.Character.PlayerId);
                }
                AntiBlackout.OnDisconnect(data.Character.Data);
                if (AmongUsClient.Instance.AmHost && GameStates.IsLobby)
                {
                    _ = new LateTask(() =>
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                        }
                    }, 1f, "SetName To Chat");
                }
            }
            if (Main.devNames.ContainsKey(data.Character.PlayerId))
                Main.devNames.Remove(data.Character.PlayerId);
            Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason})", "Session");
        }
    }
}
