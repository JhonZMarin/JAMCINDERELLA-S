using UnityEngine;
using UnityEngine.SceneManagement; //  necesario para cambiar de escena

public class ChangeScene : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
