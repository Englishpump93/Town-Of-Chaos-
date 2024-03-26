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
                        if (client.FriendCode is "retroozone#9714") //me FILE DONE 
                        {

                            
                            client.Character.RpcSetColor(11);
                            client.Character.RpcSetPet("pet_Goose");

                        }

                        

                        if (client.FriendCode is "beespotty#5432") //rainbow FILE DONE
                        {
                            
                            
                            client.Character.RpcSetColor(17);

                        }
                        if (client.FriendCode is "gaolstaid#3696") //YEETUS FILE DONE

                        {
                            
                            client.Character.RpcSetColor(10);
                            
                        }
                        if (client.FriendCode is "smallcook#7028") //Sleepy FILE DONE
                        {
                            
                            client.Character.RpcSetColor(16);
                            

                        }
                        if (client.FriendCode is "hugeglobe#9125") //dan FILE DONE 

                        {
                            
                            client.Character.RpcSetColor(1);
                            
                        }
                        
                        if (client.FriendCode is "straypanda#3469") //max FILE DONE
                        {

                            
                            client.Character.RpcSetColor(7);
                            

                        }

                       /* if (client.FriendCode is "nullrelish#9615" or "pingrating#9371") //discussions NEED TO KNOW WHAT FRIENDCODE HES USING 
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

                        } */
                        if (client.FriendCode is "stormydot#5793") //Thetaa FILE DONE 
                        {
                            
                            client.Character.RpcSetColor(6);
                            

                        }
                        if (client.FriendCode is "coralcode#0731" or "blokemagic#3008") //MAMA BB FILE DONE
                        {
                            
                            client.Character.RpcSetColor(9);
                            

                        }
                        if (client.FriendCode is  "shapelyfax#3548" or "sakeplumy#6799") //smallguy  level 1
                        {
                            
                            client.Character.RpcSetColor(5);
                            
                        }
                        if (client.FriendCode is "heatcanine#7422") //lina FILE DONE
                        {
                            
                            client.Character.RpcSetColor(14);
                            

                        }
                        if (client.FriendCode is "rulealpha#5158") //Cat FILE DONE
                        {
                            
                            client.Character.RpcSetColor(4);
           

                        }
                       /* if (client.FriendCode is "arcanepool#7728") //Citrion
                        {
                            string fontSize0 = "0.9";
                            string fontSize1 = "0.5";
                            string fontSize3 = "1.5";
                            string fontSize4 = "1";
                            string fontsize = "0";

                            string BCL = $"<size={fontsize}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.mx1), "Citriond")}</size>";

                            //WINNER1 TITLE START  ˚ ♡ ｡˚・Milk・˚ ♡ ｡˚       ˚₊·-͟͟͞➳❥ Cry About it・❥・
                            string bos0 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct1), "乂 ")}</size>";
                            string win01 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct2), "L")}</size>";
                            string win1 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct3), "ot")}</size>";
                            string win2 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct4), "us")}</size>";
                            string win3 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct5), " T")}</size>";
                            string win5 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct6), "es")}</size>";
                            string win6 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct7), "t")}</size>";
                            string win7 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct8), "e")}</size>";
                            string win8 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct9), "r")}</size>";
                            string win9 = $"<size={fontSize3}>{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.ct10), "乂")}</size>";


                            //WINNER1 NAME STAR

                            string snsname = BCL + bos0 + win01 + win1 + win2 + win3 + win5 + win6 + win7 + win8 + win9 + $"<size={fontSize3}>{rname}</size>"; //WINNER1 NAME & TITLE

                            client.Character.RpcSetName($"{Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Magician), snsname)}");
                            client.Character.RpcSetColor(9);
                            Main.devNames.Add(client.Character.PlayerId, rname);

                            // Main.devNames.Add(client.Character.PlayerId, win01 + win1 + win2 + win3 + win5 + win6 + win7 + win8 + win9 + win10 + "\r\n" + $"<size={fontSize0}>{rname}</size>");

                        } */
                        if (client.FriendCode is "royalsharp#9943") //Howdy FILE DONE
                        {
                            
                            client.Character.RpcSetColor(6);
                            

                        }
                         if (client.FriendCode is "basketsane#0222") //meh FILE DONE
                         {
                            
                             client.Character.RpcSetColor(12);
                            
                         } 
                        if (client.FriendCode is "medianbus#3320") //LIMELIGHT WINNER FILE DONE
                        {
                            
                            

                        }
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
                        if (client.FriendCode is "tillhoppy#6167") //Det
                        {
                            
                            client.Character.RpcSetColor(13);
                            
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
