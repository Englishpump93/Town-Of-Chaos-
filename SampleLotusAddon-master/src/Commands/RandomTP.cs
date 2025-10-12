using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Utilities;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;

[Command(CommandFlag.InGameOnly,"rtp")]
public class RandomTP : ICommandReceiver
{
    public void Receive(PlayerControl source, CommandContext context)
    {
        if (source.IsAlive()) return;
        Utils.Teleport(source.NetTransform, Game.RandomSpawn.GetRandomLocation());
    }
}