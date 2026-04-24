using UnityEngine;
using System.Collections;

public class checkPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gamemanager.instance.playerSpawnPos = transform.position;
            StartCoroutine(showPopup());
        }
    }

    IEnumerator showPopup()
    {
        gamemanager.instance.checkpointPopup.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        gamemanager.instance.checkpointPopup.SetActive(false);
    }
}
