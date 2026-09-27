using TMPro;
using UnityEngine;

/// Actividad 2 - Controlar las fuentes (tamaño y tipo) con botones.
/// La preferencia se guarda con PlayerPrefs, así se conserva entre escenas.
/// Enlazar Increase / Decrease / NextFont / ResetFont en los OnClick() de los botones.
public class FontController : MonoBehaviour
{
    public TMP_Text[] targets;
    public TMP_FontAsset[] fonts;
    public float step = 4f, minSize = 18f, maxSize = 96f;

    const string KeySize = "font_delta";
    const string KeyFont = "font_index";

    float[] baseSizes;
    float delta;
    int fontIndex;

    void Awake()
    {
        baseSizes = new float[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            if (targets[i]) baseSizes[i] = targets[i].fontSize;

        delta = PlayerPrefs.GetFloat(KeySize, 0f);
        fontIndex = PlayerPrefs.GetInt(KeyFont, 0);
        Apply();
    }

    public void Increase() => Change(step);
    public void Decrease() => Change(-step);

    public void NextFont()
    {
        if (fonts.Length == 0) return;
        fontIndex = (fontIndex + 1) % fonts.Length;
        Apply();
        Save();
    }

    public void ResetFont()
    {
        delta = 0f;
        fontIndex = 0;
        Apply();
        Save();
    }

    void Change(float d)
    {
        delta += d;
        Apply();
        Save();
    }

    void Apply()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (!targets[i]) continue;
            targets[i].fontSize = Mathf.Clamp(baseSizes[i] + delta, minSize, maxSize);
            if (fonts.Length > 0 && fonts[fontIndex % fonts.Length])
                targets[i].font = fonts[fontIndex % fonts.Length];
        }
    }

    void Save()
    {
        PlayerPrefs.SetFloat(KeySize, delta);
        PlayerPrefs.SetInt(KeyFont, fontIndex);
        PlayerPrefs.Save();
    }
}
