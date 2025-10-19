using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Asignar el volumen global guardado
        audioSource.volume = PlayerPrefs.GetFloat("gameVolume", 1f);

        // Reproducir la música
        audioSource.Play();
    }
}
