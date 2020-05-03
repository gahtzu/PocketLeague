using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bolt;

[BoltGlobalBehaviour]
public class BoltNetworkCallbacks : GlobalEventListener
{
    int i;
    public override void OnEvent(StartPocketLeagueGame evnt)
    {
        MasterLogic ml = GameObject.Find("MasterLogic").GetComponent<MasterLogic>();
        ml.StartGame();
    }

    public override void EntityAttached(BoltEntity entity)
    {
        entity.gameObject.name = "Player " + ++i;
    }
}