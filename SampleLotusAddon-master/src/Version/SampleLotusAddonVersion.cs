using Hazel;

namespace SampleRoleAddon.Version;


/// <summary>
/// Version Representing this Addon
/// </summary>
public class SampleLotusAddonVersion: VentLib.Version.Version
{
    public override VentLib.Version.Version Read(MessageReader reader)
    {
        return new SampleLotusAddonVersion();
    }

    protected override void WriteInfo(MessageWriter writer)
    {
    }

    public override string ToSimpleName()
    {
        return "Sample Lotus Addon Version v1.2.3";
    }

    public override string ToString() => "SampleLotusAddonVersion";
}