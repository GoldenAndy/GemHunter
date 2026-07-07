using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public string escenaJuego = "Nivel 1";

    public RectTransform indicadorSeleccion;

    public float posicionXIndicador = -145f;

    public float posicionYPlay = 105f;
    public float posicionYLoad = 30f;
    public float posicionYExit = -45f;

    private int opcionSeleccionada = 0;

    void Start()
    {
        MoverIndicador();
    }

    void Update()
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

    void MoverIndicador()
    {
        float posicionY = posicionYPlay;

        if (opcionSeleccionada == 1)
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
            Jugar();
        }
        else if (opcionSeleccionada == 1)
        {
            Cargar();
        }
        else if (opcionSeleccionada == 2)
        {
            Salir();
        }
    }

    public void Jugar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void Cargar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void Salir()
    {
        Application.Quit();
    }
}