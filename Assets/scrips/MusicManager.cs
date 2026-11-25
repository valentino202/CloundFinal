using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Musica de fondo del juego (un solo AudioSource, DontDestroyOnLoad):
///   - MENU (SampleScene)  -> musicaMenu  (la de "ciencia"), en loop.
///   - PELEA (GameScene)   -> musicaPelea (el rock), en loop.
///   - Al GANAR/PERDER     -> se CORTA (Stop).
///   - Al REINICIAR / REVANCHA -> vuelve a empezar la musica de pelea.
///
/// El infinito no necesita nada especial: la musica va en loop y al
/// terminar el clip vuelve a empezar sola (AudioSource.loop = true).
///
/// Uso: crear un GameObject "MusicManager" en la escena del MENU, poner
/// este componente y arrastrar los 2 clips. Sus referencias sobreviven al
/// cambio de escena (DontDestroyOnLoad).
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Clips (arrastra en el Inspector)")]
    [Tooltip("Musica del menu (SampleScene).")]
    public AudioClip musicaMenu;
    [Tooltip("Musica de la pelea (GameScene). El rock.")]
    public AudioClip musicaPelea;

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumen = 0.6f; // musica de menu (fondo)
    [Range(0f, 1f)] public float volumenCombate = 0.35f; // musica de pelea (mas baja para no tapar explosiones)

    AudioSource source;
    bool cortada; // true despues de ganar/perder (no reacciona al cambio de escena)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volumen;
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += AlCambiarEscena;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= AlCambiarEscena;
    }

    void Start()
    {
        // Aplicar segun la escena en la que se creo (normalmente el menu).
        ElegirSegunEscena(SceneManager.GetActiveScene());
    }

    void AlCambiarEscena(Scene anterior, Scene actual)
    {
        ElegirSegunEscena(actual);
    }

    void ElegirSegunEscena(Scene escena)
    {
        if (cortada) return; // el resultado gobierna hasta que se reinicie
        if (escena.name == "GameScene") Reproducir(musicaPelea);
        else Reproducir(musicaMenu); // SampleScene y cualquier otra = menu
    }

    void Reproducir(AudioClip clip)
    {
        if (clip == null) return;
        // Volumen segun el clip: combate mas bajo para no tapar explosiones.
        source.volume = (clip == musicaPelea) ? volumenCombate : volumen;
        if (source.clip == clip && source.isPlaying) return; // ya suena, no reiniciar
        source.clip = clip;
        source.Play();
    }

    /// <summary>Cortar la musica al ganar/perder/empatar.</summary>
    public void Cortar()
    {
        cortada = true;
        if (source != null) source.Stop();
        Debug.Log("[Music] Musica cortada (fin de partida).");
    }

    /// <summary>Reanudar la musica de pelea al reiniciar o dar revancha.</summary>
    public void ReanudarPelea()
    {
        cortada = false;
        Reproducir(musicaPelea);
        Debug.Log("[Music] Musica de pelea reanudada.");
    }

    /// <summary>Volver al menu (por si se usa desde un boton): reactiva y suena menu.</summary>
    public void IrAMenu()
    {
        cortada = false;
        Reproducir(musicaMenu);
    }
}
