using UnityEngine;

/// Actividad 4 - Colisión que cambia el estado del objeto:
/// si el jugador toca este objeto (agua, pinchos), reaparece en su punto inicial.
public class Hazard : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D c) => Handle(c.collider);
    void OnTriggerEnter2D(Collider2D o) => Handle(o);

    void Handle(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        if (p) p.Respawn();
    }
}
