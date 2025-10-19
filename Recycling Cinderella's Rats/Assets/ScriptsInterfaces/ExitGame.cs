using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; //  necesario para cerrar el editor
#endif

public class ExitGame : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        // Si estás en el editor, detiene el modo Play
        EditorApplication.isPlaying = false;
#else
        // Si estás en el juego compilado, cierra la aplicación
        Application.Quit();
#endif
    }
}
