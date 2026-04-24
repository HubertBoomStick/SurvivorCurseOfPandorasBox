using UnityEngine;

public class DragonPhaseTwoPushbackTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float pushForce = 12f;
    [SerializeField] private float upwardForce = 1.5f;
    [SerializeField] private bool onlyPushOnce = true;

    private bool hasPushed;

    private void OnEnable()
    {
        hasPushed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onlyPushOnce && hasPushed)
            return;

        if (((1 << other.gameObject.layer) & playerLayer) == 0)
            return;

        Rigidbody playerRb = other.attachedRigidbody;

        if (playerRb == null)
            playerRb = other.GetComponentInParent<Rigidbody>();

        if (playerRb == null)
            return;

        Vector3 pushDir = other.transform.position - transform.position;
        pushDir.y = 0f;

        if (pushDir.sqrMagnitude <= 0.001f)
            pushDir = -transform.forward;

        pushDir.Normalize();

        Vector3 finalForce = (pushDir * pushForce) + (Vector3.up * upwardForce);
        playerRb.AddForce(finalForce, ForceMode.VelocityChange);

        hasPushed = true;
    }
}