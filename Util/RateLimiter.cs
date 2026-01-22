using UnityEngine;

namespace Silksong.TheHuntIsOn.Util;

internal class RateLimiter(float limit)
{
    private float cooldown = 0;

    internal bool Check() => cooldown <= 0;

    internal void Update(float? time = null)
    {
        if (cooldown <= 0) return;

        cooldown -= time ?? Time.deltaTime;
        if (cooldown < 0) cooldown = 0;
    }

    internal void Reset() => cooldown = limit;
}
