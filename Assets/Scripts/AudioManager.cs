using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    //[Header("Audio Sources")]
    //public AudioSource musicSource;
    //public AudioSource sfxSource;

    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        //musicSlider.value = musicSource.volume;
        //sfxSlider.value = sfxSource.volume;

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        //musicSource.volume = value;
    }

    public void SetSFXVolume(float value)
    {
        //sfxSource.volume = value;
    }
}