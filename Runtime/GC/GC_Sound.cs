using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GC_Sound : MonoBehaviour {

    public static GC_Sound instance;

    [Header("Audio")]
    public Button AudioButton;
    public GameObject AudioContainer;

    private bool showAudioContainer = false;

    [Header("Music")]
    public Button MusicMuteButton;
    public Slider MusicVolumeSlider;
    public Sprite MusicMuteSprite;
	public Sprite MusicUnMuteSprite;

	private AudioSource musicAudioSource;

    [Header("SFX")]
    public Button SFXMuteButton;
    public Slider SFXVolumeSlider;
    public Sprite SFXMuteSprite;
    public Sprite SFXUnMuteSprite;

    private List<AudioSource> sfxAudioSources = new List<AudioSource>();

   

	void Awake() {

        instance = this;
        musicAudioSource = gameObject.GetComponent<AudioSource> ();
	}

	void Start () {

        if (PlayerPrefs.HasKey("GameCenterMusic"))
        {
            if (PlayerPrefs.GetString("GameCenterMusic") == "On")
            {
                MusicMuteButton.GetComponent<Image>().sprite = MusicUnMuteSprite;
                musicAudioSource.mute = false;
            }
            else
            {
                MusicMuteButton.GetComponent<Image>().sprite = MusicMuteSprite;
                musicAudioSource.mute = true;
            }
        }
        else
        {
            PlayerPrefs.SetString("GameCenterMusic", "On");
        }

        if (PlayerPrefs.HasKey("GameCenterMusicVolume"))
        {
            musicAudioSource.volume = PlayerPrefs.GetFloat("GameCenterMusicVolume");
            MusicVolumeSlider.value = PlayerPrefs.GetFloat("GameCenterMusicVolume");
        }
        else
        {
            PlayerPrefs.SetFloat("GameCenterMusicVolume", 1);
        }

        if (PlayerPrefs.HasKey("GameCenterSFX"))
        {
            if (PlayerPrefs.GetString("GameCenterSFX") == "On")
            {
                SFXMuteButton.GetComponent<Image>().sprite = SFXUnMuteSprite;
            }
            else
            {
                SFXMuteButton.GetComponent<Image>().sprite = SFXMuteSprite;
            }
        }
        else
        {
            PlayerPrefs.SetString("GameCenterSFX", "On");
        }

        if (PlayerPrefs.HasKey("GameCenterSFXVolume"))
        {
            SFXVolumeSlider.value = PlayerPrefs.GetFloat("GameCenterSFXVolume");
        }
        else
        {
            PlayerPrefs.SetFloat("GameCenterSFXVolume", 1);
        }

        AudioButton.onClick.AddListener(ToggleAudioContainer);
        MusicMuteButton.onClick.AddListener(MuteMusic);
        MusicVolumeSlider.onValueChanged.AddListener(delegate { ChangeMusicVolume(); });
        SFXMuteButton.onClick.AddListener(MuteSFX);
        SFXVolumeSlider.onValueChanged.AddListener(delegate { ChangeSFXVolume(); });
        AudioContainer.SetActive(false);

        musicAudioSource.loop = true;
        musicAudioSource.Play();

    }

    public static void PlaySound(AudioClip soundClip)
    {
        if (PlayerPrefs.GetString("GameCenterSFX") == "Off")
        {
            return;
        }

        AudioSource targetAudioSource = null;

        foreach (var item in instance.sfxAudioSources)
        {
            if(!item.isPlaying)
            {
                targetAudioSource = item;
            }
        }

        if(targetAudioSource == null)
        {
            GameObject newObject = new GameObject("Sound");
            newObject.transform.SetParent(instance.transform);
            newObject.transform.localPosition = Vector3.zero;
            targetAudioSource = newObject.AddComponent<AudioSource>();
            instance.sfxAudioSources.Add(targetAudioSource);
        }


        targetAudioSource.clip = soundClip;
        targetAudioSource.volume = PlayerPrefs.GetFloat("GameCenterSFXVolume");
        targetAudioSource.Play();
    }    

    public void ToggleAudioContainer()
    {
        if (!showAudioContainer)
        {
            AudioContainer.SetActive(true);
            showAudioContainer = true;
        }
        else
        {
            AudioContainer.SetActive(false);
            showAudioContainer = false;
        }

    }

    private void MuteMusic()
    {
        if (PlayerPrefs.GetString("GameCenterMusic") == "On")
        {
            PlayerPrefs.SetString("GameCenterMusic", "Off");
            MusicMuteButton.GetComponent<Image>().sprite = MusicMuteSprite;
            musicAudioSource.mute = true;
        }
        else
        {
            PlayerPrefs.SetString("GameCenterMusic", "On");
            MusicMuteButton.GetComponent<Image>().sprite = MusicUnMuteSprite;
            musicAudioSource.mute = false;
        }
    }

    void ChangeMusicVolume()
    {
        PlayerPrefs.SetFloat("GameCenterMusicVolume", MusicVolumeSlider.value);

        if (MusicVolumeSlider.value == 0)
        {
            PlayerPrefs.SetString("GameCenterMusic", "Off");
            MusicMuteButton.GetComponent<Image>().sprite = MusicMuteSprite;
            musicAudioSource.mute = true;
        }
        else
        {
            PlayerPrefs.SetString("GameCenterMusic", "On");
            MusicMuteButton.GetComponent<Image>().sprite = MusicUnMuteSprite;
            musicAudioSource.mute = false;
        }

        musicAudioSource.volume = MusicVolumeSlider.value;
    }

    private void MuteSFX()
    {
        if (PlayerPrefs.GetString("GameCenterSFX") == "On")
        {
            PlayerPrefs.SetString("GameCenterSFX", "Off");
            SFXMuteButton.GetComponent<Image>().sprite = SFXMuteSprite;
        }
        else
        {
            PlayerPrefs.SetString("GameCenterSFX", "On");
            SFXMuteButton.GetComponent<Image>().sprite = SFXUnMuteSprite;
        }
    }

    void ChangeSFXVolume()
    {
        PlayerPrefs.SetFloat("GameCenterSFXVolume", SFXVolumeSlider.value);

        if (SFXVolumeSlider.value == 0)
        {
            PlayerPrefs.SetString("GameCenterSFX", "Off");
            SFXMuteButton.GetComponent<Image>().sprite = SFXMuteSprite;
        }
        else
        {
            PlayerPrefs.SetString("GameCenterSFX", "On");
            SFXMuteButton.GetComponent<Image>().sprite = SFXUnMuteSprite;
        }
    }
}

