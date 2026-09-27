using UnityEngine;

/// Actividad 3 - Bucle de animación por código (alternativa al Animation window).
/// Arrastra 8 sprites al arreglo "frames". Útil para fogata, agua, estrellas, etc.
[RequireComponent(typeof(SpriteRenderer))]
public class FrameLoop : MonoBehaviour
{
    public Sprite[] frames;          // mínimo 8
    public float fps = 10f;
    public bool randomStart = false; // desfasa varias copias para que no se vean sincronizadas

    SpriteRenderer sr;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (randomStart && frames.Length > 0) t = Random.value * frames.Length / fps;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.deltaTime;
        sr.sprite = frames[(int)(t * fps) % frames.Length];
    }
}
