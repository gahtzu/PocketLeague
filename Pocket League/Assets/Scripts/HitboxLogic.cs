using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxLogic : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Hurtbox" && other.transform.parent != transform.parent)
        {
            other.transform.parent.GetComponent<PocketPlayerController>().stateMachine.ChangeState(PlayerState.Hitstun);
            transform.parent.GetComponent<PocketPlayerController>().HitOpponent();
        }
    }
}
