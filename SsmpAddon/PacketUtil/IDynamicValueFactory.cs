using System;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

internal interface IDynamicValueFactory<E, T, F> where E : Enum where T : IDynamicValue<E, T, F> where F : IDynamicValueFactory<E, T, F>, new()
{
    T Create(E type);
}
