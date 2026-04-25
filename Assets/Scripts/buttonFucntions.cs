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
    public void playerSpawn()
    {
        gamemanager.instance.RespawnPlayer();
    }

    public void openAudio()
    {
        gamemanager.instance.openAudio();
    }

    public void openControl()
    {
        gamemanager.instance.openControl();
    }

    public void openCredits()
    {
        gamemanager.instance.openCredits();
    }

    public void backToStart()
    {
        gamemanager.instance.backToStart();
    }
    public void backToPause()
    {
        gamemanager.instance.backToPause();
    }
}