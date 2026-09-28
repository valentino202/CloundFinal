using UnityEngine;

/// <summary>
/// Anima el titulo (imagen UI) en el punto donde este, solo escala:
/// 1) nace pequeno, 2) crece hasta el pico, 3) cede un poco a normal,
/// 4) palpita suave agrandando/achicando en loop.
/// Uso: agregalo al GameObject de la imagen del titulo. Sin configurar nada funciona.
/// </summary>
public class TitleAnimator : MonoBehaviour
{
    [Header("Intro")]
    public float startScale = 0.2f;
    public float peakScale = 1.15f;
    public float popTime = 0.6f;
    public float settleTime = 0.25f;
    public float startDelay = 0.2f;

    [Header("Latido idle")]
    public float pulseAmplitude = 0.04f;
    public float pulseSpeed = 2f;

    private float t = 0f;
    private int phase = 0; // 0 espera, 1 crecer, 2 ceder, 3 latido
    private float pulseT = 0f;

    private void Start()
    {
        Restart();
    }

    private void OnEnable()
    {
        Restart();
    }

    public void Restart()
    {
        t = 0f;
        phase = 0;
        pulseT = 0f;
        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (phase == 0)
        {
            t += dt;
            if (t >= startDelay) { t = 0f; phase = 1; }
        }
        else if (phase == 1)
        {
            t += dt;
            float k = Mathf.Clamp01(t / popTime);
            float s = Mathf.Lerp(startScale, peakScale, EaseOutBack(k));
            transform.localScale = Vector3.one * s;
            if (k >= 1f) { t = 0f; phase = 2; }
        }
        else if (phase == 2)
        {
            t += dt;
            float k = Mathf.Clamp01(t / settleTime);
            float s = Mathf.Lerp(peakScale, 1f, k * k * (3f - 2f * k));
            transform.localScale = Vector3.one * s;
            if (k >= 1f) { phase = 3; }
        }
        else
        {
            // Latido solo hacia arriba: 1 -> 1+amp -> 1 (nunca baja de 1).
            pulseT += dt * pulseSpeed;
            float s = 1f + (0.5f - 0.5f * Mathf.Cos(pulseT)) * pulseAmplitude;
            transform.localScale = Vector3.one * s;
        }
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
