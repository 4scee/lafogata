using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// Actividad 2 - Arrastrar un Sprite (mouse o dedo).
/// Requisitos en la escena:
///  - Un EventSystem (GameObject > UI > Event System).
///  - Physics2DRaycaster en la Main Camera (Add Component).
///  - El sprite necesita un Collider2D (sin Rigidbody dinámico).
[RequireComponent(typeof(Collider2D))]
public class DragSprite : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    /// Se dispara al soltar el sprite. Las zonas de destino (FireZone) se suscriben.
    public static event Action<DragSprite> Dropped;

    Camera cam;
    Vector3 startPos;
    Vector3 grabOffset;

    void Awake()
    {
        cam = Camera.main;               // la cámara debe tener el tag MainCamera
        startPos = transform.position;
    }

    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        float depth = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        w.z = transform.position.z;
        return w;
    }

    public void OnPointerDown(PointerEventData e) => grabOffset = transform.position - ScreenToWorld(e.position);

    public void OnDrag(PointerEventData e) => transform.position = ScreenToWorld(e.position) + grabOffset;

    public void OnPointerUp(PointerEventData e) => Dropped?.Invoke(this);

    public void ResetPosition() => transform.position = startPos;
}
