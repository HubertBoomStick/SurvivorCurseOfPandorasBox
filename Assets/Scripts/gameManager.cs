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
    [SerializeField] GameObject menuControl;


    public Image playerHPbar;
    public Image lifeStealBar;
    public GameObject player;
    public PlayerMovement playerScript;
    public GameObject checkpointPopup;
    public bool isPaused;

    public Vector3 playerSpawnPos;

    private float timeScaleOrig;

    private int gameGoalCount;

    public static bool skipStartMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerSpawnPos = player.transform.position;
    }
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

    public void openControl()
    {
        if (menuActive != null)
            menuActive.SetActive(false);

        menuActive = menuControl;
        menuControl.SetActive(true);
    }

    public void backToPause()
    {
        if (menuActive != null)
            menuActive.SetActive(false);

        menuActive = menuPause;
        menuPause.SetActive(true);
    }

    public void updateLifeStealUI(int current, int max)
    {
        lifeStealBar.fillAmount = (float)current / max;
    }

    public void RespawnPlayer()
    {
        player.transform.position = playerSpawnPos;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerScript.ResetHealth();
        playerScript.ResetLifeSteal();

        stateUnpause();
    }
}