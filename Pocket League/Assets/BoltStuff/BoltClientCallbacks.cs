using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour(BoltNetworkModes.Client, "Test")]
public class BoltClientCallbacks : Bolt.GlobalEventListener
{
    public override void SceneLoadLocalDone(string map)
    {
        BoltEntity character = BoltNetwork.Instantiate(BoltPrefabs.PocketPlayer, new Vector3(0, 0, 0), Quaternion.identity);
        character.TakeControl();
    }
}
