using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class gamemanager : MonoBehaviour
{

    public static gamemanager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuStart;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject menuAudio;

    public Image playerHPbar;
    public GameObject player;
    public PlayerMovement playerScript;
    public GameObject playerSpawnPos;
    public GameObject checkpointPopup;
    public bool isPaused;

    private float timeScaleOrig;

    private int gameGoalCount;

    public static bool skipStartMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;

        timeScaleOrig = Time.timeScale;

        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<PlayerMovement>();

        if (!skipStartMenu)
        {
            StartGame();
        }
        else
        {
            skipStartMenu = false;
            stateUnpause();
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetButtonDown("Cancel"))
        {
            if (isPaused)
                stateUnpause();
            else
            {
                statePause();
                menuActive = menuPause;
                menuActive.SetActive(true);
            }
        }
    }
     public void StartGame()
     {
       isPaused = true;
       Time.timeScale = 0;
       Cursor.visible = true;
       Cursor.lockState = CursorLockMode.None;

       menuActive = menuStart;
       menuStart.SetActive(true);
     }


    public void statePause()
    {
        isPaused = true;
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void stateUnpause()
    {
        isPaused = false;
        Time.timeScale = timeScaleOrig;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (menuActive != null)
            menuActive.SetActive(false);

        menuActive = null;
    }


    public void updateGameGoal(int amount)
    {
        gameGoalCount += amount;

        if (gameGoalCount <= 0)
        {
            statePause();
            menuActive = menuWin;
            menuActive.SetActive(true);

        }
    }
    public void youLose()
    {
        statePause();
        menuActive = menuLose;
        menuActive.SetActive(true);
    }

    public void openAudio()
    {
        if (menuActive != null)
            menuActive.SetActive(false);

        menuActive = menuAudio;
        menuAudio.SetActive(true);
    }

    public void backToPause()
    {
        menuAudio.SetActive(false);

        menuActive = menuPause;
        menuPause.SetActive(true);
    }

}