using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.Sockets;
using AmongUs.GameOptions;

[assembly: AssemblyFileVersionAttribute(TownOfHost.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHost.Main.PluginVersion)]
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host: The Other Roles", PluginVersion)]
    // Check if we are in the lobby
   

    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.discussions.tohtor";
        public static readonly string TEMPLATE_FILE_PATH = "./CHAOS/TEMPLATE.txt";
        public static readonly string ROLES_FILE_PATH = "./CHAOS/ROLES.txt";
        public static readonly string TIMMAY_FILE_PATH = "./CHAOS/warmtablet#3212.txt";
        public static readonly string PUMP_FILE_PATH = "./CHAOS/retroozone#9714.txt";
        public static readonly string MAMA_FILE_PATH = "./CHAOS/blokemagic#3008.txt";
        public static readonly string BOW_FILE_PATH = "./CHAOS/beespotty#5432.txt";
        public static readonly string YEETUS_FILE_PATH = "./CHAOS/gaolstaid#3696.txt";
        public static readonly string SG_FILE_PATH = "./CHAOS/sakeplumy#6799.txt";
        public static readonly string SG1_FILE_PATH = "./CHAOS/shapelyfax#3548.txt";
        public static readonly string MAX_FILE_PATH = "./CHAOS/straypanda#3469.txt";
        public static readonly string MEH_FILE_PATH = "./CHAOS/basketsane#0222.txt";
        public static readonly string HOWTO_FILE_PATH = "./CHAOS/HOW-TO-MAKE-TAGS.txt";
        public static readonly string NEWTAG_FILE_PATH = "./CHAOS/NEW-TAG.txt";
        public static readonly string BANNEDWORDS_FILE_PATH = "./CHAOS/BANNEDWORDS.txt";
        public static readonly string BANNEDFRIENDCODES_FILE_PATH = "./CHAOS/BANNEDFRIENDCODES.txt";
        public static readonly string RSETTINGS_FILE_PATH = @"CHAOS\RSETTINGS.txt";
        public static readonly string DiscordInviteUrl = "https://discord.gg/tohtor";
        public static readonly bool ShowDiscordButton = true;
        public const string PluginVersion = "0.0.2.9";
        public const string DevVersion = "2.2";
        public const string FullDevVersion = $" dev {DevVersion}";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static readonly bool ShowQQButton = true;
        public static readonly string QQInviteUrl = "https://jq.qq.com/?_wv=1027&k=2RpigaN6";
        public static bool CachedDevMode = false;
        public static string credentialsText;
        public static string versionText;
        public static bool IsAprilFools = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
        
        //Client Options
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> ForceJapanese { get; private set; }
        public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
        public static ConfigEntry<bool> UnlockFPS { get; private set; }
        public static ConfigEntry<bool> AmDebugger { get; private set; }
        public static ConfigEntry<string> ShowPopUpVersion { get; private set; }
        public static ConfigEntry<float> MessageWait { get; private set; }
        public static ConfigEntry<bool> ButtonImages { get; private set; }

        public static LanguageUnit EnglishLang { get; private set; }
        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        public static Dictionary<byte, string> devNames = new();
        //Other Configs
        public static ConfigEntry<bool> IgnoreWinnerCommand { get; private set; }
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static ConfigEntry<float> LastKillCooldown { get; private set; }
        public static CustomWinner currentWinner;
        public static HashSet<AdditionalWinners> additionalwinners = new();
        public static IGameOptions RealOptionsData;
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomSubRoles;
        public static Dictionary<byte, CustomRoles> LastPlayerCustomRoles;
        public static Dictionary<byte, CustomRoles> LastPlayerCustomSubRoles;
        public static Dictionary<byte, Color32> PlayerColors = new();
        public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, string> roleColors;
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#66FFC2";
        public static string StreamerColor = "#69ff93";
        public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable() && CustomRoles.Dracula.IsEnable();
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static List<byte> ResetCamPlayerList;
        public static List<byte> winnerList;
        public static List<(string, byte)> MessagesToSend;
        public static bool isChatCommand = false;
        public static string TextCursor => TextCursorVisible ? "_" : "";
        public static bool TextCursorVisible;
        public static float TextCursorTimer;
        public static List<PlayerControl> LoversPlayers = new(2);
        public static List<PlayerControl> cheatersPlayers = new();
        //Lawyer's Client
        public static List<PlayerControl> LawyerClient = new();
        public static bool isLoversDead = true;
        public static bool ExeCanChangeRoles = true;
        public static bool MercCanSuicide = true;
        public static bool satanCanSuicide = true;
        public static bool DoingYingYang = true;
        public static bool Grenaiding = false;
        public static bool ResetVision = false;
        public static bool IsInvis = false;
        public static bool IsInviswizard = false;
        public static bool IsInvispumpkin = false;

        public static Dictionary<byte, CustomRoles> HasModifier = new();
        public static List<CustomRoles> modifiersList = new();
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public static Dictionary<byte, (byte, float)> BitPlayers = new();
        public static Dictionary<byte, float> WarlockTimer = new();
        public static Dictionary<byte, PlayerControl> CursedPlayers = new();
        public static List<PlayerControl> SpelledPlayer = new();
        public static List<PlayerControl> Impostors = new();
        public static List<byte> AliveAtTheEndOfTheRound = new();
        public static List<byte> DeadPlayersThisRound = new();
        public static Dictionary<byte, bool> KillOrSpell = new();
        public static Dictionary<byte, bool> KillOrSilence = new();
        public static Dictionary<byte, bool> isCurseAndKill = new();
        public static Dictionary<byte, bool> RemoveChat = new();
        public static Dictionary<byte, bool> HasTarget = new();
        public static Dictionary<(byte, byte), bool> isDoused = new();
        public static List<byte> dousedIDs = new();
        public static Dictionary<(byte, byte), bool> isHexed = new();
        public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
        public static Dictionary<byte, float> AirshipMeetingTimer = new();
        public static Dictionary<byte, byte> ExecutionerTarget = new(); //Key : Executioner, Value : target
        public static Dictionary<byte, byte> GuardianAngelTarget = new(); //Key : GA, Value : target
        
        //Lawyer Target
        public static Dictionary<byte, byte> LawyerTarget = new(); //Key : LW, Value : client
        public static Dictionary<byte, byte> PuppeteerList = new(); // Key: targetId, Value: PuppeteerId
        public static Dictionary<byte, byte> WitchList = new(); // Key: targetId, Value: NeutWitchId
        public static Dictionary<byte, byte> WitchedList = new(); // Key: targetId, Value: WitchId
        public static Dictionary<byte, byte> CurrentTarget = new(); //Key : Player, Value : Target
        public static Dictionary<byte, byte> SpeedBoostTarget = new();
        public static Dictionary<byte, int> MayorUsedButtonCount = new();
        public static Dictionary<byte, int> HackerFixedSaboCount = new();
        public static Dictionary<byte, Vent> LastEnteredVent = new();
        public static Dictionary<byte, Vent> CurrentEnterdVent = new();
        public static Dictionary<byte, Vector2> LastEnteredVentLocation = new();
        public static int AliveImpostorCount;
        public static int AllImpostorCount;
        public static string LastVotedPlayer;
        public static bool CanTransport;
        public static int HexesThisRound;
        public static int SKMadmateNowCount;
        public static bool witchMeeting;
        public static bool isCursed;
        public static List<byte> firstKill = new();
        public static Dictionary<byte, List<byte>> knownGhosts = new();
        public static Dictionary<byte, (int, bool, bool, bool, bool)> SurvivorStuff = new(); // KEY - player ID, Item1 - NumberOfVests, Item2 - IsVesting, Item3 - HasVested, Item4 - VestedThisRound, Item5 - RoundOneVest
        public static List<byte> unreportableBodies = new();
        public static List<PlayerControl> SilencedPlayer = new();
        public static Dictionary<byte, int> DictatesRemaining = new();
        public static List<byte> ColliderPlayers = new();
        public static List<byte> KilledBewilder = new();
        public static List<byte> KilledDiseased = new();
        public static List<byte> KilledDemo = new();
        public static List<byte> KilledKami = new();
        public static bool isSilenced;
        public static bool isShipStart;
        public static bool showEjections;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<(byte, byte), string> targetArrows = new();
        public static List<PlayerControl> AllCovenPlayers = new();
        public static Dictionary<byte, byte> whoKilledWho = new();
        public static int WonFFATeam;
        public static byte WonTrollID;
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static byte WonPirateID;
        public static byte WonExecutionerID;
        public static byte WonHackerID;
        public static byte WonArsonistID;
        public static byte WonChildID;
        public static byte WonFFAid;
        public static bool CustomWinTrigger;
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static bool bkProtected = false;
        public static bool devIsHost = false;
        public static int DiscussionTime;
        public static int VotingTime;
        public static int JugKillAmounts;
        public static int AteBodies;
        public static byte currentDousingTarget;
        public static byte currentFreezingTarget;
        public static int VetAlerts;
        public static int TransportsLeft;
        public static bool IsRoundOne;

        //plague info.
        public static byte currentInfectingTarget;
        public static Dictionary<(byte, byte), bool> isInfected = new();
        public static Dictionary<byte, (PlayerControl, float)> PlagueBearerTimer = new();
        public static List<int> bombedVents = new();
        public static Dictionary<byte, (byte, bool)> SleuthReported = new();
        public static Dictionary<AmongUsExtensions.OptionType, List<CustomOption>> Options = new();

        //SHOW MODIFIERS
        
        public static bool JackalDied;

        public static Main Instance;
        public static bool CamoComms;

        //coven
        //coven main info
        public static int CovenMeetings;
        public static bool HasNecronomicon;
        public static bool ChoseWitch;
        public static bool WitchProtected;
        //role info
        public static bool HexMasterOn;
        public static bool PotionMasterOn;
        public static bool VampireDitchesOn;
        public static bool MedusaOn;
        public static bool MimicOn;
        public static bool NecromancerOn;
        public static bool ConjurorOn;

        public static bool GazeReady;
        public static bool IsGazing;
        public static bool CanGoInvis;
        public static bool CanGoInviswizard;
        public static bool CanGoInvispumpkin;

        // VETERAN STUFF //
        public static bool VettedThisRound;
        public static bool VetIsAlerted;
        public static bool VetCanAlert;

        public static int GAprotects;

        //TEAM TRACKS
        public static int TeamCovenAlive;
        public static bool TeamPestiAlive;
        public static bool TeamJuggernautAlive;
        public static bool ProtectedThisRound;
        public static bool HasProtected;
        public static int ProtectsSoFar;
        public static bool IsProtected;
        public static bool IsRoundOneGA;
        public static bool MareHasRedName;
        public static bool KamiHasRedName;
        public static bool CanUseShapeshiftAbilites;

        // NEUTRALS //
        public static bool IsRampaged;
        public static bool RampageReady;
        public static bool IsHackMode;
        public static bool PhantomCanBeKilled;
        public static bool PhantomAlert;

        // TRULY RANDOM ROLES TEST //
        public static List<CustomRoles> chosenRoles = new();
        public static List<CustomRoles> chosenImpRoles = new();
        public static List<CustomRoles> chosenDesyncRoles = new();
        public static List<CustomRoles> chosenNK = new(); // ROLE : Value -- IsShapeshifter -- Key
        public static List<CustomRoles> chosenNonNK = new();

        // specific roles //
        public static List<CustomRoles> chosenEngiRoles = new();
        public static List<CustomRoles> chosenScientistRoles = new();
        public static List<CustomRoles> chosenShifterRoles = new();
        public static List<byte> rolesRevealedNextMeeting = new();
        public static List<byte> rolesRevealedNextMeeting1 = new();
        public static Dictionary<byte, bool> CleanerCanClean = new();
        public static List<byte> IsShapeShifted = new();
        public static Dictionary<byte, int> PickpocketKills = new();
        public static Dictionary<byte, int> HustlerKills = new();
        public static Dictionary<byte, int> KillCount = new();
        public static List<byte> KillingSpree = new();

        public static int MarksmanKills = 0;
        public static bool FirstMeetingOccured = false;

        public static Dictionary<byte, int> lastAmountOfTasks = new(); // PLayerID : Value ---- AMOUNT : KEY
        public static Dictionary<byte, (int, string, string, string, string, string)> AllPlayerSkin = new(); //Key : PlayerId, Value : (1: color, 2: hat, 3: skin, 4:visor, 5: pet)
        // SPRIES //
        public static Sprite AlertSprite;
        public static Sprite DouseSprite;
        public static Sprite HackSprite;
        public static Sprite IgniteSprite;
        public static Sprite InfectSprite;
        public static Sprite MimicSprite;
        public static Sprite PoisonSprite;
        public static Sprite ProtectSprite;
        public static Sprite RampageSprite;
        public static Sprite RememberSprite;
        public static Sprite SeerSprite;
        public static Sprite SheriffSprite;
        public static Sprite VestSprite;
        public static Sprite CleanSprite;
        public static Sprite TransportSprite;
        public static Sprite FlashSprite;
        public static Sprite PoisonedSprite;
        public static Sprite MediumSprite;
        public static Sprite BlackmailSprite;
        public static Sprite MinerSprite;
        public static Sprite TargetSprite;
        public static Sprite AssassinateSprite;
        public static int WitchesThisRound = 0;
        public static string LastWinner = "None";

        public static AmongUsExtensions.OptionType currentType;
        // 31628
        public static bool FirstMeetingPassed = false;
        // ATTACK AND DEFENSE STUFF //
        public static Dictionary<CustomRoles, AttackEnum> attackValues;
        public static Dictionary<CustomRoles, DefenseEnum> defenseValues;
        public static List<byte> unvotablePlayers = new();
        //toc adds
        public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
        public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
        public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive());

        // SPECIAL STUFF
        public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 24 or 25;
        public static bool IsInitialRelease = DateTime.Now.Month == 8 && DateTime.Now.Day is 18;
        public static bool NewYears = (DateTime.Now.Month == 12 && DateTime.Now.Day is 31) || (DateTime.Now.Month == 1 && DateTime.Now.Day is 1);
        public override void Load()
        {
            Instance = this;

            TextCursorTimer = 0f;
            TextCursorVisible = true;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "MEMBERSHIP FAST TRACK");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{StreamerColor}");
            ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
            ButtonImages = Config.Bind("Client Options", "Custom Button Images", false);
            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            //TownOfHost.Logger.isDetail = true;

            currentWinner = CustomWinner.Default;
            additionalwinners = new HashSet<AdditionalWinners>();

            AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            AllPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            LastPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            LastPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            CustomWinTrigger = false;
            BitPlayers = new Dictionary<byte, (byte, float)>();
            SurvivorStuff = new Dictionary<byte, (int, bool, bool, bool, bool)>();
            WarlockTimer = new Dictionary<byte, float>();
            CursedPlayers = new Dictionary<byte, PlayerControl>();
            RemoveChat = new Dictionary<byte, bool>();
            SpelledPlayer = new List<PlayerControl>();
            Impostors = new List<PlayerControl>();
            rolesRevealedNextMeeting = new List<byte>();
            rolesRevealedNextMeeting1 = new List<byte>();
            SilencedPlayer = new List<PlayerControl>();
            AliveAtTheEndOfTheRound = new List<byte>();
            FirstMeetingPassed = false;
            LastWinner = "None";
            ColliderPlayers = new List<byte>();
            WitchesThisRound = 0;
            CleanerCanClean = new Dictionary<byte, bool>();
            HasTarget = new Dictionary<byte, bool>();
            isDoused = new Dictionary<(byte, byte), bool>();
            isHexed = new Dictionary<(byte, byte), bool>();
            isInfected = new Dictionary<(byte, byte), bool>();
            VetCanAlert = true;
            currentType = AmongUsExtensions.OptionType.None;
            ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            PlagueBearerTimer = new Dictionary<byte, (PlayerControl, float)>();
            ExecutionerTarget = new Dictionary<byte, byte>();
            GuardianAngelTarget = new Dictionary<byte, byte>();
            LawyerTarget = new Dictionary<byte, byte>();
            MayorUsedButtonCount = new Dictionary<byte, int>();
            HackerFixedSaboCount = new Dictionary<byte, int>();
            LastEnteredVent = new Dictionary<byte, Vent>();
            CurrentEnterdVent = new Dictionary<byte, Vent>();
            knownGhosts = new Dictionary<byte, List<byte>>();
            LastEnteredVentLocation = new Dictionary<byte, Vector2>();
            CurrentTarget = new Dictionary<byte, byte>();
            WitchList = new Dictionary<byte, byte>();
            HasModifier = new Dictionary<byte, CustomRoles>();
            // /DeadPlayersThisRound = new List<byte>();
            LoversPlayers = new List<PlayerControl>();
            cheatersPlayers = new List<PlayerControl>();
            //Lawyer Client
            LawyerClient = new List<PlayerControl>(); //contains Lawyer and Client
            dousedIDs = new List<byte>();
            //firstKill = new Dictionary<byte, (PlayerControl, float)>();
            winnerList = new List<byte>();
            KillingSpree = new List<byte>();
            unvotablePlayers = new();
            VisibleTasksCount = false;
            MercCanSuicide = true;
            satanCanSuicide = true;
            CanUseShapeshiftAbilites = true;
            devIsHost = false;
            ExeCanChangeRoles = true;
            MessagesToSend = new List<(string, byte)>();
            currentDousingTarget = 255;
            currentFreezingTarget = 255;
            currentInfectingTarget = 255;
            JugKillAmounts = 0;
            AteBodies = 0;
            MarksmanKills = 0;
            CovenMeetings = 0;
            GAprotects = 0;
            CanTransport = true;
            PickpocketKills = new Dictionary<byte, int>();
            HustlerKills = new Dictionary<byte, int>();
            KillCount = new Dictionary<byte, int>();
            ProtectedThisRound = false;
            HasProtected = false;
            VetAlerts = 0;
            TransportsLeft = 0;
            ProtectsSoFar = 0;
            IsProtected = false;
            ResetVision = false;
            Grenaiding = false;
            DoingYingYang = true;
            VettedThisRound = false;
            MareHasRedName = false;
            KamiHasRedName = false;
            WitchProtected = false;
            HexMasterOn = false;
            PotionMasterOn = false;
            VampireDitchesOn = false;
            IsShapeShifted = new List<byte>();
            MedusaOn = false;
            MimicOn = false;
            FirstMeetingOccured = false;
            NecromancerOn = false;
            ConjurorOn = false;
            ChoseWitch = false;
            HasNecronomicon = false;
            VetIsAlerted = false;
            IsRoundOne = false;
            IsRoundOneGA = false;
            showEjections = false;

            IsRampaged = false;
            IsInvis = false;
            IsInviswizard = false;
            IsInvispumpkin = false;
            CanGoInvis = false; 
            RampageReady = false;
            CanGoInviswizard = false;
            CanGoInvispumpkin = false;

            IsHackMode = false;
            GazeReady = true;
            IsGazing = false;
            CamoComms = false;
            HexesThisRound = 0;
            JackalDied = false;
            LastVotedPlayer = "";
            bkProtected = false;
            AlertSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Alert.png", 100f);
            DouseSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Douse.png", 100f);
            HackSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Hack.png", 100f);
            IgniteSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Ignite.png", 100f);
            InfectSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Infect.png", 100f);
            MimicSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Mimic.png", 100f);
            PoisonSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Poison.png", 100f);
            ProtectSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Protect.png", 100f);
            RampageSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Rampage.png", 100f);
            RememberSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Remember.png", 100f);
            SeerSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Seer.png", 100f);
            SheriffSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Sheriff.png", 100f);
            VestSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Vest.png", 100f);
            CleanSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Janitor.png", 100f);
            TransportSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Transport.png", 100f);
            FlashSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Flash.png", 100f);
            PoisonedSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Poisoned.png", 100f);
            BlackmailSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Blackmail.png", 100f);
            MediumSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Mediate.png", 100f);
            MinerSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.Mine.png", 100f);
            TargetSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.NinjaMarkButton.png", 100f);
            AssassinateSprite = Helpers.LoadSpriteFromResourcesTOR("TownOfHost.Resources.NinjaAssassinateButton.png", 100f);

            // OTHER//

            TeamJuggernautAlive = false;
            TeamPestiAlive = false;
            TeamCovenAlive = 3;
            PhantomAlert = false;
            PhantomCanBeKilled = false;

            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", true);
            AmDebugger.Value = false;
            CachedDevMode = AmDebugger.Value;
            ShowPopUpVersion = Config.Bind("Other", "ShowPopUpVersion", "0");
            MessageWait = Config.Bind("Other", "MessageWait", 1f);
            LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);

            NameColorManager.Begin();
            SoundEffectsManager.Load();
            Translator.Init();

            hasArgumentException = false;
            AllPlayerSkin = new();
            unreportableBodies = new();
            ExceptionMessage = "";
            try
            {

                roleColors = new Dictionary<CustomRoles, string>()
                {
                    //バニラ役職
                    {CustomRoles.Crewmate, "#ffffff"},
                    {CustomRoles.Engineer, "#b6f0ff"},
                    {CustomRoles.CrewmateGhost, "#ffffff"},
                    { CustomRoles.Scientist, "#b6f0ff"},
                    { CustomRoles.Mechanic, "#FFA60A"},
                    { CustomRoles.Physicist, "#b6f0ff"},
                    { CustomRoles.GuardianAngel, "#ffffff"},
                    { CustomRoles.Target, "#000000"},
                    { CustomRoles.CorruptedSheriff, "#ff0000"},
                    { CustomRoles.Watcher, "#800080"},
                    { CustomRoles.NiceGuesser, "#E4E085"},
                    { CustomRoles.Pirate, "#EDC240"},
                    //特殊クルー役職
                    { CustomRoles.Bait, "#00B3B3"},
                    { CustomRoles.SabotageMaster, "#0000ff"},
                    { CustomRoles.Snitch, "#b8fb4f"},
                   // { CustomRoles.Mayor, "#204d42"},
                    { CustomRoles.Sheriff, "#f8cd46"},
                    { CustomRoles.Investigator, "#ffca81"},
                    { CustomRoles.Lighter, "#eee5be"},
                    //{ CustomRoles.Bodyguard, "#7FFF00"},
                    //{ CustomRoles.Oracle, "#0042FF"},
                    { CustomRoles.Bodyguard, "#5d5d5d"},
                    { CustomRoles.Oracle, "#c88dd0"},
                    { CustomRoles.Forecaster, "#c88dd0"},
                    { CustomRoles.Medic, "#006600"},
                    { CustomRoles.SpeedBooster, "#00ffff"},
                    { CustomRoles.Mystic, "#4D99E6"},
                    { CustomRoles.Paramedic, "#FF6200"},
                    { CustomRoles.Swapper, "#66E666"},
                  //  { CustomRoles.Transporter, "#00EEFF"},{ CustomRoles.Mayor, "#204d42"},{ CustomRoles.Doctor, "#80ffdd"},{ CustomRoles.Trapper, "#5a8fd0"},
                  //  { CustomRoles.Doctor, "#80ffdd"},
                    { CustomRoles.Child, "#FFFFFF"},
                  //  { CustomRoles.Trapper, "#5a8fd0"},
                    { CustomRoles.Dictator, "#df9b00"},
                    { CustomRoles.Sleuth, "#803333"},
                    { CustomRoles.Crusader, "#c65c39"},
                    { CustomRoles.Escort, "#ffb9eb"},
                    { CustomRoles.PlagueBearer, "#E6FFB3"},
                    { CustomRoles.Pestilence, "#393939"},
                    { CustomRoles.Vulture, "#a36727"},
                    { CustomRoles.CSchrodingerCat, "#ffffff"}, //シュレディンガーの猫の派生
                    { CustomRoles.Medium, "#A680FF"},
                    { CustomRoles.Alturist, "#660000"},
                    { CustomRoles.Psychic, "#6F698C"},
                    {CustomRoles.TimeManager, "#C123E1"},
                    //第三陣営役職
                    { CustomRoles.Arsonist, "#ff6633"},
                    { CustomRoles.Jester, "#ec62a5"},
                    { CustomRoles.Terrorist, "#00ff00"},
                    { CustomRoles.Executioner, "#C96600"},
                    { CustomRoles.Opportunist, "#00ff00"},
                    { CustomRoles.Sellout, "#28C2FF"},
                    { CustomRoles.Chancer, "#2AF5FF"},
                    { CustomRoles.Satan, "#CE1A1A"},
                    { CustomRoles.Survivor, "#FFE64D"},
                    { CustomRoles.AgiTater, "#F4A460"},
                    { CustomRoles.PoisonMaster, "#ed2f91"},
                    { CustomRoles.SchrodingerCat, "#696969"},
                    { CustomRoles.Egoist, "#5600ff"},
                    { CustomRoles.EgoSchrodingerCat, "#5600ff"},
                    { CustomRoles.Postman, "#989898"},
                    { CustomRoles.Jackal, "#00b4eb"},
                    { CustomRoles.Sidekick, "#00b4eb"},
                    { CustomRoles.Marksman, "#440101"},
                    { CustomRoles.Juggernaut, "#670038"},
                    { CustomRoles.JSchrodingerCat, "#00b4eb"},
                    { CustomRoles.Phantom, "#662962"},
                    { CustomRoles.NeutWitch, "#592e98"},
                    { CustomRoles.Hitman, "#ce1924"},
                    //HideAndSeek
                    { CustomRoles.HASFox, "#e478ff"},
                    { CustomRoles.BloodKnight, "#630000"},
                    { CustomRoles.HASTroll, "#00ff00"},
                    { CustomRoles.Painter, "#FF5733"},
                    { CustomRoles.Janitor, "#c67051"},
                    { CustomRoles.Supporter, "#00b4eb"},
                    { CustomRoles.Tasker, "#2c68dc"},
                    // GM
                    { CustomRoles.GM, "#ff5b70"},
                    //サブ役職
                    { CustomRoles.NoSubRoleAssigned, "#ffffff"},
                    { CustomRoles.Lovers, "#FF66CC"},
                    { CustomRoles.LoversRecode, "#FF66CC"},
                    { CustomRoles.LoversWin, "#FF66CC"},
                    { CustomRoles.Flash, "#FF8080"},
                    { CustomRoles.Oblivious, "#808080"},
                    { CustomRoles.DoubleShot, "#ff0000"},
                    { CustomRoles.Torch, "#FFFF99"},
                    { CustomRoles.Diseased, "#AAAAAA"},
                    { CustomRoles.TieBreaker, "#99E699"},
                    { CustomRoles.Obvious, "#D3D3D3"},
                    { CustomRoles.Escalation, "#FFB34D"},
                    { CustomRoles.Transporter, "#00EEFF"},
                    { CustomRoles.Mayor, "#204d42"},
                    { CustomRoles.Doctor, "#80ffdd"},
                    { CustomRoles.Trapper, "#5a8fd0"},
                    { CustomRoles.Demolitionist, "#5e2801"},
                    { CustomRoles.Veteran, "#998040"},

                    { CustomRoles.Coven, "#bd5dfd"},
                    //{ CustomRoles.Veteran, "#998040"},
                    { CustomRoles.GuardianAngelTOU, "#B3FFFF"},
                    { CustomRoles.Lawyer, "#9F9314"},
                    { CustomRoles.TheGlitch, "#00FF00"},
                    { CustomRoles.Werewolf, "#A86629"},
                    { CustomRoles.Amnesiac, "#81DDFC"},
                    { CustomRoles.Bewilder, "#292644"},
                   // { CustomRoles.Demolitionist, "#5e2801"},
                    { CustomRoles.Bastion, "#524f4d"},
                    { CustomRoles.Hacker, "#358013"},
                    { CustomRoles.CrewPostor, "#DC6601"},
                    { CustomRoles.Magician, "#66FFC2"},
                    { CustomRoles.Dracula, "#FA6C6C"},
                    { CustomRoles.Hustler, "#93E1D8"},
                    { CustomRoles.Reviver, "#C547FF"},
                    { CustomRoles.Wizard, "#1D90E8"},
                    { CustomRoles.PUMPkinsPotion, "#FF6700"},
                    { CustomRoles.Kamikaze, "#03fc9d"},


                    { CustomRoles.CPSchrodingerCat, "#DC6601"},
                    { CustomRoles.MGSchrodingerCat, "#DC6601"},
                    { CustomRoles.RBSchrodingerCat, "#DC6601"},
                    { CustomRoles.TGSchrodingerCat, "#00FF00"},
                    { CustomRoles.WWSchrodingerCat, "#A86629"},
                    { CustomRoles.JugSchrodingerCat, "#670038"},
                    { CustomRoles.MMSchrodingerCat, "#440101"},
                    { CustomRoles.PesSchrodingerCat, "#393939"},
                    { CustomRoles.BKSchrodingerCat, "#630000"},

                    // TAGS //
                   //TEXT COLORS ROSIE
                    { CustomRoles.sns1, "#FFF9DB"},
                    { CustomRoles.sns2, "#FBE0E2"},
                    { CustomRoles.sns3, "#F6C6E8"},
                    { CustomRoles.sns4, "#F2ADEE"},
                    { CustomRoles.sns5, "#ED93F4"},
                    { CustomRoles.sns6, "#DDA1EE"},
                    { CustomRoles.sns7, "#CCAEE8"},
                    { CustomRoles.sns8, "#AAC9DB"},
                    { CustomRoles.sns9, "#88E4CF"},
                    { CustomRoles.sns10, "#66FFC2"},
                    { CustomRoles.rosecolor, "#FFD6EC"},
                    // MISC //
                    { CustomRoles.eevee, "#FF8D1C"},
                    { CustomRoles.serverbooster, "#f47fff"},
                    { CustomRoles.thetaa, "#9A9AEB"},
                    // SELF//
                    { CustomRoles.minaa, "#C8A2C8"},
                    { CustomRoles.ess, "#EAC4FB"},

                    //TEXT COLORS Candy
                    { CustomRoles.psh1, "#EF807F"},
                    { CustomRoles.psh2, "#F3969C"},
                    { CustomRoles.psh3, "#F7ABB8"},
                    { CustomRoles.psh4, "#FBC1D5"},
                    { CustomRoles.psh5, "#FBC6E9"},
                    { CustomRoles.psh6, "#F6B6E0"},
                    { CustomRoles.psh7, "#F4AEDC"},
                    { CustomRoles.psh8, "#F1A6D7"},
                    { CustomRoles.psh9, "#EC96CE"},
                                          //TEXT COLORS RAINBOW
                    { CustomRoles.rain1, "#F9CFCE"},
                    { CustomRoles.rain2, "#FAA9DA"},
                    { CustomRoles.rain3, "#FB82E5"},
                    { CustomRoles.rain4, "#FC5CF1"},
                    { CustomRoles.rain5, "#FC35FC"},
                    { CustomRoles.rain6, "#FD48D1"},
                    { CustomRoles.rain7, "#FE5AA6"},
                    { CustomRoles.rain8, "#FF6D7B" },
                    { CustomRoles.rain9, "#FF7666"},
                    { CustomRoles.rain10, "#FF7F50" },
                    //TEXT COLORS BEN
                    { CustomRoles.ben0, "#3AFF6F"},
                    { CustomRoles.ben1, "#35FF80"},
                    { CustomRoles.ben2, "#30FF91"},
                    { CustomRoles.ben3, "#2BFFA2"},
                    { CustomRoles.ben4, "#26FFB3"},
                    { CustomRoles.ben5, "#21FFC4"},
                    { CustomRoles.ben6, "#1CFFD5"},
                    { CustomRoles.ben7, "#17FFE6"},
                    { CustomRoles.ben8, "#15FFEF"},
                    { CustomRoles.ben9, "#12FFF7"},
                    //TEST COLORS AU01
                    { CustomRoles.AU1, "#35FF80"},
                    { CustomRoles.AU2, "#30FF91"},
                    { CustomRoles.AU3, "#2BFFA2"},
                    { CustomRoles.AU4, "#26FFB3"},
                    //TEXT COLORS AU02
                    { CustomRoles.AU11, "#35FF80"},
                    { CustomRoles.AU22, "#30FF91"},
                    { CustomRoles.AU33, "#2BFFA2"},
                    { CustomRoles.AU44, "#26FFB3"},
                    // CORLOR DC
                    { CustomRoles.grey1, "#646464" },
                    { CustomRoles.red1, "#FF1500" },
                    { CustomRoles.purp1, "#C30EFF" },
                    { CustomRoles.or1, "#FF9D00" },
                    { CustomRoles.or2, "#FF760D" },
                    { CustomRoles.ro1, "#FF697D" },
                    { CustomRoles.ye1, "#F5DC00" },
                    // COLOR GR1 lime-cyan
                    { CustomRoles.gr1, "#39ff14" },
                    { CustomRoles.gr2, "#2bff4d" },
                    { CustomRoles.gr3, "#1dff86" },
                    { CustomRoles.gr4, "#0fffbf" },
                    { CustomRoles.gr5, "#00fff7" },
                    // CUSTOM ROLES GRAD2 red-grey
                    { CustomRoles.gr01, "#ff1414" },
                    { CustomRoles.gr02, "#e02f2f" },
                    { CustomRoles.gr03, "#c04a4a" },
                    { CustomRoles.gr04, "#a06565" },
                    { CustomRoles.gr05, "#808080" },
                    //custom roles grad 3 purple-red
                    { CustomRoles.gr11, "#b1028c" },
                    { CustomRoles.gr22, "#bd0275" },
                    { CustomRoles.gr33, "#c9015d" },
                    { CustomRoles.gr44, "#d60146" },
                    { CustomRoles.gr55, "#e2012f" },
                    //custom roles grad 4 lime-pink
                     { CustomRoles.g1, "#5cf64a" },
                    { CustomRoles.g2, "#87e752" },
                    { CustomRoles.g3, "43b929" },
                    { CustomRoles.g4, "ffcae9" },
                    { CustomRoles.g5, "ff37a6" },
                    // custm roles grad 5 pink-cyan
                    { CustomRoles.g01, "#ff499e" },
                    { CustomRoles.g02, "#d264b6" },
                    { CustomRoles.g03, "#a480cf" },
                    { CustomRoles.g04, "#779be7" },
                    { CustomRoles.g05, "#49b6ff" },
                    // custom roles gad 6 blue-cyan
                    { CustomRoles.bl1, "#4E5BEE" },
                    { CustomRoles.bl2, "#2A8DE8" },
                    { CustomRoles.bl3, "#18A6E5" },
                    { CustomRoles.bl4, "#06BEE1" },
                    // custom roles luci (lav)
                    { CustomRoles.ar1, "#d099e2" },
                    //custom roles normal color
                    { CustomRoles.no1, "#DA85FF" },
                    { CustomRoles.no2, "#BB5CFF" },
                    { CustomRoles.no3, "#7070FF" },
                    { CustomRoles.no4, "#70FF70" },
                    { CustomRoles.no5, "#FFFF47" },
                    { CustomRoles.no6, "#FFA347" },
                    { CustomRoles.no7, "#FF4747" },
                    // custom color lulu
                    { CustomRoles.al1, "#98FF98" },
                    { CustomRoles.al2, "#9CF09F" },
                    { CustomRoles.al3, "#A0E0A5" },
                    { CustomRoles.al4, "#A8C0B2" },
                    { CustomRoles.al5, "#B0A0BF" },
                    { CustomRoles.al6, "#B780CC" },
                    { CustomRoles.al7, "#BF60D9" },
                    { CustomRoles.al8, "#C640E6" },
                    { CustomRoles.al9, "#CE20F3" },
                    { CustomRoles.al10, "#D500FF" },
                    // custom color bennie
                    { CustomRoles.bb1, "#E0E0E0" },
                    { CustomRoles.bb2, "#D5D8E2" },
                    { CustomRoles.bb3, "#CAD0E4" },
                    { CustomRoles.bb4, "#B4BFE7" },
                    { CustomRoles.bb5, "#9EAEEB" },
                    { CustomRoles.bb6, "#879DEE" },
                    { CustomRoles.bb7, "#718DF2" },
                    { CustomRoles.bb8, "#5B7CF5" },
                    { CustomRoles.bb9, "#456BF9" },
                    { CustomRoles.bb10, "#2E5AFC" },
                    //custom colors howdy
                    { CustomRoles.hw1, "#FF33E4" },
                    { CustomRoles.hw2, "#D238BE" },
                    { CustomRoles.hw3, "#A53C97" },
                    { CustomRoles.hw4, "#784071" },
                    { CustomRoles.hw5, "#4B444A" },
                    { CustomRoles.hw6, "#784071" },
                    { CustomRoles.hw7, "#A53C97" },
                    { CustomRoles.hw8, "#D238BE" },
                    { CustomRoles.hw9, "#FF33E4" },
                    { CustomRoles.hw10, "#FF46E6" },
                    //custom roles pew pew
                    { CustomRoles.pw1, "#7F41F1" },
                    { CustomRoles.pw2, "#8543EF" },
                    { CustomRoles.pw3, "#8B45ED" },
                    { CustomRoles.pw4, "#9749E9" },
                    { CustomRoles.pw5, "#A34DE5" },
                    { CustomRoles.pw6, "#AF51E1" },
                    { CustomRoles.pw7, "#AB49E0" },
                    { CustomRoles.pw8, "#A740DF" },
                    { CustomRoles.pw9, "#A337DE" },
                    { CustomRoles.pw10, "#9F2EDC" },
                    //custom colors ari
                    { CustomRoles.a6, "#BAF2BB" },
                    { CustomRoles.a7, "#BAF2D8" },
                    { CustomRoles.a8, "#BAD7F2" },
                    { CustomRoles.a9, "#F2BAC9" },
                    { CustomRoles.a10, "#F2E2BA" },
                     //custom colors dark
                    { CustomRoles.d1, "#1c150d" },
                    { CustomRoles.d2, "#2e231d" },
                    { CustomRoles.d3, "#7a3a3a" },
                    { CustomRoles.d4, "#aa3c3b" },
                    { CustomRoles.d5, "#cd3b3b" },
                    //custom roles lime-grey
                    { CustomRoles.w1, "#37FF00" },
                    { CustomRoles.w2, "#48E21E" },
                    { CustomRoles.w3, "#59C53B" },
                    { CustomRoles.w4, "#6AA858" },
                    { CustomRoles.w5, "#7B8A75" },
                    // BLACK NOIR
                    { CustomRoles.w6, "#627F7F" },
                    { CustomRoles.w7, "#5C9F88" },
                    { CustomRoles.w8, "#55BF90" },
                    { CustomRoles.w9, "#4EDF98" },
                    { CustomRoles.w10, "#47FFA0" },
                    // fifu
                    { CustomRoles.f1, "#E80073" },
                    { CustomRoles.f2, "#FF007F" },
                    { CustomRoles.f3, "#D3126C" },
                    { CustomRoles.f4, "#A72459" },
                    //dan
                    { CustomRoles.da1, "#208D34" },
                    { CustomRoles.da2, "#238145" },
                    { CustomRoles.da3, "#267657" },
                    { CustomRoles.da4, "#296B68" },
                    { CustomRoles.da5, "#2E548A" },
                     //luci red-or
                    { CustomRoles.l1, "#FF0000" },
                    { CustomRoles.l2, "#FF2500" },
                    { CustomRoles.l3, "#FF4900" },
                    { CustomRoles.l4, "#FF5B00" },
                    { CustomRoles.l5, "#FF6D00" },
                     //heda black to red
                    { CustomRoles.h1, "#E6E8E6" },
                    { CustomRoles.h2, "#ECC0BD" },
                    { CustomRoles.h3, "#F19793" },
                    { CustomRoles.h4, "#F76E69" },
                    { CustomRoles.h5, "#FC453F" },
                    { CustomRoles.h6, "#C63B38" },
                    { CustomRoles.h7, "#8F3130" },
                    { CustomRoles.h8, "#582729" },
                    { CustomRoles.h9, "#3D2225" },
                    { CustomRoles.h10, "#211C21" },
                    //diamond
                    { CustomRoles.di1, "#f1f7fb" },
                    { CustomRoles.di2, "#d9ebf4" },
                    { CustomRoles.di3, "#cbe3f0" },
                    { CustomRoles.di4, "#b8d8e7" },
                    { CustomRoles.di5, "#9ac5db" },
                    //GOLD
                    { CustomRoles.gd1, "#FAFF5C" },
                    { CustomRoles.gd2, "#DDFF5D" },
                    { CustomRoles.gd3, "#C0FE5D" },
                    { CustomRoles.gd4, "#A3FE5D" },
                    { CustomRoles.gd5, "#85FD5D" },
                                        //LAYLA
                    { CustomRoles.la1, "#7FD8BE" },
                    { CustomRoles.la2, "#A1FCDF" },
                    { CustomRoles.la3, "#FCD29F" },
                    { CustomRoles.la4, "#FCBF82" },
                    { CustomRoles.la5, "#FCAB64" },
                                        //MIKA
                                        //KNIGHT
                    { CustomRoles.k1, "#A6FC4A" },
                    { CustomRoles.k2, "#BDD44F" },
                    { CustomRoles.k3, "#D3AC53" },
                    { CustomRoles.k4, "#E98458" },
                    { CustomRoles.k5, "#FF5C5C" },
                                        //namra
                    { CustomRoles.na1, "#07FFC4" },
                    { CustomRoles.na2, "#0DFFB7" },
                    { CustomRoles.na3, "#1AFF9D" },
                    { CustomRoles.na4, "#27FF84" },
                    { CustomRoles.na5, "#34FF6A" },
                    { CustomRoles.na6, "#41FF51" },
                    { CustomRoles.na7, "#4EFF37" },
                    { CustomRoles.na8, "#5BFF1D" },
                    { CustomRoles.na9, "#68FF03" },
                    
                     //mine 2
                    { CustomRoles.m1, "#99FFC2" },
                    { CustomRoles.m2, "#B3E1CC" },
                    { CustomRoles.m3, "#CCC3D5" },
                    { CustomRoles.m4, "#E5A5DF" },
                    { CustomRoles.m5, "#FE86E8" },
                        //MAX 🍍
                    { CustomRoles.mx1, "#A6D7CF" },
                    { CustomRoles.mx2, "#AAD6CE" },
                    { CustomRoles.mx3, "#AED4CC" },
                    { CustomRoles.mx4, "#B6D0C8" },
                    { CustomRoles.mx5, "#C6C9C0" },
                    { CustomRoles.mx6, "#D6C2B9" },
                    { CustomRoles.mx7, "#E5BBB1" },
                    { CustomRoles.mx8, "#E5A8A3" },
                    { CustomRoles.mx9, "#E49595" },
                    { CustomRoles.mx10, "#E38287" },
                    //MissKitten
                    { CustomRoles.ms1, "#E1983A" },
                    { CustomRoles.ms2, "#DC8957" },
                    { CustomRoles.ms3, "#D67A74" },
                    { CustomRoles.ms4, "#D16B91" },
                    { CustomRoles.ms5, "#CB5BAD" },
                    //Rocky
                    { CustomRoles.r1, "#FF1FB0" },
                    { CustomRoles.r2, "#D329C4" },
                    { CustomRoles.r3, "#A733D8" },
                    { CustomRoles.r4, "#7B3DEC" },
                    { CustomRoles.r5, "#4E47FF" },
                    //LADYTATER
                    { CustomRoles.lt1, "#FF47ED" },
                    { CustomRoles.lt2, "#C064E0" },
                    { CustomRoles.lt3, "#8080D3" },
                    { CustomRoles.lt4, "#409CC6" },
                    { CustomRoles.lt5, "#00B8B8" },
                     //gunbaby
                    { CustomRoles.gb1, "#5CFFF1" },
                    { CustomRoles.gb2, "#6DE8F3" },
                    { CustomRoles.gb3, "#7DD1F5" },
                    { CustomRoles.gb4, "#BF75FC" },
                    { CustomRoles.gb5, "#E047FF" },
                               //my 10 color mic
                   /* { CustomRoles.q1, "#8AE9C1" },
                    { CustomRoles.q2, "#A0DBC6" },
                    { CustomRoles.q3, "#B5CDCB" },
                    { CustomRoles.q4, "#CBBFD0" },
                    { CustomRoles.q5, "#E0B0D5" },
                    { CustomRoles.q6, "#CFC3C6" },
                    { CustomRoles.q7, "#BED6B6" },
                    { CustomRoles.q8, "#ADE9A7" },
                    { CustomRoles.q9, "#A5F39F" },
                    { CustomRoles.q10, "#9CFC97" },
                    { CustomRoles.q1, "#ABFFCB" },
                    { CustomRoles.q2, "#93FFBC" },
                    { CustomRoles.q3, "#7CFFAE" },
                    { CustomRoles.q4, "#64FF9F" },
                    { CustomRoles.q5, "#5DFCB7" },
                    { CustomRoles.q6, "#56F8CF" },
                    { CustomRoles.q7, "#47F0FF" },
                    { CustomRoles.q8, "#6BF3FF" },
                    { CustomRoles.q9, "#F3E6BB" },
                    { CustomRoles.q10, "#A1F8FF" }, */
                    { CustomRoles.q1, "#68E3F9" },
                    { CustomRoles.q2, "#8CC1E2" },
                    { CustomRoles.q3, "#AF9FCA" },
                    { CustomRoles.q4, "#D27DB3" },
                    { CustomRoles.q5, "#F55A9B" },
                    { CustomRoles.q6, "#CC57AA" },
                    { CustomRoles.q7, "#A254B9" },
                    { CustomRoles.q8, "#7951C8" },
                    { CustomRoles.q9, "#4F4ED7" },
                    //banana
                    { CustomRoles.banana, "#F3E6BB" },
                     
                    //lemon
                    { CustomRoles.le1, "#33FFB1" },
                    { CustomRoles.le2, "#66F792" },
                    { CustomRoles.le3, "#99EE72" },
                    { CustomRoles.le4, "#CCE653" },
                    { CustomRoles.le5, "#FFDD33" },
                    { CustomRoles.le6, "#CCE653" },
                    { CustomRoles.le7, "#99EE72" },
                    { CustomRoles.le8, "#66F792" },
                    { CustomRoles.le9, "#33FFB1" },
                    { CustomRoles.le10, "#46FFB8" },
                    //Priya
                    { CustomRoles.py1, "#FC0202" },
                    { CustomRoles.py2, "#FD1302" },
                    { CustomRoles.py3, "#FD2402" },
                    { CustomRoles.py4, "#FE2D02" },
                    { CustomRoles.py5, "#FE3502" },
                    { CustomRoles.py6, "#FE4502" },
                    { CustomRoles.py7, "#FF5602" },
                    { CustomRoles.py8, "#FF6702" },
                    { CustomRoles.py9, "#FF7802" },
                    { CustomRoles.py10, "#FF8801" },
                    //Thetaa
                    { CustomRoles.ta1, "#DB8DFF" },
                    { CustomRoles.ta2, "#CB91FA" },
                    { CustomRoles.ta3, "#BB94F5" },
                    { CustomRoles.ta4, "#AB97F0" },
                    { CustomRoles.ta5, "#9A9AEB" },
                    { CustomRoles.ta6, "#AA89F0" },
                    { CustomRoles.ta7, "#BA78F4" },
                    { CustomRoles.ta8, "#C270F7" },
                    { CustomRoles.ta9, "#CA67F9" },
                    { CustomRoles.ta10, "#D955FD" },
                    //Non
                    { CustomRoles.nn1, "#EB8424" },
                    { CustomRoles.nn2, "#D88735" },
                    { CustomRoles.nn3, "#C58945" },
                    { CustomRoles.nn4, "#9F8D65" },
                    { CustomRoles.nn5, "#799285" },
                    { CustomRoles.nn6, "#5296A5" },
                    { CustomRoles.nn7, "#48808C" },
                    { CustomRoles.nn8, "#3E6972" },
                    { CustomRoles.nn9, "#345258" },
                    { CustomRoles.nn10, "#2A3B3E" },
                    //winners from here
                    //eevee
                    { CustomRoles.ee1, "#FF844B" },
                    { CustomRoles.ee2, "#FF7F41" },
                    { CustomRoles.ee3, "#FE7937" },
                    { CustomRoles.ee4, "#FD742D" },
                    { CustomRoles.ee5, "#DC6626" },
                    { CustomRoles.ee6, "#BB5D29" },
                    { CustomRoles.ee7, "#AB592B" },
                    { CustomRoles.ee8, "#9B552C" },
                    { CustomRoles.ee9, "#7A4C2F" },
                    { CustomRoles.ee10, "#FF7F50" },
                    //cinna
                    { CustomRoles.ci1, "#D1DCFF" },
                    { CustomRoles.ci2, "#D7DBFB" },
                    { CustomRoles.ci3, "#DDDAF7" },
                    { CustomRoles.ci4, "#E8D7EE" },
                    { CustomRoles.ci5, "#F4D4E5" },
                    { CustomRoles.ci6, "#FFD1DC" },
                    { CustomRoles.ci7, "#F9CDCC" },
                    { CustomRoles.ci8, "#F3C8BB" },
                    { CustomRoles.ci9, "#EDC3AA" },
                    { CustomRoles.ci10, "#E6BE99" },
                    //smokie
                    { CustomRoles.sm1, "#D1DCFF" },
                    { CustomRoles.sm2, "#C8C5FF" },
                    { CustomRoles.sm3, "#BEAEFE" },
                    { CustomRoles.sm4, "#B597FD" },
                    { CustomRoles.sm5, "#AB80FC" },
                    { CustomRoles.sm6, "#9E78E5" },
                    { CustomRoles.sm7, "#9070CE" },
                    { CustomRoles.sm8, "#8368B7" },
                    { CustomRoles.sm9, "#7C64AB" },
                    { CustomRoles.sm10, "#75609F" },
                    //MAMA BB
                    { CustomRoles.mm1, "#FD3535" },
                    { CustomRoles.mm2, "#FE4133" },
                    { CustomRoles.mm3, "#FE4D30" },
                    { CustomRoles.mm4, "#FE642A" },
                    { CustomRoles.mm5, "#FF7C25" },
                    { CustomRoles.mm6, "#FF931F" },
                    { CustomRoles.mm7, "#FFA624" },
                    { CustomRoles.mm8, "#FFB829" },
                    { CustomRoles.mm9, "#FFCB2E" },
                    { CustomRoles.mm10, "#FFDD33" },
                    //2 thic
                    { CustomRoles.th1, "#F8BBD0" },
                    { CustomRoles.th2, "#EB93B3" },
                    { CustomRoles.th3, "#DD6A96" },
                    { CustomRoles.th4, "#D75688" },
                    { CustomRoles.th5, "#D04179" },
                    { CustomRoles.th6, "#C2185B" },
                    { CustomRoles.th7, "#B41658" },
                    { CustomRoles.th8, "#A51355" },
                    { CustomRoles.th9, "#971152" },
                    { CustomRoles.th10, "#880E4F" },
                     //meh
                    { CustomRoles.mh1, "#453939" },
                    { CustomRoles.mh2, "#433C52" },
                    { CustomRoles.mh3, "#403E6B" },
                    { CustomRoles.mh4, "#3D4084" },
                    { CustomRoles.mh5, "#3A429C" },
                    { CustomRoles.mh6, "#6C6983" },
                    { CustomRoles.mh7, "#9D9069" },
                    { CustomRoles.mh8, "#CEB750" },
                    { CustomRoles.mh9, "#E7CA43" },
                    { CustomRoles.mh10, "#FFDD36" },
                     //Det
                    { CustomRoles.dt1, "#99C2FF" },
                    { CustomRoles.dt2, "#85B6FF" },
                    { CustomRoles.dt3, "#71AAFF" },
                    { CustomRoles.dt4, "#67A4FF" },
                    { CustomRoles.dt5, "#5D9EFF" },
                    { CustomRoles.dt6, "#4892FF" },
                    { CustomRoles.dt7, "#3686FD" },
                    { CustomRoles.dt8, "#247AFA" },
                    { CustomRoles.dt9, "#126EF8" },
                    { CustomRoles.dt10, "#0062F5" },
                    //lina
                    { CustomRoles.Li1, "#FF499E" },
                    { CustomRoles.Li2, "#E957AB" },
                    { CustomRoles.Li3, "#D265B7" },
                    { CustomRoles.Li4, "#BB73C3" },
                    { CustomRoles.Li5, "#A480CF" },
                    { CustomRoles.Li6, "#9987D5" },
                    { CustomRoles.Li7, "#8E8EDB" },
                    { CustomRoles.Li8, "#779BE7" },
                    { CustomRoles.Li9, "#60A9F3" },
                    { CustomRoles.Li10, "#49B6FF" },
                    //Cat
                    { CustomRoles.ca1, "#F5A032" },
                    { CustomRoles.ca2, "#F5952F" },
                    { CustomRoles.ca3, "#F58A2C" },
                    { CustomRoles.ca4, "#F57F29" },
                    { CustomRoles.ca5, "#F57325" },
                    { CustomRoles.ca6, "#F56822" },
                    { CustomRoles.ca7, "#F55C1F" },
                    { CustomRoles.ca8, "#F54518" },
                    { CustomRoles.ca9, "#F53922" },
                    { CustomRoles.ca10, "#F52C2C" },
                    //Ary
                    { CustomRoles.ay1, "#25F5C4" },
                    { CustomRoles.ay2, "#30E7C5" },
                    { CustomRoles.ay3, "#3BD9C6" },
                    { CustomRoles.ay4, "#51BDC7" },
                    { CustomRoles.ay5, "#67A1C9" },
                    { CustomRoles.ay6, "#7C84CA" },
                    { CustomRoles.ay7, "#9268CC" },
                    { CustomRoles.ay8, "#A84CCD" },
                    { CustomRoles.ay9, "#BE30CF" },
                    { CustomRoles.ay10, "#D313D0" },
                     //citrion
                    { CustomRoles.ct1, "#00FF00" },
                    { CustomRoles.ct2, "#79F505" },
                    { CustomRoles.ct3, "#B5F007" },
                    { CustomRoles.ct4, "#D3ED08" },
                    { CustomRoles.ct5, "#F1EA09" },
                    { CustomRoles.ct6, "#F2D30B" },
                    { CustomRoles.ct7, "#F2BC0D" },
                    { CustomRoles.ct8, "#F28D10" },
                    { CustomRoles.ct9, "#F35F14" },
                    { CustomRoles.ct10, "#F33017" },
                    //sleepy
                    { CustomRoles.sl1, "#F859D8" },             
                    { CustomRoles.sl2, "#F53DE6" },
                    { CustomRoles.sl3, "#F73ACE" },
                    { CustomRoles.sl4, "#F836B5" },
                    { CustomRoles.sl5, "#FA2E83" },
                    { CustomRoles.sl6, "#FC2B6A" },
                    { CustomRoles.sl7, "#FD2751" },
                    { CustomRoles.sl8, "#FE2338" },
                    { CustomRoles.sl9, "#FF212C" },
                    { CustomRoles.sl10, "#FF1F1F" },
                     //whited
                    { CustomRoles.dv1, "#C3FFAD" },
                    { CustomRoles.dv2, "#B8FF9F" },
                    { CustomRoles.dv3, "#ACFF91" },
                    { CustomRoles.dv4, "#A1FF83" },
                    { CustomRoles.dv5, "#95FF75" },
                    { CustomRoles.dv6, "#8AFF67" },
                    { CustomRoles.dv7, "#7EFF59" },
                    { CustomRoles.dv8, "#72FF4B" },
                    { CustomRoles.dv9, "#6CFF44" },
                    { CustomRoles.dv10, "#66FF3C" },
                    //August
                    { CustomRoles.ag1, "#A733FF" },
                    { CustomRoles.ag2, "#B233F7" },
                    { CustomRoles.ag3, "#BD33EE" },
                    { CustomRoles.ag4, "#C833E5" },
                    { CustomRoles.ag5, "#D333DC" },
                    { CustomRoles.ag6, "#DE33D3" },
                    { CustomRoles.ag7, "#E933CA" },
                    { CustomRoles.ag8, "#F433C1" },
                    { CustomRoles.ag9, "#FA33BD" },
                    { CustomRoles.ag10, "#FF33B8" },
                    //Gunbaby
                    { CustomRoles.au1, "#E477DB" },
                    { CustomRoles.au2, "#DA74D6" },
                    { CustomRoles.au3, "#CF71D0" },
                    { CustomRoles.au4, "#BA6AC4" },
                    { CustomRoles.au5, "#B05DB4" },
                    { CustomRoles.au6, "#A64FA4" },
                    { CustomRoles.au7, "#B148A0" },
                    { CustomRoles.au8, "#BC419C" },
                    { CustomRoles.au9, "#D23394" },
                    { CustomRoles.au10, "#E7248C" },
                                        //Winners points
                    { CustomRoles.sg1, "#FF9999" },
                    { CustomRoles.sg2, "#FF8A8A" },
                    { CustomRoles.sg3, "#FF7B7B" },
                    { CustomRoles.sg4, "#FF6C6C" },
                    { CustomRoles.sg5, "#FF5D5D" },
                    { CustomRoles.sg6, "#FF4E4E" },
                    { CustomRoles.sg7, "#FF3F3F" },
                    { CustomRoles.sg8, "#FF3838" },
                    { CustomRoles.sg9, "#FF3030" },
                    { CustomRoles.sg10, "#FF2121" },
                    //FFA WINNER
                    { CustomRoles.fw1, "#34F6F2" },
                    { CustomRoles.fw2, "#4EF4DB" },
                    { CustomRoles.fw3, "#67F2C3" },
                    { CustomRoles.fw4, "#81F0AC" },
                    { CustomRoles.fw5, "#9AED94" },
                    { CustomRoles.fw6, "#B4EB7D" },
                    { CustomRoles.fw7, "#CDE965" },
                    { CustomRoles.fw8, "#E6E74D" },
                    { CustomRoles.fw9, "#F3E641" },
                    { CustomRoles.fw10, "#FFE435" },
                    //toc boosters
                    { CustomRoles.tb1, "#696EFF" },
                    { CustomRoles.tb2, "#8D7EFF" },
                    { CustomRoles.tb3, "#B18DFF" },
                    { CustomRoles.tb4, "#D59DFF" },
                    { CustomRoles.tb5, "#F8ACFF" },
                    { CustomRoles.tb6, "#EDC1D2" },
                    { CustomRoles.tb7, "#E8CCBB" },
                    { CustomRoles.tb8, "#E2D6A4" },
                    { CustomRoles.tb9, "#D7EB77" },
                    { CustomRoles.tb10, "#CBFF49" },
                    { CustomRoles.tb11, "#D2F151" },
                    { CustomRoles.tb12, "#DCDB5D" },
                    { CustomRoles.tb13, "#E5C469" },
                    { CustomRoles.tb14, "#F2A779" },
                    { CustomRoles.tb15, "#FF8989" },
                    { CustomRoles.tb16, "#FF9684" },
                    { CustomRoles.tb17, "#FFA37E" },
                    { CustomRoles.tb18, "#FEBC72" },
                    { CustomRoles.tb19, "#FDD567" },
                    { CustomRoles.tb20, "#FCEE5B" },
                    //Mine new 
                    { CustomRoles.bn1, "#60EFFF" },
                    { CustomRoles.bn2, "#82BBFF" },
                    { CustomRoles.bn3, "#A486FF" },
                    { CustomRoles.bn4, "#C651FF" },
                    { CustomRoles.bn5, "#E81CFF" },
                    { CustomRoles.bn6, "#CB8C95" },
                    { CustomRoles.bn7, "#C4A87B" },
                    { CustomRoles.bn8, "#BDC460" },
                    { CustomRoles.bn9, "#B6E045" },
                    { CustomRoles.bn10, "#AEFB2A" },
                    { CustomRoles.bn11, "#BCBF22" },
                    { CustomRoles.bn12, "#C3A11E" },
                    { CustomRoles.bn13, "#CA831A" },
                    { CustomRoles.bn14, "#D84712" },
                    { CustomRoles.bn15, "#DF290E" },
                    { CustomRoles.bn16, "#C4412C" },
                    { CustomRoles.bn17, "#708686" },
                    { CustomRoles.bn18, "#38B5C2" },
                    { CustomRoles.bn19, "#00E3FD" },
                    //toc boosters
                    { CustomRoles.aw1, "#C5F9D7" },
                    { CustomRoles.aw2, "#DEE7AF" },
                    { CustomRoles.aw3, "#F7D486" },
                    { CustomRoles.aw4, "#F5A782" },
                    { CustomRoles.aw5, "#F27A7D" },
                    { CustomRoles.aw6, "#DF898A" },
                    { CustomRoles.aw7, "#CC9796" },
                    { CustomRoles.aw8, "#A5B3AE" },
                    { CustomRoles.aw9, "#7ECFC6" },
                    { CustomRoles.aw10, "#57EBDE" },
                    { CustomRoles.aw11, "#6DEFB1" },
                    { CustomRoles.aw12, "#78F19B" },
                    { CustomRoles.aw13, "#83F384" },
                    { CustomRoles.aw14, "#99F757" },
                    { CustomRoles.aw15, "#AEFB2A" },
                    //yeetus
                    { CustomRoles.yt1, "#20CEDE" },
                    { CustomRoles.yt2, "#40C3CF" },
                    { CustomRoles.yt3, "#60B8C1" },
                    { CustomRoles.yt4, "#80ADB2" },
                    { CustomRoles.yt5, "#A0A2A4" },
                    { CustomRoles.yt6, "#C09795" },
                    { CustomRoles.yt7, "#D0928E" },
                    { CustomRoles.yt8, "#E08C87" },
                    { CustomRoles.yt9, "#FF8178" },
                    { CustomRoles.yt10, "#E77889" },
                    { CustomRoles.yt11, "#CE6E9A" },
                    { CustomRoles.yt12, "#9D5BBC" },
                    { CustomRoles.yt13, "#8552CD" },
                    { CustomRoles.yt14, "#6C48DE" },
                    { CustomRoles.yt15, "#533EEF" },
                    //invite winner
                    { CustomRoles.iw1, "#EF9306" },
                    { CustomRoles.iw2, "#F28508" },
                    { CustomRoles.iw3, "#F47709" },
                    { CustomRoles.iw4, "#F36B09" },
                    { CustomRoles.iw5, "#F25E08" },
                    { CustomRoles.iw6, "#F35409" },
                    { CustomRoles.iw7, "#F34A09" },
                    { CustomRoles.iw8, "#F43609" },
                    { CustomRoles.iw9, "#F43518" },
                    { CustomRoles.iw10, "#F43427" },
                    { CustomRoles.iw11, "#F53D31" },
                    //Moon Gold
                    //toc boosters
                    { CustomRoles.ml1, "#B800B8" },
                    { CustomRoles.ml2, "#DB0BC6" },
                    { CustomRoles.ml3, "#FD16D3" },
                    { CustomRoles.ml4, "#F40B74" },
                    { CustomRoles.ml5, "#F00644" },
                    { CustomRoles.ml6, "#EB0014" },
                    { CustomRoles.ml7, "#F0404F" },
                    { CustomRoles.ml8, "#F5808A" },
                    { CustomRoles.ml9, "#FAC0C5" },
                    { CustomRoles.ml10, "#FADE97" },
                    { CustomRoles.ml11, "#FAFC69" },
                    { CustomRoles.ml12, "#F8FB37" },
                    { CustomRoles.ml13, "#92B8CB" },
                    { CustomRoles.ml14, "#6FA1FC" },
                    //timmay own
                    //Mine new 
                    { CustomRoles.ti1, "#3BCFD4" },
                    { CustomRoles.ti2, "#F20094" },
                    { CustomRoles.ti3, "#ed2e72" },
                    { CustomRoles.ti4, "#e86549" },
                    { CustomRoles.ti5, "#e5892e" },
                    { CustomRoles.ti6, "#a8a26a" },
                    { CustomRoles.ti7, "#75b79c" },
                    { CustomRoles.ti8, "#3bcfd4" },
                    { CustomRoles.ti9, "#F20094" },
                    { CustomRoles.ti10, "#F20094" },
                    { CustomRoles.ti11, "#3bcfd4" },
                    { CustomRoles.ti12, "#96b373" },
                    { CustomRoles.ti13, "#cfa135" },
                    { CustomRoles.ti14, "#fa7027" },
                    { CustomRoles.ti15, "#f74b4b" },
                    { CustomRoles.ti16, "#f20094" },
                    { CustomRoles.ti17, "#3BCFD4" },

                };
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    switch (role.GetRoleType())
                    {
                        case RoleType.Impostor:
                            roleColors.TryAdd(role, "#ff0000");
                            break;
                        case RoleType.Madmate:
                            roleColors.TryAdd(role, "#ff0000");
                            break;
                        case RoleType.Coven:
                            roleColors.TryAdd(role, "#bd5dfd");
                            break;
                        default:
                            break;
                    }
                }
                attackValues = new Dictionary<CustomRoles, AttackEnum>()
                {
                    {CustomRoles.Crewmate, AttackEnum.None},
                    {CustomRoles.Engineer, AttackEnum.None},
                    { CustomRoles.Scientist, AttackEnum.None},
                    { CustomRoles.Mechanic, AttackEnum.None},
                    { CustomRoles.Physicist, AttackEnum.None},
                    { CustomRoles.GuardianAngel, AttackEnum.None},
                    { CustomRoles.Target, AttackEnum.None},
                    { CustomRoles.Watcher, AttackEnum.None},
                    { CustomRoles.NiceGuesser, AttackEnum.Powerful},
                    { CustomRoles.Pirate, AttackEnum.Powerful},
                    { CustomRoles.Bait, AttackEnum.None},
                    { CustomRoles.SabotageMaster, AttackEnum.None},
                    { CustomRoles.Snitch, AttackEnum.None},
                    { CustomRoles.TimeManager, AttackEnum.None},
                    { CustomRoles.Mayor, AttackEnum.None},
                    { CustomRoles.Sheriff, AttackEnum.Basic},
                    { CustomRoles.Investigator, AttackEnum.None},
                    { CustomRoles.Lighter, AttackEnum.None},
                    { CustomRoles.Bodyguard, AttackEnum.Powerful},
                    { CustomRoles.Oracle, AttackEnum.None},
                    { CustomRoles.Medic, AttackEnum.None},
                    { CustomRoles.SpeedBooster, AttackEnum.None},
                    { CustomRoles.Mystic, AttackEnum.None},
                    { CustomRoles.Swapper, AttackEnum.None},
                    { CustomRoles.Transporter, AttackEnum.None},
                    { CustomRoles.Doctor, AttackEnum.None},
                    { CustomRoles.Child, AttackEnum.Unblockable},
                    { CustomRoles.Trapper, AttackEnum.None},
                    { CustomRoles.Dictator, AttackEnum.Unblockable},
                    { CustomRoles.Sleuth ,AttackEnum.None},
                    { CustomRoles.Crusader, AttackEnum.Powerful},
                    { CustomRoles.Escort, AttackEnum.None},
                    { CustomRoles.PlagueBearer, AttackEnum.None},
                    { CustomRoles.Pestilence, AttackEnum.Powerful},
                    { CustomRoles.Vulture, AttackEnum.None},
                    { CustomRoles.CSchrodingerCat, AttackEnum.None},
                    { CustomRoles.Medium, AttackEnum.None},
                    { CustomRoles.Alturist, AttackEnum.None},
                    { CustomRoles.Psychic, AttackEnum.None},
                    { CustomRoles.Arsonist, AttackEnum.Powerful},
                    { CustomRoles.Jester, AttackEnum.None},
                    { CustomRoles.Terrorist, AttackEnum.Unblockable},
                    { CustomRoles.Executioner, AttackEnum.None},
                    { CustomRoles.Opportunist, AttackEnum.None},
                    { CustomRoles.Survivor, AttackEnum.None},
                    { CustomRoles.AgiTater, AttackEnum.Powerful},
                    { CustomRoles.PoisonMaster, AttackEnum.Basic},
                    { CustomRoles.SchrodingerCat, AttackEnum.None},
                    { CustomRoles.Egoist, AttackEnum.Basic},
                    { CustomRoles.EgoSchrodingerCat, AttackEnum.None},
                    { CustomRoles.Jackal, AttackEnum.Basic},
                    { CustomRoles.Sidekick, AttackEnum.Basic},
                    { CustomRoles.Marksman, AttackEnum.Basic},
                    { CustomRoles.Juggernaut, AttackEnum.Powerful},
                    { CustomRoles.JSchrodingerCat, AttackEnum.None},
                    { CustomRoles.Phantom, AttackEnum.None},
                    { CustomRoles.NeutWitch, AttackEnum.None},
                    { CustomRoles.Hitman, AttackEnum.Basic},
                    { CustomRoles.BloodKnight, AttackEnum.Powerful},
                    { CustomRoles.Veteran, AttackEnum.Powerful},
                    { CustomRoles.GuardianAngelTOU, AttackEnum.None},
                    { CustomRoles.TheGlitch, AttackEnum.Basic},
                    { CustomRoles.Werewolf, AttackEnum.Powerful},
                    { CustomRoles.Amnesiac, AttackEnum.None},
                    { CustomRoles.Reviver, AttackEnum.None},
                    { CustomRoles.Demolitionist, AttackEnum.Unstoppable},
                    { CustomRoles.Bastion, AttackEnum.Unstoppable},
                    { CustomRoles.Hacker, AttackEnum.None},
                    { CustomRoles.CrewPostor, AttackEnum.Basic},
                    { CustomRoles.Magician, AttackEnum.Powerful},
                    { CustomRoles.Chancer, AttackEnum.None},
                    { CustomRoles.Sellout, AttackEnum.None},
                    { CustomRoles.Satan, AttackEnum.Powerful},
                    { CustomRoles.CPSchrodingerCat, AttackEnum.None},
                    { CustomRoles.MGSchrodingerCat, AttackEnum.None},
                    { CustomRoles.RBSchrodingerCat, AttackEnum.None},
                    { CustomRoles.TGSchrodingerCat, AttackEnum.None},
                    { CustomRoles.WWSchrodingerCat, AttackEnum.None},
                    { CustomRoles.JugSchrodingerCat,AttackEnum.None},
                    { CustomRoles.MMSchrodingerCat, AttackEnum.None},
                    { CustomRoles.PesSchrodingerCat, AttackEnum.None},
                    { CustomRoles.BKSchrodingerCat, AttackEnum.None},
                };
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    switch (role.GetRoleType())
                    {
                        case RoleType.Impostor:
                            attackValues.Add(role, AttackEnum.Basic);
                            break;
                        case RoleType.Madmate:
                            if (role == CustomRoles.Parasite)
                                attackValues.Add(role, AttackEnum.Basic);
                            else
                                attackValues.Add(role, AttackEnum.None);
                            break;
                        case RoleType.Coven:
                            attackValues.Add(role, AttackEnum.Basic);
                            break;
                        default:
                            if (!attackValues.ContainsKey(role))
                                attackValues.Add(role, AttackEnum.None);
                            break;
                    }
                }
                defenseValues = new Dictionary<CustomRoles, DefenseEnum>()
                {
                    {CustomRoles.Crewmate, DefenseEnum.None},
                    {CustomRoles.Engineer, DefenseEnum.None},
                    { CustomRoles.Scientist, DefenseEnum.None},
                    { CustomRoles.Mechanic, DefenseEnum.None},
                    { CustomRoles.Physicist, DefenseEnum.None},
                    { CustomRoles.GuardianAngel, DefenseEnum.None},
                    { CustomRoles.Target, DefenseEnum.None},
                    { CustomRoles.Watcher, DefenseEnum.None},
                    { CustomRoles.NiceGuesser, DefenseEnum.None},
                    { CustomRoles.Pirate, DefenseEnum.Basic},
                    { CustomRoles.Bait, DefenseEnum.None},
                    { CustomRoles.SabotageMaster, DefenseEnum.None},
                    { CustomRoles.Snitch, DefenseEnum.None},
                    { CustomRoles.TimeManager, DefenseEnum.None},
                    { CustomRoles.Mayor, DefenseEnum.None},
                    { CustomRoles.Sheriff, DefenseEnum.None},
                    { CustomRoles.Investigator, DefenseEnum.None},
                    { CustomRoles.Lighter, DefenseEnum.None},
                    { CustomRoles.Bodyguard, DefenseEnum.Basic},
                    { CustomRoles.Oracle, DefenseEnum.None},
                    { CustomRoles.Medic, DefenseEnum.None},
                    { CustomRoles.SpeedBooster, DefenseEnum.None},
                    { CustomRoles.Mystic, DefenseEnum.None},
                    { CustomRoles.Swapper, DefenseEnum.None},
                    { CustomRoles.Transporter, DefenseEnum.None},
                    { CustomRoles.Doctor, DefenseEnum.None},
                    { CustomRoles.Child, DefenseEnum.None},
                    { CustomRoles.Trapper, DefenseEnum.None},
                    { CustomRoles.Dictator, DefenseEnum.None},
                    { CustomRoles.Sleuth ,DefenseEnum.None},
                    { CustomRoles.Crusader, DefenseEnum.None},
                    { CustomRoles.Escort, DefenseEnum.None},
                    { CustomRoles.PlagueBearer, DefenseEnum.None},
                    { CustomRoles.Pestilence, DefenseEnum.Invincible},
                    { CustomRoles.Vulture, DefenseEnum.None},
                    { CustomRoles.CSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.Medium, DefenseEnum.None},
                    { CustomRoles.Alturist, DefenseEnum.None},
                    { CustomRoles.Psychic, DefenseEnum.None},
                    { CustomRoles.Arsonist, DefenseEnum.Basic},
                    { CustomRoles.Jester, DefenseEnum.None},
                    { CustomRoles.Terrorist, DefenseEnum.None},
                    { CustomRoles.Executioner, DefenseEnum.Basic},
                    { CustomRoles.Opportunist, DefenseEnum.None},
                    { CustomRoles.Survivor, DefenseEnum.None},
                    { CustomRoles.AgiTater, DefenseEnum.Basic},
                    { CustomRoles.PoisonMaster, DefenseEnum.Basic},
                    { CustomRoles.SchrodingerCat, DefenseEnum.None},
                    { CustomRoles.Egoist, DefenseEnum.None},
                    { CustomRoles.EgoSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.Jackal, DefenseEnum.Basic},
                    { CustomRoles.Sidekick, DefenseEnum.Basic},
                    { CustomRoles.Marksman, DefenseEnum.Basic},
                    { CustomRoles.Juggernaut, DefenseEnum.Basic},
                    { CustomRoles.JSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.Phantom, DefenseEnum.None},
                    { CustomRoles.NeutWitch, DefenseEnum.None},
                    { CustomRoles.Hitman, DefenseEnum.None},
                    { CustomRoles.BloodKnight, DefenseEnum.None},
                    { CustomRoles.Veteran, DefenseEnum.None},
                    { CustomRoles.GuardianAngelTOU, DefenseEnum.None},
                    { CustomRoles.TheGlitch, DefenseEnum.None},
                    { CustomRoles.Werewolf, DefenseEnum.Basic},
                    { CustomRoles.Amnesiac, DefenseEnum.None},
                    { CustomRoles.Reviver, DefenseEnum.None},
                    { CustomRoles.Demolitionist, DefenseEnum.None},
                    { CustomRoles.Bastion, DefenseEnum.Basic},
                    { CustomRoles.Hacker, DefenseEnum.None},
                    { CustomRoles.CrewPostor, DefenseEnum.None},
                    { CustomRoles.Magician, DefenseEnum.None},
                    { CustomRoles.Chancer, DefenseEnum.None},
                    { CustomRoles.Sellout, DefenseEnum.None},
                    { CustomRoles.Satan, DefenseEnum.None},
                    { CustomRoles.CPSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.MGSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.RBSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.TGSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.WWSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.JugSchrodingerCat,DefenseEnum.None},
                    { CustomRoles.MMSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.PesSchrodingerCat, DefenseEnum.None},
                    { CustomRoles.BKSchrodingerCat, DefenseEnum.None},
                };
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    switch (role.GetRoleType())
                    {
                        case RoleType.Impostor:
                            defenseValues.Add(role, DefenseEnum.None);
                            break;
                        case RoleType.Madmate:
                            defenseValues.Add(role, DefenseEnum.None);
                            break;
                        case RoleType.Coven:
                            if (role == CustomRoles.CovenWitch)
                                defenseValues.Add(role, DefenseEnum.Basic);
                            else
                                defenseValues.Add(role, DefenseEnum.None);
                            break;
                        default:
                            if (!defenseValues.ContainsKey(role))
                                defenseValues.Add(role, DefenseEnum.None);
                            break;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                TownOfHost.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TownOfHost.Logger.Error(ex.Message, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}", "GitVersion");

            if (!File.Exists(TEMPLATE_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                    if (File.Exists(@"./TEMPLATE.txt"))
                    {
                        File.Move(@"./TEMPLATE.txt", TEMPLATE_FILE_PATH);
                    }
                    else
                    {
                        TownOfHost.Logger.Info("No template.txt file found.", "TemplateManager");
                        File.WriteAllText(TEMPLATE_FILE_PATH, "test:This is template text.\\nLine breaks are also possible.\ntest:これは定型文です。\\n改行も可能です。");
                    }
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Exception(ex, "TemplateManager");
                }
            }
            if (!File.Exists(ROLES_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                    if (File.Exists(@"./ROLES.txt"))
                    {
                        File.Move(@"./ROLES.txt", ROLES_FILE_PATH);
                    }
                    else
                    {
                        TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                        File.WriteAllText(ROLES_FILE_PATH, "IMPS=\r\nbountyhunter: BountyHunter(Impostors) \\nIf the BountyHunters kill their designated target, their next kill cooldown will be much less than usual.\\nKilling a player except their current target results in an increased kill cooldown.\\nTargets are changed at regular intervals.\r\nfireworks: FireWorks(Impostors) \\nThey can put a few fireworks by Shapeshift.\\n After setting everything, you can ignite all at once by shapeshift when you are the last Impostor.\\nOnly after that you can kill.\r\nmercenary:Mercenay(Impostors):\\nMercenary has even shorter kill cooldown.\\nUnless taking a kill by deadline, they murder themselves instantly.\\nIf the Mercenary decides to shift, they will suicide for even thinking about it.\r\nshapemaster:ShapeMaster(Impostors):\\nShapeMaster has no Shapeshift cooldown.\\nOn the other hand, their default Shapeshift duration is shorter (default: 10s).\r\nmare:Mare(Impostors):\\nThey can only kill while lights are out, but next kill cooldown will be half.\\nWhile lights out they can move faster, and yet their name looks red by everyone.\r\nvampire:Vampire(Impostors):\\nThe Vampires can bite players instead of kill.\\nBitten players die after a configurable amount of time or just before emergency meeting call.\\nWhen biting the bait, However, they take a no delay kill and are forced to self report.\r\nmorphling:Morphling(Imposotrs):\\nThe Morphling is just a Shapeshifter with nothing added. It is added for people to still allow vanilla roles inside the game.\r\nvampress:Vampress(Impostors):\\nThe Vampiress can bite when not shifting, but kills regularly when shifted.\r\nconsort:Consort(Impostor):\\nThe Consort is essentially an evil Escort. They roleblock when unshifted and kill regularly when they are shifted.\r\nwarlock:Warlock(Impostors):\\nThe Warlocks can curse other players before attempting to shapeshift.\\nAfter cursing, when they shift, the player next to the cursed is killed no matter how far away they are.\\nWhile in a shifted form, they can kill regularly.\r\nwitch:Witch(Impostors):\\nThe Witches can perform kills or spells by turns.\\nThe players spelled by Witches before a meeting are marked 'cross' in the meeting,\\nand unless exiling Witches, They all die just after the meeting.\r\nmafia:Mafia(Impostors):\\nInitially, the Mafias cannot kill (still have a button).\\nThey will be able to kill after Impostors except them are all gone.\r\npuppeteer:Puppeteer(Impostors):\\nThey can perform secondhand kills but cannot kill like other Impostors.\\nTheir direct kill is blocked, but instead, the protected player forcibly kills another player first getting in kill range.\r\ndisperser:Disperser(Impostors):\\nThe Disperser can shift to disperse everyone onto a random vent.\r\nfreezer:Freezer(Impostors):\\nThe Freezer can shift to temporarily freeze someone in place. After they unshift, that person is allowed to move.\r\ncleaner:Cleaner(Impostors):\\nThe Janitor can report a body to clean it, in return for their kill cooldown being used.\r\nyingyanger:YingYanger(Impostors):\\nThe Ying Yanger can tag 2 crewmates. Once the crewmates go within kill distance of each other, they kill each other and the Ying Yanger is a regular impostor until the next meeting depending on settings.\r\ntimethief:TimeThief(Impostors):\\nEvery kill cuts down discussion and voting time in meeting.\\nDepending on option, the lost time is returned after they die.\r\nsniper:Sniper(Impostors):\\nSniper can shoot players so far away.\\nthey kill a player on the extension line from Shapeshift point to release point.\\nYou can perform normal kills after all bullets run out.\r\nsilencer:Silencer(Impostors):\\nThe first killing attempt will silence the crewmate in the next meeting. After that, you are a regular impostor.\r\npickpocket:Pickpocket(Impostors):\\nThe Vote Stealer gains the vote of whoever they kill.\r\ncorruptedsheriff:CorruptedSheriff(Impostors):\\nA Corrupted Sheriff, or Traitor, spawns when the last Impostor is voted off inside a meeting or killed.\\nNo Traitor will spawn if the Impostor died by other means.\\nThe Traitor will replace the Sheriff role always when Corrupted Sheriff is not at 0%.\\nYou can also make it so there is a minimum number of players for Traitor to spawn.\\nOtherwise, it is just a regular Impostor with nothing different.\r\nswooper:Swooper(Impostors):\\nThe Swooper can vent to turn invisible. They can also kill.\r\ncamoflager:Camouflager(Impostors):\\nThe Camouflager can shift to temporarily make everyone shift into who they shifted into. After shifting, everyone is the same person. WHen the duration is over or a meeting is called, everyone returns back to normal form.\r\nninja:Ninja(Impostors):\\nThe Ninja is an Impostor that can kill players from anywhere.\\nIf a kill is made during a shape-shift, the kill is guarded. When the transformation is released, the player teleports to the player they used their button on and does the kill.\\nWhen not shape-shifting, it is possible to make normal kills./nThere is no transformation cooldown, and the transformation can be performed indefinitely.\r\ngrenadier:Grenadier(Impostors):\\nThe Grenadier uses their shifitng ability to bring all crew's vision to none. Any non-impostor sided players will have their vision set to 0, and won't be able to see. When the Grenadier unshifts, shortly after the crew will have normal vision.\r\nminer:Miner(Impostors):\\nThe Miner can pet their pet to enter the vent they last went in.\r\nmanipulator:Manipulator(Impostors):\\nThe Manipulator has a longer kill cooldown than other Impostors. When someone reports a body killed by the Manipulator, the next meeting is sabotaged. When this happens, the anonymous voting option will be flipped, meaning if you had them on they are off for this meeting. In addition, the voting and discussion time has been set to a predetermined amount by host. The Manipulator's voting and discussion time overwrites the ones TimeThief has.\r\nescapist:Escapist(Modifier):\\nEscapists can use their pet to teleport and mark locations.\\nThe first time they pet, it will mark a location.\\nThe next time they pet, it will teleport them back to the location they marked, and they are able to mark another location. The process repeats.END\r\nbomber:Bomber(Impostors):\\nThe Bomber plants a bomb on someone via their kill button. You can change whether the Bombed player is visible to all impostors. The Bombed player will kill the next person that shows up to them after they have been bombed. Once they kill someone, they are roleblocked for some time defined by the host and cannot move during this too. You can also change whether or not the Bomber cannot move either. If a meeting is called before they can kill someone, the Bombed player dies on the spot. There can only be one Bombed player at a time.\",\"(Impostors):\\nThe Bomber plants a bomb on someone via their kill button.\r\ncreeper:Creeper(Impostors):\\nA Creeper is a normal impostor, however, they can use the pet button to explode. Exploding causes everyone around them, regardless of role, to die. The Creeper dies as well.\r\nCREWMATES=\r\nbait:Bait(Modifiers):\\nWhen killed, they force the killer to self report in no time.\r\nbodyguard:Bodyguard(Crewmates):\\nThe Bodyguard can protect the life of someone. For the first meeting, vote someone to lock your target in. Whenever your target is attempted to be killed, you kill the killer and also kill yourself.NOTE IF YOU PROTECT A KILLER THEY DIE TO KAMIKAZE AND DEMO!\r\nmedic:Medic(Crewmates):\\nThe Medic can cast a shield around someone. For the first meeting, vote someone to lock your target in. When your target is attempted to be killed, the kill is blocked and the killer sees a Guardian Angel shield. The Medic cannot unshield, and the shield is attached to that person for the rest of the game.\r\nparamedic:Paramedic(Crew):\\nThe Paramedic can cast a shield around someone. Vote someone at the first meeting to shield them. When someone attempts to kill someone with a shield, the kill is blocked and the killer sees a Guardian Angel shield. The Paramedic cannot unshield, and the shield is attached to that person for the rest of the game. The Paramedic has a portable vitals panel and can see cause of death in meetings.\r\noracle:Oracle(Crewmates):\\nThe Oracle can reveal someone's role upon death. For the first meeting, vote someone to lock your target in. When you die, the next meeting will have their role displayed to everyone.\r\nescort:Escort(Crewmates):\\nThe Escort's kill button is the same as The Glitch's hack ability. However, you cannot roleblock the glitch and the glitch cannot roleblock you.\r\ncrusader:Crusader(Crewmates):\\nThe Crusader can use their kill button to protect someone like the Bodyguard. However, they do not suicide like the Bodyguard.\r\nveteran:Veteran(Modifier):\\nVeteran can pet their pet to activate their Alert function.\\nIf you don't have a pet, a pet will be given automatically for you to use. This will only be visible to you.\\nWhen alerted, any role that interacts with the Veteran, will die.\\nSettings can choose the duration, cooldown, and how many alerts. \\nOnly 1 Veteran can exist per game.\r\ntransporter:Transporter(Modifier):\\nThe Transporter can pet their pet to transport 2 random people. \\nIf the player is dead, their body will be transported instead.\\nIf you don't have a pet, a pet will be given automatically for you to use. This will only be visible to you.\r\nlighter:Lighter(Crewmates):\\nAfter finishing all the task, The lighters have their vision expanded and ignore lights out.\r\nmayor:Mayor(Modifier):\\nThe Mayors' votes count twice or more.\\nDepending on the options, they can call emergency meeting by entering vents.\r\nsabotagemaster:SabotageMaster(Crewmates):\\nThey can fix both of Communications in MIRA HQ, Reactor and O2 by fixing either.\\nLights can be fixed by touching a single lever.\\nOpening a door will open all the linked doors.\r\nsheriff:Sheriff(Crewmates):\\nThe Sheriffs can kill Impostors you will gain there vote.\\nIf trying to kill the Crewmates, however, they will kill themselves instead.\\nWhether or not they can kill Madmates or Neutrals is up to each option.\\nThey have no tasks.\r\ninvestigator:Investigator(Crewmates):\\nThe Investigator can investigate people to find out if their role is bad or not. \\nUse the kill button to investigate. \\nThe Veteran can kill you if the option is turned on.\r\nphysicist:Physicist(Crewmates):\\nThe Physicist is just a regular Scientist with nothing different. It is added for suppot for vanilla roles to be used in conjunction with modded roles.\r\nmechanic:Mechanic(Crewmates):\\nThe Mechanic is just a regular engineer with nothing different. It is added for support for vanilla roles to be used in conjunction with modded roles.\r\nmystic:Mystic(Crewmates)\\nThe Mystic gets a reactor flash when someone dies.\r\nsnitch:Snitch(Crewmates):\\nWhen finishing all the tasks, the Snitches can see Impostors with their red player name.\\nWhen the Snitches have one task left, they will be revealed to the Impostors.\r\nlovers:Lovers(Modifiers):\\nThe Lovers are additionally assigned to two of all players.\\nWin if the game ends with both lovers alive(except Crewmates task completion).\\nIf one lover dies, the other lover suisides.\r\nspeedbooster:SpeedBooster(Crewmates):\\nFinishing all the tasks boosts the player speed of someone alive.\r\ndoctor:Doctor(Modifier):\\nThe Doctors have a portable vitals panel just like the vanilla Role Scientists.\\nIn addtion, the Doctors can see all causes of death in meetings.\r\ntrapster:Trapper(Modifier):\\nThe Trapsters forbid the killer to move in the least for a configurable seconds.\r\ndictator:Dictator(Crewmates):\\nWhen you vote for someone in a meeting, they forcibly break that meeting and exile the player they vote for.\\nAfter exercising the force, the Dictators die just after meeting.\r\nchild:Child(Crewmates):\\nWhen you die by voting or killing, you instantly win. (Arsonist still wins even if Child is killed.\r\nsleuth:Sleuth(Modifiers):\\nThe Sleuth knows the role of the bodies they report.\r\npsychic:Psychic(Crewmates):\\nThe Psychic will see 3 random people's names in red every meeting. At least 1 of the red people are bad. It will only display less than 3 of there are less than 3 people alive.\r\nbastion:Bastion(Crewmate):\\nAs Bastion, vent inside a vent to place a bomb inside of it. \\nAfterwards, if another Bastion or a role that can vent without being kicked out vents in that vent, they die. \\nYou can only bomb 1 person per round.\r\ndemolitionist:Demolitionist(Modifier):\\nWhen the Demolitionist is killed, they bomb the ground, giving the killer a few seconds to find a vent. \\nIf they don't vent in time, the killer suicides. \\nThe killer is marked with an Arsonist triangle.\r\nmedium:Medium(Crewmates):\\nThe Medium is basically Sleuth but for the killer. \\nWhen the Medium reports a body, they know the killer's role. \\nOptionally, they can also be given the ability to Meditate. Meditating allows them to see all ghosts since the last meeting, they have to die in the round of the meditate so you can see them.\r\nalturist:Alturist(Crewmates):\\nThe Alturist can use the report button to revive someone back from the dead. Due to bugs, the revived person will self report the Alturist's body as the game still thinks they are dead and gives them Ghost Powers.\r\ntimemanager:TimeManager(Crewmates):\\nEvery kill adds time to discussion and voting time in meeting.\\nDepending on option, the gained time is returned after they die.\r\nreviver:Reviver(Crewmates):\\nBring the person back to life that you report. You cant report the bodys of imps\r\nwizard:Wizard(Crewmates):\\nThe Wizard can vent to temporarily turn invisible. \\nVent to turn invisible. \\nThe Wizard can only vent to go insivible.\r\nNK=\r\narsonist:Arsonist(Neutrals):\\nThey can douse other players by using the kill button and remaining next to the player for a few seconds.\\nAfter dousing everyone alive the Arsonists can enter the vents to ignite which results in Arsonist win.\r\njester:Jester(Neutrals):\\nThey win the game as a solo if they get voted out during a meeting. Can also be given the option to be able to vent.\\nOtherwise Jester lose.\r\nphantom:Phantom(Neutrals):\\nThe Phantom has the same tasks as others and is unkillable from the start. After finishing a few tasks, they become killable. When they have the remaining tasks to send alert, everyone gets an arrow pointing straight towards the Phantom. Kill the Phantom before they finish to stop them.\r\nterrorist:Terrorist(Neutrals):\\nThe Terrorists win alone when killed with all of his tasks completed.\r\nexecutioner:Executioner(Neutrals):\\nThe Executioners are assigned a target upon the game starting.\\nTheir target can be recognised by their name being darkened among the rest.\\nThey win if the target is voted out.\\nIf the target dies before voted out, the Executioner changes their Role and become Crewmate, Jester, or Opportunist according to a game option.\\nIf the target is the Jester, the Executioners can be an additional winner.\r\nswapper:Swapper(Neutrals):\\nThe Swapper is an Executioner but with a bit of a twist. After each meeting, they have a randomly assigned target to vote out. If their target dies or leaves the game, they will not be assigned a new target until the next meeting.\r\n:schrodingercat:SchrodingerCat(Neutrals):\\nWhen killed, they prevents the kill and belongs to the killer's team.\r\nopportunist:Opportunist(Neutrals):\\nThey win the game with any other Roles only if they are just alive at the game end(only say the person who killed you).\r\nsuper spy:Super spy(nk/crew):\\nYou have being revived you also cant be killed and can see and walk though walls(only say the person who killed you).\r\nchancer:Chancer(crew):\\nYou have being revived this is your 2nd chance.\r\nsatan:Satan(imps):\\nyou should still win with the imps do tasks to kill DONT DO TASKS IF DEAD (only say the person who killed you).\r\nhitman:Hitman(Neutrals):\\nThe Hitman is a neutral benign role who can kill. They win with anyone and their only goal is to help whatever team they choose. They have no kill consequence and may kill anyone. They do not count towards the Neutral Killers as the Hitman is not one, they are a hired killer. They can be chosen to become Traitor for impostors. They lose if they die (unless turned corrupt, where they then win with the Impostors).\r\negoist:Egoist(Neutrals):\\nAfter all the Impostors has gone, the Egoists Replace Impostor win.\\nThe Impostors and Egoists can see each other.\r\njackal:Jackal(Neutrals):\\nSerial Killer can kill anyone, even Crewmates and Impostors.\\nSerial Killer can win when living players are only 1 serial killer and 1 crew. Serial Killer can also be given a Sidekick.\r\nneutwitch:NeutWitch(Neutrals):\\nThe Witch is a neutral killer not counted to the game. The Witch is just a puppeteer clone that can kill anyone but themselves. They win with whoever wins as long as crewmates do not win. The Witch can be killed by the Sheriff no matter what.\r\npostman:Postman(Neutrals):\\nA Postman can complete tasks to gather letters to deliver to others. After completing a task, you will see your target under your name, and an arrow pointing to them (depending on host's settings).\\nOnce you go within distance of the target (defined by host), you will see a Guardian Angel shield around the target. That signifies you succesfully delivered the mail.\\nThe Postman can deliver mail to an Alerted Veteran without being killed.\\nCompleting another task while having a target will result in a suicide.\\nOnce the Postman has completed all tasks and/or delivered a message to all living players, they are done delivering.\\nNow all they have to do is wait for a meeting. After a meeting is called, everyone will be notified that the Postman has finished delivering. The alive crew must eject the Postman that meeting or the Postman will win.\\nThe Postman CANNOT bomb the Pestilence. So if there is a Pestilence alive, the Postman will not win.\",\"(Neutrals):\\nA Postman can complete tasks to gather letters to deliver to others. After completing a task, you will see your target under your name, and an arrow pointing to them (depending on host's settings).\\nOnce you go within distance of the target (defined by host), you will see a Guardian Angel shield around the target. That signifies you succesfully delivered the mail.\\nThe Postman can deliver mail to an Alerted Veteran without being killed.\\nCompleting another task while having a target will result in a suicide.\\nOnce the Postman has completed all tasks and/or delivered a message to all living players, they are done delivering.\\nNow all they have to do is wait for a meeting. After a meeting is called, everyone will be notified that the Postman has finished delivering. The alive crew must eject the Postman that meeting or the Postman will win.\\nThe Postman CANNOT bomb the Pestilence. So if there is a Pestilence alive, the Postman will not win.\",\"(Neutrals):\\nA Postman can complete tasks to gather letters to deliver to others. After completing a task, you will see your target under your name, and an arrow pointing to them (depending on host's settings).\\nOnce you go within distance of the target (defined by host), you will see a Guardian Angel shield around the target. That signifies you succesfully delivered the mail.\\nThe Postman can deliver mail to an Alerted Veteran without being killed.\\nCompleting another task while having a target will result in a suicide.\\nOnce the Postman has completed all tasks and/or delivered a message to all living players, they are done delivering.\\nNow all they have to do is wait for a meeting. After a meeting is called, everyone will be notified that the Postman has finished delivering. The alive crew must eject the Postman that meeting or the Postman will win.\\nThe Postman CANNOT bomb the Pestilence. So if there is a Pestilence alive, the Postman will not win.\",\"(Neutrals):\\nA Postman can complete tasks to gather letters to deliver to others. After completing a task, you will see your target under your name, and an arrow pointing to them (depending on host's settings).\\nOnce you go within distance of the target (defined by host), you will see a Guardian Angel shield around the target. That signifies you succesfully delivered the mail.\\nThe Postman can deliver mail to an Alerted Veteran without being killed.\\nCompleting another task while having a target will result in a suicide.\\nOnce the Postman has completed all tasks and/or delivered a message to all living players, they are done delivering.\\nNow all they have to do is wait for a meeting. After a meeting is called, everyone will be notified that the Postman has finished delivering. The alive crew must eject the Postman that meeting or the Postman will win.\\nThe Postman CANNOT bomb the Pestilence. So if there is a Pestilence alive, the Postman will not win.\",\"(Neutrals):\\nA Postman can complete tasks to gather letters to deliver to others. After completing a task, you will see your target under your name, and an arrow pointing to them (depending on host's settings).\\nOnce you go within distance of the target (defined by host), you will see a Guardian Angel shield around the target. That signifies you succesfully delivered the mail.\\nThe Postman can deliver mail to an Alerted Veteran without being killed.\\nCompleting another task while having a target will result in a suicide.\\nOnce the Postman has completed all tasks and/or delivered a message to all living players, they are done delivering.\\nNow all they have to do is wait for a meeting. After a meeting is called, everyone will be notified that the Postman has finished delivering. The alive crew must eject the Postman that meeting or the Postman will win.\\nThe Postman CANNOT bomb the Pestilence. So if there is a Pestilence alive, the Postman will not win.\"\r\nhunter:Sidekick(Neutrals):\\nThe Hunter is the Serial Killer's pal and helps them win. If Serial Killer and Hunter are among 2 crew, they auto-win.\r\nplaguebearer:PlagueBearer(Neutrals):\\nYour kill button is an Infect button. Infect everyone until you become Pestilence.\r\npestilence:Pestilence(Neutrals):\\nPestilence cannot die and can only be voted. \\nWhen someone tries to kill Pestilence, they die instead. \\nEven their Lover dying won't bring the Pestilence down.\\nHowever, if Pestilence is killed by an un-direct source, such as Warlock and Puppeteer, the Pestilence WILL die.\r\njuggernaut:Juggernaut(Neutrals):\\nThe Juggernaut's kill cooldown decreases with each kill.\r\nmarksman:Marksman(Neutrals):\\nThe Marksman's kill distance increases with each kill.\r\nagitater:AgiTater(Neutrals):\\nThe AgiTater's premise is essentially Hot Potato.\\nThe player will be able to click their kill button and the targeted person will have a bomb.\\nThe AgiTater can only pass a bomb once per round however.\\nThe player that recieved the bomb will be notified with a message that will be above their head until they pass the bomb.\\nWhen near another player, the bomb will be given to that player and they cannot throw the bomb back to you, and the cycle starts again with them finding someone else.\\nWhen the next meeting is called, the person who has the bomb will die.\\nCareful though! The bomb can be passed back to the AgiTater! And, attempting to pass a bomb to an Alerted Veteran or Pestilence, will result in the bombed person dying.\r\nvulture:Vulture(Neutrals):\\nThe Vulture has to eat a certain number of bodies to win. \\nWhen the Vulture attempts to report a body, the body will be determined ate to the Vulture. \\nThis means that the Vulture cannot report any dead bodies.\r\nwerewolf:Werewolf(Neutrals):\\nThe Werewolf can vent to activate their rampage function. Once you don't get kicked out of the vent, then your rampage is ready. While rampaged, they have a short kill cooldown and can only kill for a short amount of time. After the Rampage Duration, they can no longer kill. You can choose if the Werewolf can vent during rampage.\r\nglitch:TheGlitch(Neutrals):\\nThe Glitch is a neutral killing role with the Ability to shapeshift. \\nThey can pet their pet to switch killing modes. \\nAfter every meeting, you are on killing mode. \\nWhen you pet your pet, you are on Hacking Mode. \\nOn hacking mode, press the kill button on someone to prevent them from doing something for a set period of time.\\nIf you don't have a pet, a pet will be given automatically for you to use. This will only be visible to you.\r\nguardianangel:GuardianAngelTOU(Neutrals):\\nThe Guardian Angel has a target they are supposed to protect. \\nIf their target wins, so does the GA. \\nThe GA can vent to temporarily prevent their target from being killed.\r\namnesiac:Amnesiac(Neutrals):\\nSteal someone's role by clicking the report button.\r\nhacker:Hacker(Neutrals):\\nAs the Hacker, your goal is to fix a number of points by fixing sabotages. \\nYou have the powers of a SabotageMaster. \\nComplete sabotages to win. \\nElectrical gains the most points. \\nReactor gives you like 2 points. \\nThe Sheriff can kill Hacker no matter what.\r\nbloodknight:BloodKnight(Neutrals):\\nEach kill the Bloodknight does will protect them from attacks for a few seconds.\r\nsurvivor:Survivor(Neutrals):\\nThe Survivor is Opportunist with a buff. The Survivor can vent to protect them from all attacks. However, unlike Opportunist, Survivor cannot win with Child, Jester, and Executioner.\r\ndracula:Dracula(Neutral Killing):\\nThe dracula is a neutral killing role with delayed kills.\\nThey win alone and must kill everyone.\\n\\nWin Condition: Kill everyone with a bite.\",\"(Neutrals):\\nThe dracula is a neutral killer similar to a Vampire. They win alone and must kill everyone.\",\"(Neutrals):\\nThe Dracula is a neutral killer similar to a Vampire. They win alone and must kill everyone.\r\nmagician:Magician(Neutrals):\\n(DONT DO TASKS WHEN DEAD)The Magician can do tasks to kill the Nearest person !!. \\nThe Nearest person can be any role.\r\nlawyer:Lawyer(NK):\\nThe Lawyer has a client to defend during meetings.\\nIf their target is voted out or wins, so does the Lawyer. \\nIf the target dies the lawyer becomes a new role determined by settings.\r\nhustler:Hustler(Neutrals):\\nThe Huster cons the other player of there vote by killing.\r\nMODIFIERS=\r\nflash:Flash(Modifiers):\\nFlash is a Global Modifier that can be applied to anyone. \\nThe Flash has a boost of speed that makes them faster than others.\r\ntiebreaker:TieBreaker(Modifiers):\\nTieBreaker is a Global Modifier that can be applied to anyone. \\nWhenever there is a tie, and the TieBreaker is on one of the sides that tied, their vote gets priority.\r\ndiseased:Diseased(Modifiers):\\nDiseased is a Crew Modifier that can only be applied to crewmates. \\nWhenever someone kills Diseased, they are met with a cooldown multiplied by the chosen amount in the settings.\r\nobvious:Obvious(Modifiers):\\nThe Obvious is a Global Modifier that can be applied to anyone. \\nWhenever the Obvious gets within range of a body, they auto report it.\r\noblivious:Oblivious(Modifiers):\\nOblivious is a Global Modifier that can be applied to anyone. \\nAs Oblivious, you cannot report dead bodies.\r\nbewilder:Bewilder(Modifiers):\\nBewilder is a Crew Modifier that can only be applied to crewmates. \\nAs Bewilder, when you get killed, the killer will steal your vision.\r\ntorch:Torch(Modifiers):\\nTorch is a Crew Modifier that can only be applied to crewmates. \\nAs Torch, you still have full vision even when lights are turned off.\r\nescalation:Escalation(Modifiers):\\nEscalation is a Global Modifier that can be applied to anyone. \\nEach kill that happens randomly throughout the game will enhance your speed!\r\ndoubleshot:DoubleShot(Modifiers):\\nDoubleShot is a Guesser Modifier that can only be given to Vigilante and Assassin. When having the DoubleShot Modifier, you gain a second guess. When you misguess someone's role, your guess will not go through but you will be notified of your misguess.\r\npumpkinspotion:PUMPkinsPotion(Modifier):\\nThe PUMPkinsPotion uses a vent to turn invisible can be paired with any role.\r\nwatcher:Watcher(Modifier):\\nThe Watchers can see colored votes regardless of anonymous votes.\r\nMADMATES=\r\ncrewpostor:CrewPostor(Neutrals):\\n(DONT DO TASKS WHEN DEAD)The CrewPostor can do tasks to kill the Nearest person . \\nThe Nearest person can be any role, so you can even kill your fellow impostors.\r\nGUESSER=\r\nassassin:\"<color=#EDC240>Assassin(Neutrals):\\nAs a Guessing role,\\n Type<color=#FF0000> /shoot show <color=#EDC240> This will show 2 lists \\n (ID) = The NUMBER after players name. \\n (RoleID) = The NUMBER After the role name.\\n<color=#FF0000> /shoot 4(ID) 17(RoleID) <color=#EDC240> = This is how you Shoot. \\n Explain = <color=#FF0000> /shoot 4 17\r\nvigilante:\"<color=#EDC240>Vigilante(Neutrals):\\nAs a Guessing role,\\n Type<color=#FF0000> /shoot show <color=#EDC240> This will show 2 lists \\n (ID) = The NUMBER after players name. \\n (RoleID) = The NUMBER After the role name.\\n<color=#FF0000> /shoot 4(ID) 17(RoleID) <color=#EDC240> = This is how you Shoot. \\n Explain = <color=#FF0000> /shoot 4 17\r\npirate:\"<color=#EDC240>Pirate(Neutrals):\\nAs a Guessing role,\\n Type<color=#FF0000> /shoot show <color=#EDC240> This will show 2 lists \\n (ID) = The NUMBER after players name. \\n (RoleID) = The NUMBER After the role name.\\n<color=#FF0000> /shoot 4(ID) 17(RoleID) <color=#EDC240> = This is how you Shoot. \\n Explain = <color=#FF0000> /shoot 4 17\r\nkamikaze:(Crewmate):\\nWhen the Kamikaze dies they take their killer with them but watch out everyone can see your role when comms is on .\r\nforecaster:Forecaster,\"Reveal the role of the person you vote in the next meeting\"");
                    }
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Exception(ex, "TemplateManager");
                }
            }
            if (!File.Exists(BANNEDWORDS_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                    if (File.Exists(@"./BANNEDWORDS.txt"))
                    {
                        File.Move(@"./BANNEDWORDS.txt", BANNEDWORDS_FILE_PATH);
                    }
                    else
                    {
                        TownOfHost.Logger.Info("No bannedwords.txt file found.", "BannedWordsManager");
                        File.WriteAllText(BANNEDWORDS_FILE_PATH, $"Enter banned words here. Note, the game will take each message and turn each character into the lowercase version. So no need to include every variation of one word.");
                    }
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Exception(ex, "TemplateManager");
                }
            }
            if (!File.Exists(RSETTINGS_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                    if (File.Exists(@"./RSETTINGS.txt"))
                    {
                        File.Move(@"./RSETTINGS.txt", RSETTINGS_FILE_PATH);
                    }
                    else
                    {
                        TownOfHost.Logger.Info("No RSETTINGS.txt file found.", "RSETTINGS");
                        File.WriteAllText(RSETTINGS_FILE_PATH, $"KillCooldown=17 \r\nPlayerSpeedMod=1.5 \r\nCrewLightMod=0.75\r\nImpostorLightMod=1.5 \r\nNumCommonTasks=1 \r\nNumLongTasks=0 \r\nNumShortTasks=5 \r\nNumEmergencyMeetings=2 \r\nKillDistance=0 \r\nDiscussionTime=15 \r\nVotingTime=60 \r\nConfirmImpostor=true\r\nVisualTasks=false \r\nEmergencyCooldown=17\r\n\r\nShapeshifterCooldown=10\r\nShapeshifterDuration=30\r\nShapeshifterLeaveSkin=false\r\n\r\nImpostorsCanSeeProtect=false  \r\nProtectionDurationSeconds=10\r\nGuardianAngelCooldown=60\r\n\r\nScientistCooldown=15\r\nScientistBatteryCharge=5\r\n\r\nEngineerCooldown=0\r\nEngineerInVentMaxTime=5\r\n.");
                    }
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Exception(ex, "TemplateManager");
                }
            }
            if (!File.Exists(BANNEDFRIENDCODES_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                    if (File.Exists(@"./BANNEDFRIENDCODES.txt"))
                    {
                        File.Move(@"./BANNEDFRIENDCODES.txt", BANNEDFRIENDCODES_FILE_PATH);
                    }
                    else
                    {
                        TownOfHost.Logger.Info("No bannedfriendcodes.txt file found.", "BannedFriendCodesManager");
                        File.WriteAllText(BANNEDFRIENDCODES_FILE_PATH, $"Please include the part before and after #. Make sure each friend code is on a new line.\nHere are some example friend codes:\nbrassfive#8929\nraggedsofa#2041\nmerryrule#0412\ngnuedaphic#7196\nNOTE: These people were people who were banned from the TOH TOR Server for various reasons. It is recommended to keep these people here.\nNOTE: Devs of the mod are unbannable. Putting their friend codes in this file has no affect.");
                    }
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Exception(ex, "TemplateManager");
                }
            }
            //TAG spawns
            if (!File.Exists(TIMMAY_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                    if (File.Exists(@"./warmtablet#3212.txt"))
                    {
                        File.Move(@"./warmtablet#3212.txt", TIMMAY_FILE_PATH);
                    }
                    else
                    {
                        TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                        File.WriteAllText(TIMMAY_FILE_PATH, "type:sforce\r\ncode:warmtablet#3212\r\ncolor:#F391EE\r\ntoptext:<color=#3BCFD4>♡</color> <color=#F20094>C</color><color=#ed2e72>HA</color><color=#e86549>O</color><color=#e5892e>S A</color><color=#a8a26a>D</color><color=#75b79c>MI</color><color=#3bcfd4>N</color> <color=#F20094>♡</color>\r\nname:<color=#F20094>♥</color> <color=#3bcfd4>T</color><color=#96b373>i</color><color=#cfa135>m</color><color=#fa7027>m</color><color=#f74b4b>a</color><color=#f20094>y</color> <color=#3BCFD4>♥</color>");
                    }
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Exception(ex, "TemplateManager");
                }
            }
            if (!File.Exists(BOW_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                    if (File.Exists(@"./beespotty#5432.txt"))
                    {
                        File.Move(@"./beespotty#5432.txt", BOW_FILE_PATH);
                    }
                    else
                    {
                        TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                        File.WriteAllText(BOW_FILE_PATH, "type:sforce\r\ncode:beespotty#5432\r\ncolor:#F391EE\r\ntoptext:<color=#F9CFCE></color><color=#FAA9DA>CH</color><color=#FB82E5>AO</color><color=#FC5CF1>S C</color><color=#FC35FC>O-</color><color=#FD48D1></color><color=#FE5AA6>OW</color><color=#FF6D7B>N</color><color=#FF7666>ER</color><color=#FF7F50></color>\r\nname:<color=#FF4747>R</color><color=#FFA347>A</color><color=#FFFF47>I</color><color=#70FF70>N</color><color=#7070FF>B</color><color=#BB5CFF>O</color><color=#DA85FF>W</color>");
                    }
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Exception(ex, "TemplateManager");
                }
                if (!File.Exists(MAMA_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./blokemagic#3008.txt"))
                        {
                            File.Move(@"./blokemagic#3008.txt", MAMA_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(MAMA_FILE_PATH, "type:sforce\r\ncode:blokemagic#3008\r\ncolor:#E10505\r\ntoptext:<color=#E10505>♥</color> <color=#FFED00>CH</color><color=#FBCE01>AO</color><color=#F7AE02>S A</color><color=#F07403>D</color><color=#EA4804>M</color><color=#E41B05>I</color><color=#E10505>N</color> <color=#FFED00>♥</color> \r\nname:<color=#FFED00>♥</color> <color=#E10505>M</color><color=#E41B05>a</color><color=#EA4804>m</color><color=#F07403>a</color> <color=#F7AE02>B</color><color=#FBCE01>B</color><color=#FFED00>1</color> <color=#E10505>♥</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(YEETUS_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./gaolstaid#3696.txt"))
                        {
                            File.Move(@"./gaolstaid#3696.txt", YEETUS_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(YEETUS_FILE_PATH, "type:sforce\r\ncode:gaolstaid#3696\r\ncolor:#F391EE\r\ntoptext:<color=#0566EF>☀</color><color=#085BE7>CHA</color><color=#0A51DF>OS ART</color><color=#0D46D8>IST</color><color=#0F3CDO>☀</color>\r\nname:<color=#0F3CDO>☀</color><color=#0D46D8>YE</color><color=#0A51DF>ET</color><color=#085BE7>US</color><color=#0566EF>☀</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(PUMP_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./retroozone#9714.txt"))
                        {
                            File.Move(@"./retroozone#9714.txt", PUMP_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(PUMP_FILE_PATH, "type:sforce\r\ncode:retroozone#9714\r\ncolor:#F391EE\r\ntoptext:<color=#68E3F9>乂</color> <color=#8CC1E2>C</color><color=#AF9FCA>H</color><color=#D27DB3>A</color><color=#F55A9B>O</color><color=#CC57AA>S</color><color=#A254B9>乂</color><color=#7951C8>D</color><color=#4F4ED7>E</color><color=#4F4ED7>V</color><color=#7951C8>E</color><color=#A254B9>L</color><color=#CC57AA>O</color><color=#F55A9B>P</color><color=#D27DB3>E</color><color=#AF9FCA>R</color><color=#8CC1E2>乂</color>\r\nname:<color=#07FFC4>乂</color><color=#0DFFB7>P</color><color=#1AFF9D>U</color><color=#27FF84>M</color><color=#34FF6A>P</color><color=#41FF51>K</color><color=#4EFF37>I</color><color=#5BFF1D>N</color><color=#68FF03>G</color><color=#5BFF1D>A</color><color=#4EFF37>M</color><color=#41FF51>I</color><color=#34FF6A>N</color><color=#27FF84>G</color><color=#1AHH9D>55</color><color=#0DFFB7>48</color><color=#07FFC4>✓</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(MAX_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./straypanda#3469.txt"))
                        {
                            File.Move(@"./straypanda#3469.txt", MAX_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(MAX_FILE_PATH, "type:sforce\r\ncode:straypanda#3469\r\ncolor:#E5A8A3\r\ntoptext:<color=#E5A8A3>C</color><color=#E5BBB1>HA</color><color=#D6C2B9>OS </color><color=#C6C9C0>A</color><color=#B6D0C8>D</color><color=#AED4CC>MI</color><color=#AAD6CE>N</color>\r\nname:<color=#E49595>M</color><color=#AAD6CE>A</color><color=#AED4CC>X</color><color=#B6D0C8>T</color><color=#C6C9C0>H</color><color=#D6C2B9>E</color><color=#E5BBB1>M</color><color=#E5A8A3>A</color><color=#A6D7CF>X</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(MEH_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./basketsane#0222.txt"))
                        {
                            File.Move(@"./basketsane#0222.txt", MEH_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(MEH_FILE_PATH, "type:sforce\r\ncode:basketsane#0222\r\ncolor:#F391EE\r\ntoptext:<color=#FF1B6B>CH</color>AO<color=#D14790>S KE</color><color=#A273B5>EP</color><color=#749FDA>E</color><color=#45CAFF>R</color>\r\nname:<color=#45CAFF>M</color><color=#A273B5>E</color><color=#FF1B6B>H</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(SG_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./sakeplumy#6799.txt"))
                        {
                            File.Move(@"./sakeplumy#6799.txt", SG_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(SG_FILE_PATH, "type:sforce\r\ncode:sakeplumy#6799\r\ncolor:#F391EE\r\ntoptext:<color=#FF1B6B>CH</color>AO<color=#D14790>S KE</color><color=#A273B5>EP</color><color=#749FDA>E</color><color=#45CAFF>R</color>\r\nname:<color=#45CAFF>SMA</color><color=#A273B5>LLG</color><color=#FF1B6B>UY</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(SG1_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./shapelyfax#3548.txt"))
                        {
                            File.Move(@"./shapelyfax#3548.txt", SG1_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(SG1_FILE_PATH, "type:sforce\r\ncode:shapelyfax#3548\r\ncolor:#F391EE\r\ntoptext:<color=#FF1B6B>CH</color>AO<color=#D14790>S KE</color><color=#A273B5>EP</color><color=#749FDA>E</color><color=#45CAFF>R</color>\r\nname:<color=#45CAFF>SMA</color><color=#A273B5>LLG</color><color=#FF1B6B>UY</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(HOWTO_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./HOW-TO-MAKE-TAGS.txt"))
                        {
                            File.Move(@"./HOW-TO-MAKE-TAGS.txt", HOWTO_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(HOWTO_FILE_PATH, "1-Change the code to the players friendcode you would like to have a tag as shown below.\r\n2-From google add the Hex code for the color you would like into EXP = <color=ADD HEX CODE HERE>♡</color> . ADD ONE COLOR CODE TO A LETTER FOR GRADIENT TAGS\r\n3-Add the letter or if you would like only 1 color tags add the full work EXP = <color=#3BCFD4>ADD LETTER WORD OR EMOJIS</color> .\r\n4- the tag should look like \r\ntoptext:<color=#3BCFD4>♡</color> <color=#F20094>C</color><color=#ed2e72>HA</color><color=#e86549>O</color><color=#e5892e>S A</color><color=#a8a26a>D</color><color=#75b79c>MI</color><color=#3bcfd4>N</color> <color=#F20094>♡</color>\r\nname:<color=#F20094>♥</color> <color=#3bcfd4>T</color><color=#96b373>i</color><color=#cfa135>m</color><color=#fa7027>m</color><color=#f74b4b>a</color><color=#f20094>y</color> <color=#3BCFD4>♥</color>\r\n5- here is a blank color u can copy and paste into the file called NEW TAG <color=></color>\r\n6-Make sure to rename NEW TAG with the friendcode of the player like  retroozone#9741\r\nTHE FINISHED TAG SHOULD LOOK LIKE \r\ntype:sforce\r\ncode:retroozone#9741\r\ntoptext:<color=#3BCFD4>♡</color> <color=#F20094>C</color><color=#ed2e72>HA</color><color=#e86549>O</color><color=#e5892e>S A</color><color=#a8a26a>D</color><color=#75b79c>MI</color><color=#3bcfd4>N</color> <color=#F20094>♡</color>\r\nname:<color=#F20094>♥</color> <color=#3bcfd4>T</color><color=#96b373>i</color><color=#cfa135>m</color><color=#fa7027>m</color><color=#f74b4b>a</color><color=#f20094>y</color> <color=#3BCFD4>♥</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
                if (!File.Exists(NEWTAG_FILE_PATH))
                {
                    try
                    {
                        if (!Directory.Exists(@"CHAOS")) Directory.CreateDirectory(@"CHAOS");
                        if (File.Exists(@"./NEW-TAGS.txt"))
                        {
                            File.Move(@"./NEW-TAGS.txt", NEWTAG_FILE_PATH);
                        }
                        else
                        {
                            TownOfHost.Logger.Info("No roles.txt file found.", "rolesManager");
                            File.WriteAllText(NEWTAG_FILE_PATH, "type:sforce\r\ncode:FRIENDCODE\r\ntoptext:<color=HEXCODE>ADD LETTER/WORD</color>\r\nname:<color=HEXCODE>ADD LETTER/WORD</color>");
                        }
                    }
                    catch (Exception ex)
                    {
                        TownOfHost.Logger.Exception(ex, "TemplateManager");
                    }
                }
            }

            Harmony.PatchAll();
        }
        private delegate bool DLoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
    }
    public enum CustomRoles
    {
        //Default
        Crewmate = 0,
        //Impostor(Vanilla)
        Impostor,
        Shapeshifter,
        CrewmateGhost,
        ImpostorGhost,
        Morphling,
        Mechanic,
        Physicist,
        Target,
        //Impostor
        BountyHunter,
        Pickpocket,
        FireWorks,
        Mafia,
        SerialKiller,
        Escapist,
        //ShapeMaster,
        Sniper,
        Vampire,
        Vampress,
        Witch,
        Warlock,
        Mare,
        Miner,
        Consort,
        YingYanger,
        Grenadier,
        Disperser,
        Puppeteer,
        Satan,
        // EVENT WINNING ROLES
        IdentityTheft,
        Manipulator,
        AgiTater,
        Bomber,
        Creeper,
        // JK NOW //
        TimeThief,
        Silencer,
        Ninja,
        Swooper,
        Camouflager,
        Freezer,
        Cleaner,
        Assassin,
        LastImpostor,
        //Madmate
        MadGuardian,
        Madmate,
        MadSnitch,
        CrewPostor,
        CorruptedSheriff,
        SKMadmate,
        Parasite,
        // SPECIAL ROLES //
        Cultist,
        Whisperer,
        Chameleon,
        GodFather,
        Mafioso,
        Framer,
        Disguiser,
        // VANILLA
        Engineer,
        GuardianAngel,
        Scientist,
        //Crewmate
        Alturist,
        Lighter,
        Medium,
        // Demolitionist,
        //Bastion,
        NiceGuesser,
        Escort,
        Crusader,
        Psychic,
        Mystic,
        Swapper,
        // Mayor, Demolitionist,Transporter,Doctor,Trapper,
        SabotageMaster,
        Oracle,
        Forecaster,
        Medic,
        Bodyguard,
        Sheriff,
        Investigator,
        Snitch,
        // Transporter,
        SpeedBooster,
        //  Trapper,
        Dictator,
        //  Doctor,
        Paramedic,
        Child,
        //Veteran,
        TimeManager,
        Reviver,
        Chancer,
        //Neutral
        Arsonist,
        Egoist,
        PlagueBearer,
        Pestilence,
        Vulture,
        TheGlitch,
        Postman,
        Werewolf,
        NeutWitch,
        Marksman,
        GuardianAngelTOU,
        Lawyer,
        Jester,
        Amnesiac,
        Hacker,
        PoisonMaster,
        BloodKnight,
        Hitman,
        Phantom,
        Pirate,
        Juggernaut,
        Opportunist,
        Sellout,
        Survivor,
        Terrorist,
        Executioner,
        Jackal,
        Sidekick,
        Dracula,
        Magician,
        Hustler,
        Wizard,
        Kamikaze,
        // ALL CAT ROLES //
        SchrodingerCat,
        JSchrodingerCat,
        CSchrodingerCat,
        MSchrodingerCat,
        EgoSchrodingerCat,
        BKSchrodingerCat,
        CPSchrodingerCat,
        MGSchrodingerCat,
        JugSchrodingerCat,
        MMSchrodingerCat,
        PesSchrodingerCat,
        WWSchrodingerCat,
        TGSchrodingerCat,
        RBSchrodingerCat,
        //HideAndSeek
        HASFox,
        HASTroll,
        //GM
        GM,
        //coven
        Coven,
        Poisoner,
        CovenWitch,
        HexMaster,
        PotionMaster,
        Medusa,
        Mimic,
        Necromancer,
        Conjuror,

        // NEW GAMEMODE ROLES //
        Painter,
        Janitor,
        Supporter,
        Tasker,

        // RANDOM ROLE HELPERS //
        LoversWin,
        // Sub-roles are After 500. Meaning, all roles under this are Modifiers.
        NoSubRoleAssigned = 500,

        // GLOBAL MODIFIERS //
        Lovers,
        LoversRecode,
        Flash, // DONE
        Escalation,
        TieBreaker, // DONE
        Oblivious, // DONE
        Sleuth, // DONE
        Watcher, // DONE
        Obvious,
        DoubleShot,
        Mayor,
        Demolitionist,
        Transporter,
        Doctor,
        Trapper,
        Veteran,
        PUMPkinsPotion,
        Bastion,

        // CREW MODIFIERS //
        Bewilder, // DONE
        Bait, // DONE
        Torch, // DONE
        Diseased,

        // TAG COLORS //
        sns1,
        sns2,
        sns3,
        sns4,
        sns5,
        sns6,
        sns7,
        sns8,
        sns9,
        sns10,
        rosecolor,
        // random //
        thetaa,
        eevee,
        serverbooster,
        // SELF //
        minaa,
        ess,
        // end random //
        psh1,
        psh2,
        psh3,
        psh4,
        psh5,
        psh6,
        psh7,
        psh8,
        psh9,


        //CUSTOM COLORS RAINBOW
        rain1,
        rain2,
        rain3,
        rain4,
        rain5,
        rain6,
        rain7,
        rain8,
        rain9,
        rain10,
        //CUSTOM COLORS BEN
        ben0,
        ben1,
        ben2,
        ben3,
        ben4,
        ben5,
        ben6,
        ben7,
        ben8,
        ben9,
        ben10,
        //CUSTOM COLORS AU01
        AU1,
        AU2,
        AU3,
        AU4,
        //CUSTOM COLORS AU02
        AU11,
        AU22,
        AU33,
        AU44,
        // CUSTOM COLOR DC
        grey1,
        red1,
        purp1,
        or1,
        or2,
        ro1,
        ye1,
        //CUSTOM COLOR GRAD1
        gr1,
        gr2,
        gr3,
        gr4,
        gr5,
        //CUSTOM COLOR GR2
        gr01,
        gr02,
        gr03,
        gr04,
        gr05,
        // CUSTOM COLORS GR 3
        gr11,
        gr22,
        gr33,
        gr44,
        gr55,
        //custom grad 4
        g1,
        g2,
        g3,
        g4,
        g5,
        // custom grad 5
        g01,
        g02,
        g03,
        g04,
        g05,
        //custom grad 6
        bl1,
        bl2,
        bl3,
        bl4,
        // custom ari
        ar1,
        //custom colors normal
        no1,
        no2,
        no3,
        no4,
        no5,
        no6,
        no7,
        //custom color bennie
        bb1,
        bb2,
        bb3,
        bb4,
        bb5,
        bb6,
        bb7,
        bb8,
        bb9,
        bb10,
        //custom color howdy
        hw1,
        hw2,
        hw3,
        hw4,
        hw5,
        hw6,
        hw7,
        hw8,
        hw9,
        hw10,
        //custom color pew pew
        pw1,
        pw2,
        pw3,
        pw4,
        pw5,
        pw6,
        pw7,
        pw8,
        pw9,
        pw10,
        //custom colors ar2
        a6,
        a7,
        a8,
        a9,
        a10,
        //custom colors dark
        d1,
        d2,
        d3,
        d4,
        d5,
        //lime-grey
        w1,
        w2,
        w3,
        w4,
        w5,
        //fifu
        f1,
        f2,
        f3,
        f4,
        //dan
        da1,
        da2,
        da3,
        da4,
        da5,
        //luci 1
        l1,
        l2,
        l3,
        l4,
        l5,
        //heda
        h1,
        h2,
        h3,
        h4,
        h5,
        h6,
        h7,
        h8,
        h9,
        h10,
        //diamond
        di1,
        di2,
        di3,
        di4,
        di5,
        //GOLD
        gd1,
        gd2,
        gd3,
        gd4,
        gd5,
        //layla
        la1,
        la2,
        la3,
        la4,
        la5,
        //knight
        k1,
        k2,
        k3,
        k4,
        k5,
        //namra
        na1,
        na2,
        na3,
        na4,
        na5,
        na6,
        na7,
        na8,
        na9,
        na10,
        //mine2
        m1,
        m2,
        m3,
        m4,
        m5,
        //MAX
        mx1,
        mx2,
        mx3,
        mx4,
        mx5,
        mx6,
        mx7,
        mx8,
        mx9,
        mx10,
        //miskitten
        ms1,
        ms2,
        ms3,
        ms4,
        ms5,
        //rocky
        r1,
        r2,
        r3,
        r4,
        r5,
        //LADYTATER
        lt1,
        lt2,
        lt3,
        lt4,
        lt5,
        //gunbaby
        gb1,
        gb2,
        gb3,
        gb4,
        gb5,
        //my 10 color mix
        q1,
        q2,
        q3,
        q4,
        q5,
        q6,
        q7,
        q8,
        q9,
        q10,
        //LEMON
        le1,
        le2,
        le3,
        le4,
        le5,
        le6,
        le7,
        le8,
        le9,
        le10,
        //PRIYA
        py1,
        py2,
        py3,
        py4,
        py5,
        py6,
        py7,
        py8,
        py9,
        py10,
        //Thetaa
        ta1,
        ta2,
        ta3,
        ta4,
        ta5,
        ta6,
        ta7,
        ta8,
        ta9,
        ta10,
        //nn
        nn1,
        nn2,
        nn3,
        nn4,
        nn5,
        nn6,
        nn7,
        nn8,
        nn9,
        nn10,

        //eevee
        ee1,
        ee2,
        ee3,
        ee4,
        ee5,
        ee6,
        ee7,
        ee8,
        ee9,
        ee10,
        //cinnamoroll
        ci1,
        ci2,
        ci3,
        ci4,
        ci5,
        ci6,
        ci7,
        ci8,
        ci9,
        ci10,
        //smokie
        sm1,
        sm2,
        sm3,
        sm4,
        sm5,
        sm6,
        sm7,
        sm8,
        sm9,
        sm10,
        //MaMa BB
        mm1,
        mm2,
        mm3,
        mm4,
        mm5,
        mm6,
        mm7,
        mm8,
        mm9,
        mm10,
        //2thic
        th1,
        th2,
        th3,
        th4,
        th5,
        th6,
        th7,
        th8,
        th9,
        th10,
        //meh
        mh1,
        mh2,
        mh3,
        mh4,
        mh5,
        mh6,
        mh7,
        mh8,
        mh9,
        mh10,
        //det
        dt1,
        dt2,
        dt3,
        dt4,
        dt5,
        dt6,
        dt7,
        dt8,
        dt9,
        dt10,
        //sleepypie
        sl1,
        sl2,
        sl3,
        sl4,
        sl5,
        sl6,
        sl7,
        sl8,
        sl9,
        sl10,
        //August
        ag1,
        ag2,
        ag3,
        ag4,
        ag5,
        ag6,
        ag7,
        ag8,
        ag9,
        ag10,
        //whited
        dv1,
        dv2,
        dv3,
        dv4,
        dv5,
        dv6,
        dv7,
        dv8,
        dv9,
        dv10,
        //noir
        w6,
        w7,
        w8,
        w9,
        w10,
        //lulu
        al1,
        al2,
        al3,
        al4,
        al5,
        al6,
        al7,
        al8,
        al9,
        al10,
        //gunbaby
        au1,
        au2,
        au3,
        au4,
        au5,
        au6,
        au7,
        au8,
        au9,
        au10,
        //winners points
        sg1,
        sg2,
        sg3,
        sg4,
        sg5,
        sg6,
        sg7,
        sg8,
        sg9,
        sg10,
        //ffa winner
        fw1,
        fw2,
        fw3,
        fw4,
        fw5,
        fw6,
        fw7,
        fw8,
        fw9,
        fw10,
        //toc boosters
        tb1,
        tb2,
        tb3,
        tb4,
        tb5,
        tb6,
        tb7,
        tb8,
        tb9,
        tb10,
        tb11,
        tb12,
        tb13,
        tb14,
        tb15,
        tb16,
        tb17,
        tb18,
        tb19,
        tb20,
        //anonworks
        aw1,
        aw2,
        aw3,
        aw4,
        aw5,
        aw6,
        aw7,
        aw8,
        aw9,
        aw10,
        aw11,
        aw12,
        aw13,
        aw14,
        aw15,
        //mine new
        bn1,
        bn2,
        bn3,
        bn4,
        bn5,
        bn6,
        bn7,
        bn8,
        bn9,
        bn10,
        bn11,
        bn12,
        bn13,
        bn14,
        bn15,
        bn16,
        bn17,
        bn18,
        bn19,
        //yeetus
        yt1,
        yt2,
        yt3,
        yt4,
        yt5,
        yt6,
        yt7,
        yt8,
        yt9,
        yt10,
        yt11,
        yt12,
        yt13,
        yt14,
        yt15,
        //invite winner
        iw1,
        iw2,
        iw3,
        iw4,
        iw5,
        iw6,
        iw7,
        iw8,
        iw9,
        iw10,
        iw11,
        //Moon Gold
        ml1,
        ml2,
        ml3,
        ml4,
        ml5,
        ml6,
        ml7,
        ml8,
        ml9,
        ml10,
        ml11,
        ml12,
        ml13,
        ml14,
        //Lina
        Li1,
        Li2,
        Li3,
        Li4,
        Li5,
        Li6,
        Li7,
        Li8,
        Li9,
        Li10,
        //Cat
        ca1,
        ca2,
        ca3,
        ca4,
        ca5,
        ca6,
        ca7,
        ca8,
        ca9,
        ca10,
        //ary
        ay1,
        ay2,
        ay3,
        ay4,
        ay5,
        ay6,
        ay7,
        ay8,
        ay9,
        ay10,
        //citrion
        ct1,
        ct2,
        ct3,
        ct4,
        ct5,
        ct6,
        ct7,
        ct8,
        ct9,
        ct10,
        //Timmay own 
        ti1,
        ti2,
        ti3,
        ti4,
        ti5,
        ti6,
        ti7,
        ti8,
        ti9,
        ti10,
        ti11,
        ti12,
        ti13,
        ti14,
        ti15,
        ti16,
        ti17,
        //banana
        banana,











    }
    //WinData
    public enum CustomWinner
    {
        Draw = -1,
        Default = -2,
        None = -3,
        Impostor = CustomRoles.Impostor,
        Crewmate = CustomRoles.Crewmate,
        Jester = CustomRoles.Jester,
        Terrorist = CustomRoles.Terrorist,
        Lovers = CustomRoles.LoversWin,
        Child = CustomRoles.Child,
        Executioner = CustomRoles.Executioner,
        Arsonist = CustomRoles.Arsonist,
        Vulture = CustomRoles.Vulture,
        Egoist = CustomRoles.Egoist,
        Pestilence = CustomRoles.Pestilence,
        Jackal = CustomRoles.Jackal,
        Juggernaut = CustomRoles.Juggernaut,
        Swapper = CustomRoles.Swapper,
        HASTroll = CustomRoles.HASTroll,
        Phantom = CustomRoles.Phantom,
        Coven = CustomRoles.Coven,
        TheGlitch = CustomRoles.TheGlitch,
        Werewolf = CustomRoles.Werewolf,
        Hacker = CustomRoles.Hacker,
        BloodKnight = CustomRoles.BloodKnight,
        Pirate = CustomRoles.Pirate,
        Marksman = CustomRoles.Marksman,
        Painter = CustomRoles.Painter,
        AgiTater = CustomRoles.AgiTater,
        Tasker = CustomRoles.Tasker,
        Postman = CustomRoles.Postman,
        Dracula = CustomRoles.Dracula,
        Hustler = CustomRoles.Hustler,
        Magician = CustomRoles.Magician,
        DisconnectError = -3,


    }
    public enum AdditionalWinners
    {
        None = -1,
        Opportunist = CustomRoles.Opportunist,
        Survivor = CustomRoles.Survivor,
        SchrodingerCat = CustomRoles.SchrodingerCat,
        Executioner = CustomRoles.Executioner,
        HASFox = CustomRoles.HASFox,
        GuardianAngelTOU = CustomRoles.GuardianAngelTOU,
        Lawyer = CustomRoles.Lawyer,
        Hitman = CustomRoles.Hitman,
        Witch = CustomRoles.NeutWitch,
        sellout = CustomRoles.Sellout,
        chancer = CustomRoles.Chancer,
        demon = CustomRoles.Satan,
    }
    /*public enum CustomRoles : byte
    {
        Default = 0,
        HASTroll = 1,
        HASHox = 2
    }*/
    [HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
    class GetBroadcastVersionPatch
    {
        static void Postfix(ref int __result)
        
        {
            if (AmongUsClient.Instance.NetworkMode is NetworkModes.LocalGame or NetworkModes.FreePlay) return;
            __result += 25;
          
        }
    }
    

    [HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
    public static class IsVersionModdedPatch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
    public enum SuffixModes
    {
        None = 0,
        TOH,
        Streaming,
        Recording,
        Dev
    }
    public enum VersionTypes
    {
        Released = 0,
        Beta = 1
    }

    public enum VoteMode
    {
        Default,
        Suicide,
        SelfVote,
        Skip
    }

    // ATTACK AND DEFENSE
    public enum AttackEnum
    {
        None = 0,
        Basic,
        Powerful,
        Unstoppable,
        Unblockable
    }
    public enum DefenseEnum
    {
        None = 0,
        Basic,
        Powerful,
        Invincible
    }
}
