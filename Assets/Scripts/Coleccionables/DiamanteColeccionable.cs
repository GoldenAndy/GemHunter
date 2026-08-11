using UnityEngine;
using TMPro;

public class DiamanteColeccionable : MonoBehaviour
{
    [SerializeField] private TMP_Text textoGemas;

    private int gemas = 0;
    private int totalGemas = 3;

    private void Start()
    {
        ActualizarContador();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            gemas++;
            ActualizarContador();

            Destroy(gameObject);
        }
    }

    private void ActualizarContador()
    {
        if (textoGemas != null)
        {
            textoGemas.text = "Gemas: " + gemas + "/" + totalGemas;
        }
    }
}