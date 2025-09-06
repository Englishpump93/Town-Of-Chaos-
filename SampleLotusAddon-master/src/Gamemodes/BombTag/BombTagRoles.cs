using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lotus.GameModes;
using Lotus.Roles;
using SampleRoleAddon.Roles.BombTag;

namespace SampleRoleAddon.Gamemodes.BombTag;

// A RoleHolder holds role instances for your gamemode.
// Just add them all to a list, or you can separate them like how Standard Gamemode does.
// It's IMPORTANT that you call Solidify for every single role. This generates the options and does a lot of other things.
// So, removing it might cause some issues.
public class BombTagRoles: RoleHolder
{
    public override List<Action> FinishedCallbacks() => Callbacks;
    public static List<Action> Callbacks { get; set; } = new List<Action>();
    
    public readonly StaticRoles Static;
    
    public static BombTagRoles Instance = null!;

    public BombTagRoles()
    {
        Instance = this;

        MainRoles = new List<CustomRole>();
        AllRoles = new List<CustomRole>();
        
        Static = new StaticRoles();

        MainRoles = Static.GetType()
            .GetFields()
            .Select(f => (CustomRole)f.GetValue(Static)!)
            .ToList();

        List<CustomRole> realAllRoleList = MainRoles;
        AllRoles = realAllRoleList;
        AllRoles.ForEach(r => r.Solidify());
    }

    public class StaticRoles
    {
        public readonly HasBomb HasBomb = new();
        public readonly HasNoBomb HasNoBomb = new();
    }   
}