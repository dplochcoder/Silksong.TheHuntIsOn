using SSMP.Api.Client;
using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntClientAddon : ClientAddon
{
    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;

    protected override string Name => "TheHuntIsOn";

    protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public override void Initialize(IClientApi clientApi)
    {
        // FIXME
    }
}
