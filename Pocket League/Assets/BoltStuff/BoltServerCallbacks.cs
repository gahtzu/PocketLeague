using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour(BoltNetworkModes.Server, "Test")]
public class BoltServerCallbacks : Bolt.GlobalEventListener
{
    void Awake()
    {
        BoltPocketPlayerRegistry.CreateServerPlayer();
    }

    public override void Connected(BoltConnection connection)
    {
        BoltPocketPlayerRegistry.CreateClientPlayer(connection);
    }

    public override void SceneLoadLocalDone(string map)
    {
        BoltPocketPlayer pl = BoltPocketPlayerRegistry.ServerPlayer;
        pl.Spawn();
    }

    public override void SceneLoadRemoteDone(BoltConnection connection)
    {
        BoltPocketPlayer pl = BoltPocketPlayerRegistry.GetPocketPlayer(connection);
        pl.Spawn();

        if (BoltPocketPlayerRegistry.numPlayers >= 2)
        {
            var ev = StartPocketLeagueGame.Create();
            ev.Send();
        }
    }
}
