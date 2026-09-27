using UnityEngine;
using UnityEngine.SceneManagement;

/// Actividad 2 y 5 - Navegación entre escenas.
/// Adjuntar a un objeto vacío (p. ej. "Navigator") y enlazar los métodos
/// en el evento OnClick() de cada botón.
public class SceneNavigator : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;   // por si se sale desde la pausa
        SceneManager.LoadScene(sceneName);
    }

    public void LoadNext()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings) next = 0;
        SceneManager.LoadScene(next);
    }

    public void ReloadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
