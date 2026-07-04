using Cinemachine;
using UnityEngine;

public class ActivarSeguimientoCamara : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineVirtualCamera camaraVirtual;
    [SerializeField] private Transform jugador;

    [Header("Activación")]
    [Range(0f, 1f)]
    [SerializeField] private float puntoActivacionHorizontal = 0.5f;

    [Header("Seguimiento horizontal")]
    [SerializeField] private float suavizadoHorizontal = 0.15f;

    private bool seguimientoActivado;

    private float posicionYFija;
    private float velocidadSuavizadoX;

    private void Awake()
    {
        if (camaraVirtual == null)
        {
            camaraVirtual = GetComponent<CinemachineVirtualCamera>();
        }

        if (camaraVirtual == null || jugador == null)
        {
            Debug.LogWarning(
                "Faltan referencias en ActivarSeguimientoCamara.",
                this
            );

            enabled = false;
            return;
        }

        // La cámara no utiliza Follow.
        camaraVirtual.Follow = null;
        camaraVirtual.LookAt = null;

        // Mantiene Body en Do Nothing.
        camaraVirtual
            .DestroyCinemachineComponent<CinemachineFramingTransposer>();

        // Guarda la altura correcta de la cámara inicial.
        posicionYFija = camaraVirtual.transform.position.y;

        seguimientoActivado = false;
    }

    private void LateUpdate()
    {
        if (!seguimientoActivado)
        {
            RevisarActivacion();
            return;
        }

        SeguirJugadorHorizontalmente();
    }

    private void RevisarActivacion()
    {
        Camera camaraPrincipal = Camera.main;

        if (camaraPrincipal == null)
        {
            return;
        }

        Vector3 posicionJugadorEnPantalla =
            camaraPrincipal.WorldToViewportPoint(jugador.position);

        if (posicionJugadorEnPantalla.x >= puntoActivacionHorizontal)
        {
            seguimientoActivado = true;
        }
    }

    private void SeguirJugadorHorizontalmente()
    {
        Vector3 posicionActual = camaraVirtual.transform.position;

        float nuevaPosicionX = Mathf.SmoothDamp(
            posicionActual.x,
            jugador.position.x,
            ref velocidadSuavizadoX,
            suavizadoHorizontal
        );

        camaraVirtual.transform.position = new Vector3(
            nuevaPosicionX,
            posicionYFija,
            posicionActual.z
        );
    }
}