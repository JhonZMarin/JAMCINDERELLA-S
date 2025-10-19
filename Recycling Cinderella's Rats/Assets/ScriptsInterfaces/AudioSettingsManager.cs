using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    public Slider volumeSlider;
    private const string volumeKey = "gameVolume";

    void Start()
    {
        // Cargar volumen guardado
        if (PlayerPrefs.HasKey(volumeKey))
        {
            float savedVolume = PlayerPrefs.GetFloat(volumeKey);
            AudioListener.volume = savedVolume;
            volumeSlider.value = savedVolume;
        }
        else
        {
            AudioListener.volume = 1f;
            volumeSlider.value = 1f;
        }

        // Escuchar cambios en tiempo real
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
    }

    public void ChangeVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat(volumeKey, volumeSlider.value);
        PlayerPrefs.Save();
        Debug.Log("Volumen guardado: " + volumeSlider.value);
    }
}
