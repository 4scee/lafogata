using UnityEngine;

/// Actividad 2/4 - Zona de destino del arrastre (la fogata).
/// Si sueltas un tronco (DragSprite) dentro del collider, la fogata crece y suma puntos.
/// Adjuntar a la fogata. Collider2D con "Is Trigger" activado.
[RequireComponent(typeof(Collider2D))]
public class FireZone : MonoBehaviour
{
    public float growPerLog = 0.25f;     // cuánto crece la fogata por tronco

    Collider2D zone;

    void Awake() => zone = GetComponent<Collider2D>();
    void OnEnable() => DragSprite.Dropped += OnDropped;
    void OnDisable() => DragSprite.Dropped -= OnDropped;

    void OnDropped(DragSprite log)
    {
        if (zone.OverlapPoint(log.transform.position))
        {
            log.gameObject.SetActive(false);                    // cambia el estado del tronco
            transform.localScale += Vector3.one * growPerLog;   // cambia el estado de la fogata
            if (GameManager.Instance != null) GameManager.Instance.AddWood();
        }
        else
        {
            log.ResetPosition();                                // no cayó en la fogata: regresa
        }
    }
}
