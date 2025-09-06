using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Victory.Conditions;
using SampleRoleAddon.Roles.BombTag;

namespace SampleRoleAddon.Gamemodes.BombTag.WinCons;

public class BombTagWinCondition: IWinCondition
{
    private readonly WinReason soloWin = new(ReasonType.SoloWinner, "They were the last person standing with a bomb.");
    private readonly WinReason bombWin = new(ReasonType.GameModeSpecificWin, "All alive players had bombs.");
    
    private WinReason currentWinCon;
    
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        bool singleAlivePlayer = Players.GetAlivePlayers().Count() == 1;
        bool allAlivePlayersAreBombs = Players.GetAlivePlayers().All(p => p.PrimaryRole() is HasBomb);
        
        if (singleAlivePlayer) {
            winners = Players.GetAlivePlayers().ToList();
            currentWinCon = soloWin;
            return true;
        } else if (allAlivePlayersAreBombs) {
            winners = Players.GetAlivePlayers().ToList();
            currentWinCon = bombWin;
            return true;
        } else {
            winners = null;
            return false;
        }
    }

    public WinReason GetWinReason() => currentWinCon;
}