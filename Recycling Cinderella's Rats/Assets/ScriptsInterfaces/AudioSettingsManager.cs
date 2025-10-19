using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    public Slider volumeSlider;
    private const string volumeKey = "gameVolume";

    private float savedVolume;  // volumen guardado real
    private float tempVolume;   // volumen temporal para cambios sin guardar

    void Start()
    {
        // Cargar volumen guardado (o por defecto)
        savedVolume = PlayerPrefs.GetFloat(volumeKey, 1f);
        tempVolume = savedVolume;

        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;

        // Escucha los cambios en el slider
        volumeSlider.onValueChanged.AddListener(OnVolumeChange);
    }

    // Cuando mueves el slider
    void OnVolumeChange(float value)
    {
        tempVolume = value;
        AudioListener.volume = value; // se escucha el cambio, pero no se guarda
    }

    // Cuando presionas "Guardar"
    public void SaveVolume()
    {
        savedVolume = tempVolume;
        PlayerPrefs.SetFloat(volumeKey, savedVolume);
        PlayerPrefs.Save();
        Debug.Log("Volumen guardado: " + savedVolume);
    }

    // Cuando sales sin guardar (por ejemplo al presionar "Volver")
    public void RevertVolume()
    {
        tempVolume = savedVolume;
        AudioListener.volume = savedVolume;
        volumeSlider.value = savedVolume;
        Debug.Log("Volumen revertido al guardado: " + savedVolume);
    }
}
