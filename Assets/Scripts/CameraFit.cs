using UnityEngine;

/// Actividad 1 - Pantalla para diferentes dispositivos.
/// Ajusta el tamaño ortográfico de la cámara para que SIEMPRE se vea toda el área
/// de diseño (designSize, en unidades de Unity), sin importar la relación de aspecto.
/// Adjuntar a la Main Camera.
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [Tooltip("Área del nivel que siempre debe verse (unidades de Unity). 16x9 = 1920x1080 con 120 px por unidad.")]
    public Vector2 designSize = new Vector2(16f, 9f);

    Camera cam;
    int lastW, lastH;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        Apply();
    }

    void Update()
    {
        // Se recalcula si cambia la ventana (rotación del móvil, redimensionar en PC).
        if (Screen.width != lastW || Screen.height != lastH) Apply();
    }

    void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        float target = designSize.x / designSize.y;
        float screen = (float)Screen.width / Screen.height;

        cam.orthographicSize = screen >= target
            ? designSize.y / 2f                    // pantalla más ancha: manda el alto
            : designSize.x / screen / 2f;          // pantalla más angosta: manda el ancho
    }
}
