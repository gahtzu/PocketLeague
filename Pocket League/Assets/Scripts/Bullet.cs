using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector]
    public Vector3 dir;
    [HideInInspector]
    public float speed;
    [HideInInspector]
    public GameObject playerOwner;
    [HideInInspector]
    public int framesProjectileIsAlive;

    private int framesSinceBirth = 0;

    private void LateUpdate()
    {
        transform.Translate(dir * speed, Space.World);
        framesSinceBirth++;
        if (framesSinceBirth >= framesProjectileIsAlive)
            Destroy(gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Hurtbox" && other.transform.parent != playerOwner.transform)
        {
            other.transform.parent.GetComponent<PocketPlayerController>().currProjectileDir = dir;
            other.transform.parent.GetComponent<PocketPlayerController>().stateMachine.ChangeState(PlayerState.BulletHitstun);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Wall")
            dir = Vector3.Reflect(dir, collider.transform.position.normalized);
    }

}
