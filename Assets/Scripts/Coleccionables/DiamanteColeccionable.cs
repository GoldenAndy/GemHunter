using UnityEngine;
using TMPro;

public class DiamanteColeccionable : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoGemas;

    [Header("Sonido")]
    [SerializeField] private AudioClip sonidoRecoger;

    private int gemas = 0;
    private int totalGemas = 3;

    private void Start()
    {
        ActualizarContador();
    }

    private void OnTriggerEnter2D(
        Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        gemas++;

        ActualizarContador();

        if (AudioManager.Instance != null &&
            sonidoRecoger != null)
        {
            AudioManager.Instance.ReproducirSFX(
                sonidoRecoger
            );
        }

        Destroy(gameObject);
    }

    private void ActualizarContador()
    {
        if (textoGemas != null)
        {
            textoGemas.text =
                "Gemas: " +
                gemas +
                "/" +
                totalGemas;
        }
    }
}