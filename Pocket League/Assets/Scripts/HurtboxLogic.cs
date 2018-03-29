using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class HurtboxLogic : MonoBehaviour
{
    private MasterLogic masterLogic;

    void Start()
    {
        masterLogic = FindObjectOfType<MasterLogic>();
    }

    private void OnTriggerEnter(Collider other)
    {
		if (other.tag == "Wall") {
			PocketPlayerController pocketController = transform.parent.gameObject.GetComponent<PocketPlayerController> ();
			pocketController.trajectory = Vector3.Reflect (pocketController.trajectory, other.transform.position.normalized);
		} else if (other.tag == "Hole") {
            masterLogic.LoseStock(transform.parent.GetComponent<PocketPlayerController>());
		}
    }
}
