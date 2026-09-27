using TMPro;
using UnityEngine;

public enum GameState { Playing, Paused, Won }

/// Actividad 3 y 5 - Bucle de videojuego y estados.
/// Update() corre cada frame: cuenta el tiempo, refresca el HUD y revisa la condición de victoria.
/// Adjuntar a un objeto vacío "GameManager" en la escena Game.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Objetivo")]
    public int woodNeeded = 3;

    [Header("UI (opcional)")]
    public TMP_Text woodText;
    public TMP_Text timeText;
    public GameObject pausePanel;
    public GameObject winPanel;

    public GameState State { get; private set; } = GameState.Playing;

    int wood;
    float elapsed;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        UpdateHud();
    }

    void Update()
    {
        if (State == GameState.Playing) elapsed += Time.deltaTime;
        UpdateHud();
    }

    public void AddWood()
    {
        wood++;
        if (wood >= woodNeeded) Win();
    }

    public void TogglePause()
    {
        if (State == GameState.Won) return;
        bool pause = State == GameState.Playing;
        State = pause ? GameState.Paused : GameState.Playing;
        Time.timeScale = pause ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(pause);
    }

    void Win()
    {
        State = GameState.Won;
        if (winPanel) winPanel.SetActive(true);
    }

    void UpdateHud()
    {
        if (woodText) woodText.text = $"Leña: {wood}/{woodNeeded}";
        if (timeText) timeText.text = $"Tiempo: {elapsed:0}s";
    }
}
