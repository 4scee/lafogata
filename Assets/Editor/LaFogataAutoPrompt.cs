using System.IO;
using UnityEditor;

/// Al abrir el proyecto, si todavía no existen las escenas, ofrece construirlas.
[InitializeOnLoad]
static class LaFogataAutoPrompt
{
    const string Key = "LaFogata_PromptShown";

    static LaFogataAutoPrompt()
    {
        EditorApplication.delayCall += Check;
    }

    static void Check()
    {
        if (SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, true);
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists("Assets/Scenes/MainMenu.unity")) return;

        if (EditorUtility.DisplayDialog("La Fogata",
            "Todavía no se han creado las escenas del juego.\n\n¿Construir el proyecto ahora?",
            "Construir ahora", "Más tarde"))
        {
            LaFogataBuilder.BuildAll();
        }
    }
}
