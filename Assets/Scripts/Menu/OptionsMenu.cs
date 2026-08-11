using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Controles")]
    public Slider sliderMusic;
    public Slider sliderSFX;
    public Toggle toggleFullscreen;

    [Header("Indicador")]
    public RectTransform indicadorOptions;

    [Header("Posiciones indicador")]
    public float posicionX = -36f;

    public float posicionYMusic = 19f;
    public float posicionYSFX = 6f;
    public float posicionYFullscreen = -8f;
    public float posicionYBack = -23f;

    [Header("Menu Principal")]
    public MenuPrincipal menuPrincipal;

    private int opcionSeleccionada = 0;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            sliderMusic.value =
                AudioManager.Instance.ObtenerVolumenMusica();

            sliderSFX.value =
                AudioManager.Instance.ObtenerVolumenSFX();
        }

        bool fullscreen =
            PlayerPrefs.GetInt(
                "Fullscreen",
                Screen.fullScreen ? 1 : 0
            ) == 1;

        toggleFullscreen.isOn = fullscreen;

        MoverIndicador();
    }

    private void OnEnable()
    {
        opcionSeleccionada = 0;

        if (indicadorOptions != null)
        {
            indicadorOptions.gameObject.SetActive(true);
            MoverIndicador();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.S))
        {
            opcionSeleccionada++;

            if (opcionSeleccionada > 3)
                opcionSeleccionada = 0;

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.W))
        {
            opcionSeleccionada--;

            if (opcionSeleccionada < 0)
                opcionSeleccionada = 3;

            MoverIndicador();

            if (AudioManager.Instance != null)
                AudioManager.Instance.SonidoMover();
        }

        // IZQUIERDA
        if (Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.A))
        {
            CambiarValor(-0.1f);
        }

        // DERECHA
        if (Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.D))
        {
            CambiarValor(0.1f);
        }

        // ENTER
        if (Input.GetKeyDown(KeyCode.Return))
        {
            EjecutarOpcion();
        }

        // ESCAPE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuPrincipal.CerrarOptions();
        }
    }

    private void CambiarValor(float cantidad)
    {
        if (opcionSeleccionada == 0)
        {
            sliderMusic.value =
                Mathf.Clamp01(sliderMusic.value + cantidad);

            CambiarMusica(sliderMusic.value);
        }

        else if (opcionSeleccionada == 1)
        {
            sliderSFX.value =
                Mathf.Clamp01(sliderSFX.value + cantidad);

            CambiarSFX(sliderSFX.value);
        }
    }

    private void EjecutarOpcion()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SonidoSeleccionar();

        // FULLSCREEN
        if (opcionSeleccionada == 2)
        {
            toggleFullscreen.isOn =
                !toggleFullscreen.isOn;

            CambiarFullscreen(
                toggleFullscreen.isOn
            );
        }

        // BACK
        else if (opcionSeleccionada == 3)
        {
            menuPrincipal.CerrarOptions();
        }
    }

    private void MoverIndicador()
    {
        if (indicadorOptions == null)
            return;

        float posicionY = posicionYMusic;

        if (opcionSeleccionada == 1)
        {
            posicionY = posicionYSFX;
        }
        else if (opcionSeleccionada == 2)
        {
            posicionY = posicionYFullscreen;
        }
        else if (opcionSeleccionada == 3)
        {
            posicionY = posicionYBack;
        }

        indicadorOptions.anchoredPosition =
            new Vector2(
                posicionX,
                posicionY
            );
    }

    public void CambiarMusica(float valor)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .CambiarVolumenMusica(valor);
        }
    }

    public void CambiarSFX(float valor)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .CambiarVolumenSFX(valor);
        }
    }

    public void CambiarFullscreen(bool activo)
    {
        Screen.fullScreen = activo;

        PlayerPrefs.SetInt(
            "Fullscreen",
            activo ? 1 : 0
        );

        PlayerPrefs.Save();
    }
}