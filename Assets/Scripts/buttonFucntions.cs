using UnityEngine;
using UnityEngine.SceneManagement;


public class buttonFunctions : MonoBehaviour
{
    public void resume()
    {
        gamemanager.instance.stateUnpause();
    }

    public void restart()
    {
        gamemanager.skipStartMenu = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void quit()
    {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
                Application.Quit();
    #endif
    }

    // for when we do the checkpoints

    //public void playerspawn()
    //{
    //    gamemanager.instance.playerScript.spawnPlayer();
    //    gamemanager.instance.stateUnpause();
    //}
}