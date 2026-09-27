using UnityEngine;
using UnityEngine.EventSystems;

/// Actividad 1/2 - Botón de navegación que se mantiene presionado (izquierda / derecha).
/// Adjuntar al botón UI. direction: -1 = izquierda, 1 = derecha.
public class MoveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public PlayerController player;
    public int direction = 1;

    public void OnPointerDown(PointerEventData e) => player.SetMove(direction);
    public void OnPointerUp(PointerEventData e) => player.SetMove(0);
    public void OnPointerExit(PointerEventData e) => player.SetMove(0);
}
