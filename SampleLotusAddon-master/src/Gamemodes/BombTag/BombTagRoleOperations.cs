using Lotus.GameModes;
using Lotus.GameModes.Standard;

namespace SampleRoleAddon.Gamemodes.BombTag;

// Role Operations are very complicated, so I recommend just making a subclass of the Standard one.
// The standard one does everything you need it to and will handle things just fine.
// However, you can rewrite the code if you want more control over which role actions run when.
// It's pretty much just up to whether or not you feel like taking the time to do that.
public class BombTagRoleOperations: StandardRoleOperations
{
    public BombTagRoleOperations(GameMode parentGamemode): base(parentGamemode)
    {
        
    }
}