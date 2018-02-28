using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtboxLogic : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Wall")
        {
            PocketPlayerController pocketController = transform.parent.gameObject.GetComponent<PocketPlayerController>();
            pocketController.trajectory = Vector3.Reflect(pocketController.trajectory, other.transform.position.normalized);
        }
    }
}
