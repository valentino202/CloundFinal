using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Navegacion entre Menu y Partida (capa UI, MonoBehaviour OK).
/// El boton Jugar llama a LoadGameScene(). El boton Volver llama a LoadMenu().
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Nombres de escena (ver Build Settings)")]
    public string menuScene = "SampleScene";
    public string gameScene = "GameScene";

    public void LoadGameScene()
    {
        Debug.Log("[Scene] Cargando partida: " + gameScene);
        SceneManager.LoadScene(gameScene);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(menuScene);
    }
}
