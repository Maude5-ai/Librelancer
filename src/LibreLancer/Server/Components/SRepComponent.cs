using System.Collections.Generic;
using LibreLancer.Data.GameData;
using LibreLancer.Net;
using LibreLancer.World;

namespace LibreLancer.Server.Components;

// Base component for determining reputations between objects server-side
public class SRepComponent : GameComponent
{
    public Faction? Faction;

    public Dictionary<GameObject, RepAttitude> forcedReps = new();

    public void SetAttitude(GameObject go, RepAttitude a)
    {
        forcedReps[go] = a;
        if (go.TryGetComponent<SPlayerComponent>(out var p))
        {
            p.Player.RpcClient.UpdateAttitude(new ObjNetId(Parent.NetID), a);
        }
    }

    private static RepAttitude FromNumber(float a)
    {
        if (a <= Faction.HostileThreshold)
            return RepAttitude.Hostile;
        if (a >= Faction.FriendlyThreshold)
            return RepAttitude.Friendly;
        return 0;
    }

    public RepAttitude GetRep(GameObject other)
    {
        if (forcedReps.TryGetValue(other, out var f))
            return f;
        if (Faction == null)
            return RepAttitude.Neutral;
        if (other.TryGetComponent<SPlayerComponent>(out var sp))
        {
            // Character goes null when the player disconnects; NPC spawn/rep
            // queries can still see this object mid-teardown (disconnect race,
            // crash-evidence-419 round 5/6/8 stacks)
            var character = sp.Player.Character;
            if (character == null)
                return RepAttitude.Neutral;
            return FromNumber(character.Reputation.GetReputation(Faction));
        }
        if (other.TryGetComponent<SRepComponent>(out var r))
        {
            if (r.Faction != null)
                return FromNumber(Faction.GetReputation(r.Faction));
        }
        return RepAttitude.Neutral;
    }

    public bool IsHostileTo(GameObject other) => GetRep(other) == RepAttitude.Hostile;

    public SRepComponent(GameObject parent) : base(parent)
    {
    }
}
