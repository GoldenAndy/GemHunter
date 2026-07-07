using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    public GameObject menuPausaCompleto;
    public string escenaMenuPrincipal = "MenuPrincipal";

    public RectTransform indicadorSeleccion;

    public float posicionXIndicador = -145f;

    public float posicionYPlay = 105f;
    public float posicionYLoad = 30f;
    public float posicionYExit = -45f;

    private bool juegoPausado = false;
    private int opcionSeleccionada = 1;

    void Start()
    {
        menuPausaCompleto.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }

        if (juegoPausado)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                opcionSeleccionada++;

                if (opcionSeleccionada > 2)
                {
                    opcionSeleccionada = 0;
                }

                MoverIndicador();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                opcionSeleccionada--;

                if (opcionSeleccionada < 0)
                {
                    opcionSeleccionada = 2;
                }

                MoverIndicador();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                EjecutarOpcion();
            }
        }
    }

    public void Pausar()
    {
        menuPausaCompleto.SetActive(true);
        Time.timeScale = 0f;
        juegoPausado = true;

        opcionSeleccionada = 1;
        MoverIndicador();
    }

    public void Reanudar()
    {
        menuPausaCompleto.SetActive(false);
        Time.timeScale = 1f;
        juegoPausado = false;
    }

    void MoverIndicador()
    {
        float posicionY = posicionYLoad;

        if (opcionSeleccionada == 0)
        {
            posicionY = posicionYPlay;
        }
        else if (opcionSeleccionada == 1)
        {
            posicionY = posicionYLoad;
        }
        else if (opcionSeleccionada == 2)
        {
            posicionY = posicionYExit;
        }

        indicadorSeleccion.anchoredPosition = new Vector2(posicionXIndicador, posicionY);
    }

    void EjecutarOpcion()
    {
        if (opcionSeleccionada == 0)
        {
            Reanudar();
        }
        else if (opcionSeleccionada == 1)
        {
            Reanudar();
        }
        else if (opcionSeleccionada == 2)
        {
            VolverAlMenu();
        }
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    public void Salir()
    {
        Application.Quit();
    }
}