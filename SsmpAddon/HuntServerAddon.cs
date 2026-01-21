using SSMP.Api.Server;
using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntServerAddon : ServerAddon
{
    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;

    protected override string Name => "TheHuntIsOn";

    protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public override void Initialize(IServerApi serverApi)
    {
        // FIXME
    }
}
