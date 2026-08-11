using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemigoVida))]
[RequireComponent(typeof(EnemigoContacto))]
public class AguilaEnemy : MonoBehaviour
{
    public enum EstadoAguila
    {
        Esperando,
        Patrulla,
        Picada,
        Retirada,
        Evasion,
        Golpeada,
        DuoOrbita,
        DuoLanzamiento,
        DuoHostigamiento
    }

    [Header("Referencias")]
    [SerializeField] private Transform jugador;

    // =========================================================
    // ZONA DEL ÁGUILA
    // =========================================================

    [Header("Zona de activación")]

    [Tooltip(
        "Distancia desde el lugar donde colocaste el águila " +
        "a la que detectará al jugador."
    )]
    [SerializeField] private float rangoDeteccion = 7f;

    [Tooltip(
        "Distancia que puede alejarse el jugador antes de que " +
        "el águila abandone la persecución."
    )]
    [SerializeField] private float rangoAbandono = 10f;

    [Tooltip(
        "Ancho de patrulla cuando todavía no ha detectado al jugador."
    )]
    [SerializeField] private float anchoPatrullaReposo = 3f;

    // =========================================================
    // VUELO NORMAL
    // =========================================================

    [Header("Vuelo normal")]

    [Tooltip(
        "Ancho dentro del cual se mueve alrededor del jugador " +
        "una vez que comienza el combate."
    )]
    [SerializeField] private float anchoPatrullaCombate = 6f;

    [SerializeField] private float velocidadPatrulla = 2.5f;

    [SerializeField] private float velocidadAjusteAltura = 3f;

    // =========================================================
    // ATAQUE
    // =========================================================

    [Header("Ataque en picada")]

    [SerializeField] private float tiempoMinimoEntreAtaques = 1.5f;

    [SerializeField] private float tiempoMaximoEntreAtaques = 2.8f;

    [SerializeField] private float velocidadPicada = 8.5f;

    [Tooltip(
        "Predicción de movimiento del jugador durante la picada."
    )]
    [SerializeField] private float anticipacionJugador = 0.20f;

    [SerializeField] private float tiempoMaximoPicada = 1.6f;

    // =========================================================
    // SUELO
    // =========================================================

    [Header("Detección del suelo")]

    [SerializeField] private LayerMask capaSuelo;

    [SerializeField] private float distanciaMinimaSuelo = 0.7f;

    // =========================================================
    // RETIRADA
    // =========================================================

    [Header("Retirada")]

    [SerializeField] private float velocidadRetirada = 6.5f;

    [SerializeField] private float distanciaLateralRetirada = 2.5f;

    [SerializeField] private float distanciaLlegada = 0.25f;

    // =========================================================
    // EVASIÓN
    // =========================================================

    [Header("Evasión de espada")]

    [SerializeField] private float velocidadEvasion = 10f;

    [SerializeField] private float distanciaEvasionHorizontal = 3f;

    [SerializeField] private float distanciaEvasionVertical = 2.2f;

    // =========================================================
    // GOLPE RECIBIDO
    // =========================================================

    [Header("Reacción al recibir golpe")]

    [SerializeField] private float tiempoGolpeada = 0.35f;

    [Tooltip(
        "Fuerza horizontal con la que sale despedida al recibir daño."
    )]
    [SerializeField] private float retrocesoGolpe = 5.5f;

    [Tooltip(
        "Pequeño impulso vertical al recibir daño."
    )]
    [SerializeField] private float impulsoVerticalGolpe = 1.2f;

    [Tooltip(
        "Qué tan rápido pierde velocidad después del golpe."
    )]
    [SerializeField] private float frenadoTrasGolpe = 10f;


    // =========================================================
    // CONTROL DE DÚO / JEFE
    // =========================================================

    private bool modoJefeDuoActivo;

    private Vector2 duoCentroOrbita;
    private float duoRadioOrbita;
    private float duoVelocidadAngular;
    private float duoAnguloActual;
    private float duoVueltasActuales;
    private int duoVueltasObjetivo;

    private Vector2 duoDestino;
    private float duoVelocidadMovimiento;
    private bool duoIgnorarEvasion;

    


    // =========================================================
    // ANIMATOR
    // =========================================================

    [Header("Animator")]

    [SerializeField] private string estadoIdle = "Aguila_Idle";

    [SerializeField] private string estadoAtaque = "Aguila_Ataque";

    [SerializeField] private string estadoGolpeada = "Aguila_Golpeada";

    // =========================================================
    // COMPONENTES
    // =========================================================

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemigoContacto contacto;

    private Rigidbody2D jugadorRb;
    private Camera camara;

    // =========================================================
    // POSICIÓN ORIGINAL
    // =========================================================

    private Vector2 posicionInicial;

    /*
     * ESTA es la altura que nos importa.
     *
     * Se guarda exactamente donde pusiste al águila
     * en el escenario.
     */
    private float alturaBaseY;

    // =========================================================
    // ESTADO
    // =========================================================

    private EstadoAguila estadoActual;

    private bool combateActivo;

    private float direccionPatrulla = -1f;

    private float tiempoHastaAtaque;

    private float tiempoActualPicada;

    private float tiempoRestanteGolpeada;

    private Vector2 objetivoPicada;

    private Vector2 objetivoRetirada;

    private Vector2 objetivoEvasion;

    // =========================================================
    // EMPUJE ESPECIAL
    // =========================================================

    private Vector2 impulsoGolpePendiente;

    private bool debeAplicarImpulsoGolpe;

    // =========================================================
    // PROPIEDADES PÚBLICAS
    // =========================================================

    public EstadoAguila EstadoActual => estadoActual;

    public bool EstaEnPicada =>
        estadoActual == EstadoAguila.Picada;

    public bool EstaPatrullando =>
        estadoActual == EstadoAguila.Patrulla;

    public bool CombateActivo =>
        combateActivo;

    public float AlturaBaseY =>
        alturaBaseY;


    public Vector2 PosicionInicial => posicionInicial;

    public bool EstaBajoControlDuo =>
        estadoActual == EstadoAguila.DuoOrbita ||
        estadoActual == EstadoAguila.DuoLanzamiento ||
        estadoActual == EstadoAguila.DuoHostigamiento;

    public bool EstaEnLanzamientoDuo =>
        estadoActual == EstadoAguila.DuoLanzamiento;

    public bool DuoIgnoraEvasion =>
        duoIgnorarEvasion;

    public bool EstaEnOrbitaDuo =>
        estadoActual == EstadoAguila.DuoOrbita;

    public bool TerminoVueltasDuo =>
        estadoActual == EstadoAguila.DuoOrbita &&
        duoVueltasActuales >= duoVueltasObjetivo;

    public bool PuedeSerDirigidaPorPareja =>
        estadoActual != EstadoAguila.Golpeada &&
        estadoActual != EstadoAguila.Evasion;



    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        contacto = GetComponent<EnemigoContacto>();

        // Guardamos EXACTAMENTE dónde fue colocada.
        posicionInicial = transform.position;

        // Esta será su altura de vuelo durante toda su vida.
        alturaBaseY = transform.position.y;

        rb.gravityScale = 0f;

        rb.freezeRotation = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        camara = Camera.main;
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (jugador == null)
        {
            GameObject objetoJugador =
                GameObject.FindGameObjectWithTag("Player");

            if (objetoJugador != null)
            {
                jugador = objetoJugador.transform;
            }
        }

        if (jugador == null)
        {
            Debug.LogError(
                $"{name}: No se encontró al jugador."
            );

            enabled = false;

            return;
        }

        jugadorRb =
            jugador.GetComponent<Rigidbody2D>();

        combateActivo = false;

        estadoActual =
            EstadoAguila.Esperando;

        direccionPatrulla =
            Random.value < 0.5f
                ? -1f
                : 1f;

        ReproducirIdle();

        ProgramarSiguienteAtaque();
    }

    // =========================================================
    // EVENTOS
    // =========================================================

    private void OnEnable()
    {
        if (contacto != null)
        {
            contacto.OnDanoAplicado +=
                ManejarDanoAplicadoAlJugador;
        }
    }

    private void OnDisable()
    {
        if (contacto != null)
        {
            contacto.OnDanoAplicado -=
                ManejarDanoAplicadoAlJugador;
        }
    }

    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (jugador == null)
            return;

        switch (estadoActual)
        {
            case EstadoAguila.Esperando:

                ActualizarEspera();

                break;

            case EstadoAguila.Patrulla:

                ActualizarPatrulla();

                break;

            case EstadoAguila.Picada:

                ActualizarPicada();

                break;

            case EstadoAguila.Retirada:

                ActualizarRetirada();

                break;

            case EstadoAguila.Evasion:

                ActualizarEvasion();

                break;

            case EstadoAguila.Golpeada:

                ActualizarGolpeada();

                break;

            case EstadoAguila.DuoOrbita:
                ActualizarDuoOrbita();
                break;

            case EstadoAguila.DuoLanzamiento:
                ActualizarDuoLanzamiento();
                break;

            case EstadoAguila.DuoHostigamiento:
                ActualizarDuoHostigamiento();
                break;
        }
    }

    // =========================================================
    // ESPERA
    // =========================================================

    private void ActualizarEspera()
    {
        /*
         * Antes de detectar al jugador, el águila patrulla
         * alrededor del lugar donde fue colocada.
         */

        float mitad =
            anchoPatrullaReposo * 0.5f;

        float izquierda =
            posicionInicial.x - mitad;

        float derecha =
            posicionInicial.x + mitad;

        if (transform.position.x <= izquierda)
        {
            direccionPatrulla = 1f;
        }
        else if (transform.position.x >= derecha)
        {
            direccionPatrulla = -1f;
        }

        float diferenciaY =
            alturaBaseY -
            transform.position.y;

        float velocidadY =
            Mathf.Clamp(
                diferenciaY * 2f,
                -velocidadAjusteAltura,
                velocidadAjusteAltura
            );

        rb.velocity =
            new Vector2(
                direccionPatrulla *
                velocidadPatrulla,

                velocidadY
            );

        ActualizarDireccionVisual(
            rb.velocity.x
        );

        // ¿Entró el jugador a nuestra zona?
        if (JugadorDentroDeRango(
            rangoDeteccion))
        {
            ActivarCombate();
        }
    }

    // =========================================================
    // ACTIVAR COMBATE
    // =========================================================

    private void ActivarCombate()
    {
        combateActivo = true;

        estadoActual =
            EstadoAguila.Patrulla;

        ProgramarSiguienteAtaque();

        ReproducirIdle();

        Debug.Log(
            $"{name}: ¡Jugador detectado!"
        );
    }

    // =========================================================
    // DESACTIVAR COMBATE
    // =========================================================

    private void DesactivarCombate()
    {
        combateActivo = false;

        estadoActual =
            EstadoAguila.Esperando;

        ReproducirIdle();

        Debug.Log(
            $"{name}: El jugador abandonó la zona."
        );
    }

    // =========================================================
    // PATRULLA DE COMBATE
    // =========================================================

    private void ActualizarPatrulla()
    {
        /*
         * IMPORTANTE:
         *
         * X sigue al jugador.
         *
         * Y NO.
         *
         * La Y siempre intenta regresar a la altura
         * donde colocaste originalmente al águila.
         */

        float centroX =
            jugador.position.x;

        float mitad =
            anchoPatrullaCombate * 0.5f;

        float limiteIzquierdo =
            centroX - mitad;

        float limiteDerecho =
            centroX + mitad;

        if (transform.position.x <=
            limiteIzquierdo)
        {
            direccionPatrulla = 1f;
        }
        else if (transform.position.x >=
                 limiteDerecho)
        {
            direccionPatrulla = -1f;
        }

        // =====================================================
        // ALTURA FIJA
        // =====================================================

        float diferenciaY =
            alturaBaseY -
            transform.position.y;

        float velocidadY =
            Mathf.Clamp(
                diferenciaY * 2f,
                -velocidadAjusteAltura,
                velocidadAjusteAltura
            );

        rb.velocity =
            new Vector2(
                direccionPatrulla *
                velocidadPatrulla,

                velocidadY
            );

        ActualizarDireccionVisual(
            rb.velocity.x
        );

        // =====================================================
        // JUGADOR SE FUE
        // =====================================================

        if (!modoJefeDuoActivo &&
            !JugadorDentroDeRango(rangoAbandono))
        {
            DesactivarCombate();
            return;
        }

        // =====================================================
        // ATAQUE
        // =====================================================

        tiempoHastaAtaque -=
            Time.fixedDeltaTime;

        if (tiempoHastaAtaque <= 0f &&
            EstaVisibleEnCamara())
        {
            IniciarPicada();
        }
    }

    // =========================================================
    // PICADA
    // =========================================================

    private void IniciarPicada()
    {
        estadoActual =
            EstadoAguila.Picada;

        tiempoActualPicada = 0f;

        Vector2 posicionPredicha =
            jugador.position;

        if (jugadorRb != null)
        {
            posicionPredicha +=
                jugadorRb.velocity *
                anticipacionJugador;
        }

        objetivoPicada =
            posicionPredicha;

        animator.CrossFade(
            estadoAtaque,
            0.05f
        );
    }

    private void ActualizarPicada()
    {
        tiempoActualPicada +=
            Time.fixedDeltaTime;

        Vector2 diferencia =
            objetivoPicada -
            rb.position;

        Vector2 direccion =
            diferencia.normalized;

        Vector2 velocidad =
            direccion *
            velocidadPicada;

        rb.velocity =
            velocidad;

        ActualizarDireccionVisual(
            velocidad.x
        );

        if (SueloDemasiadoCerca())
        {
            IniciarRetirada();

            return;
        }

        if (tiempoActualPicada >=
            tiempoMaximoPicada)
        {
            IniciarRetirada();

            return;
        }

        if (diferencia.magnitude <=
            0.35f)
        {
            IniciarRetirada();
        }
    }

    // =========================================================
    // SUELO
    // =========================================================

    private bool SueloDemasiadoCerca()
    {
        RaycastHit2D golpe =
            Physics2D.Raycast(
                rb.position,
                Vector2.down,
                distanciaMinimaSuelo,
                capaSuelo
            );

        return golpe.collider != null;
    }

    // =========================================================
    // RETIRADA
    // =========================================================

    private void IniciarRetirada()
    {
        duoIgnorarEvasion = false;
        
        estadoActual =
            EstadoAguila.Retirada;

        float lado =
            Mathf.Sign(
                transform.position.x -
                jugador.position.x
            );

        if (lado == 0f)
        {
            lado =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        /*
         * X puede variar.
         *
         * Y SIEMPRE es alturaBaseY.
         */
        objetivoRetirada =
            new Vector2(
                jugador.position.x +
                lado *
                distanciaLateralRetirada,

                alturaBaseY
            );

        ReproducirIdle();
    }

    private void ActualizarRetirada()
    {
        if (MoverHacia(
            objetivoRetirada,
            velocidadRetirada))
        {
        if (modoJefeDuoActivo ||
            JugadorDentroDeRango(rangoAbandono))
            {
                estadoActual =
                    EstadoAguila.Patrulla;

                ProgramarSiguienteAtaque();

                ReproducirIdle();
            }
            else
            {
                DesactivarCombate();
            }
        }
    }

    // =========================================================
    // EVASIÓN
    // =========================================================

    public void ForzarEvasion()
    {
        if (jugador == null)
            return;

        estadoActual =
            EstadoAguila.Evasion;

        float lado =
            Mathf.Sign(
                transform.position.x -
                jugador.position.x
            );

        if (lado == 0f)
        {
            lado =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        objetivoEvasion =
            rb.position +
            new Vector2(
                lado *
                distanciaEvasionHorizontal,

                distanciaEvasionVertical
            );

        ReproducirIdle();
    }

    private void ActualizarEvasion()
    {
        if (MoverHacia(
            objetivoEvasion,
            velocidadEvasion))
        {
            IniciarRetirada();
        }
    }

    // =========================================================
    // GOLPE RECIBIDO
    // =========================================================

    public void RegistrarGolpeRecibido(
        DamageInfo damageInfo)
    {
        estadoActual =
            EstadoAguila.Golpeada;

        tiempoRestanteGolpeada =
            tiempoGolpeada;

        animator.CrossFade(
            estadoGolpeada,
            0.03f
        );

        // =====================================================
        // CALCULAR HACIA DÓNDE EMPUJAR
        // =====================================================

        float direccionX = 0f;

        /*
         * Preferimos alejarnos físicamente
         * del atacante.
         */
        if (damageInfo.atacante != null)
        {
            direccionX =
                Mathf.Sign(
                    transform.position.x -
                    damageInfo.atacante
                        .transform.position.x
                );
        }

        /*
         * Si por alguna razón atacante no da
         * una dirección útil, usamos DamageInfo.
         */
        if (Mathf.Abs(direccionX) <
            0.01f)
        {
            direccionX =
                Mathf.Sign(
                    damageInfo.direccion.x
                );
        }

        if (Mathf.Abs(direccionX) <
            0.01f)
        {
            direccionX =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        impulsoGolpePendiente =
            new Vector2(
                direccionX *
                retrocesoGolpe,

                impulsoVerticalGolpe
            );

        /*
         * Lo aplicamos en el próximo FixedUpdate.
         *
         * Así evitamos que EnemigoVida o el golpe
         * de espada sobrescriba inmediatamente
         * nuestra reacción especial.
         */
        debeAplicarImpulsoGolpe =
            true;
    }

    private void ActualizarGolpeada()
    {
        // =====================================================
        // PRIMER FRAME DEL GOLPE
        // =====================================================

        if (debeAplicarImpulsoGolpe)
        {
            rb.velocity =
                impulsoGolpePendiente;

            debeAplicarImpulsoGolpe =
                false;
        }
        else
        {
            /*
             * Luego va perdiendo velocidad
             * progresivamente.
             */

            rb.velocity =
                Vector2.MoveTowards(
                    rb.velocity,
                    Vector2.zero,
                    frenadoTrasGolpe *
                    Time.fixedDeltaTime
                );
        }

        tiempoRestanteGolpeada -=
            Time.fixedDeltaTime;

        if (tiempoRestanteGolpeada <= 0f)
        {
            IniciarRetirada();
        }
    }

    // =========================================================
    // ÁGUILA GOLPEÓ AL JUGADOR
    // =========================================================

private void ManejarDanoAplicadoAlJugador(
    Vector2 direccion)
{
    switch (estadoActual)
    {
        // Ataque individual.
        case EstadoAguila.Picada:

        // Contacto durante patrulla.
        case EstadoAguila.Patrulla:

        // Salida disparada del dúo.
        case EstadoAguila.DuoLanzamiento:

        // El compañero logró cerrar al jugador.
        case EstadoAguila.DuoHostigamiento:

            IniciarRetirada();

            break;

        /*
         * DuoOrbita NO aparece aquí a propósito.
         *
         * Durante el Supergiro puede rozar al jugador,
         * hacerle daño y CONTINUAR girando.
         *
         * Eso es exactamente lo que queríamos.
         */
    }
}

    // =========================================================
    // RANGO
    // =========================================================

    private bool JugadorDentroDeRango(
        float rango)
    {
        /*
         * MUY IMPORTANTE:
         *
         * El rango está centrado en donde colocaste
         * originalmente al águila.
         *
         * No viaja junto con ella.
         */

        float distancia =
            Vector2.Distance(
                jugador.position,
                posicionInicial
            );

        return distancia <= rango;
    }

    // =========================================================
    // MOVIMIENTO HACIA OBJETIVO
    // =========================================================

    private bool MoverHacia(
        Vector2 destino,
        float velocidadMaxima)
    {
        Vector2 diferencia =
            destino -
            rb.position;

        if (diferencia.magnitude <=
            distanciaLlegada)
        {
            rb.velocity =
                Vector2.zero;

            return true;
        }

        Vector2 velocidadNecesaria =
            diferencia /
            Time.fixedDeltaTime;

        Vector2 velocidad =
            Vector2.ClampMagnitude(
                velocidadNecesaria,
                velocidadMaxima
            );

        rb.velocity =
            velocidad;

        ActualizarDireccionVisual(
            velocidad.x
        );

        return false;
    }

    // =========================================================
    // SPRITE
    // =========================================================

    private void ActualizarDireccionVisual(
        float movimientoX)
    {
        if (Mathf.Abs(movimientoX) <
            0.05f)
        {
            return;
        }

        /*
         * Sprite original:
         * mira a la IZQUIERDA.
         */

        spriteRenderer.flipX =
            movimientoX > 0f;
    }

    // =========================================================
    // ANIMACIONES
    // =========================================================

    private void ReproducirIdle()
    {
        animator.CrossFade(
            estadoIdle,
            0.05f
        );
    }

    // =========================================================
    // PROGRAMAR ATAQUE
    // =========================================================

    private void ProgramarSiguienteAtaque()
    {
        tiempoHastaAtaque =
            Random.Range(
                tiempoMinimoEntreAtaques,
                tiempoMaximoEntreAtaques
            );
    }

    // =========================================================
    // CÁMARA
    // =========================================================

    private bool EstaVisibleEnCamara()
    {
        if (camara == null)
        {
            camara =
                Camera.main;
        }

        if (camara == null)
            return true;

        Vector3 viewport =
            camara.WorldToViewportPoint(
                transform.position
            );

        return
            viewport.z > 0f &&
            viewport.x > 0.03f &&
            viewport.x < 0.97f &&
            viewport.y > 0.03f &&
            viewport.y < 0.97f;
    }



// =========================================================
// COMBATE COMPARTIDO
// =========================================================

// =========================================================
// SUPERGIRO
// =========================================================

private void ActualizarDuoOrbita()
{
    // Cuántos grados avanzamos durante este FixedUpdate.
    float deltaAngulo =
        duoVelocidadAngular *
        Time.fixedDeltaTime;

    duoAnguloActual +=
        deltaAngulo;

    // Convertimos los grados recorridos en vueltas.
    duoVueltasActuales +=
        Mathf.Abs(deltaAngulo) /
        360f;

    // Convertimos el ángulo a radianes.
    float radianes =
        duoAnguloActual *
        Mathf.Deg2Rad;

    // Calculamos el punto de la circunferencia
    // en el que debe encontrarse el águila.
    Vector2 offsetOrbita =
        new Vector2(
            Mathf.Cos(radianes),
            Mathf.Sin(radianes)
        ) *
        duoRadioOrbita;

    Vector2 destinoOrbita =
        duoCentroOrbita +
        offsetOrbita;

    // Calculamos una velocidad adecuada para la órbita.
    //
    // v = velocidadAngularEnRadianes * radio
    float velocidadOrbita =
        Mathf.Abs(
            duoVelocidadAngular
        ) *
        Mathf.Deg2Rad *
        duoRadioOrbita;

    // Le damos un pequeño margen para que pueda
    // alcanzar correctamente el punto que se mueve.
    velocidadOrbita *= 1.20f;

    velocidadOrbita =
        Mathf.Max(
            velocidadOrbita,
            2f
        );

    MoverHacia(
        destinoOrbita,
        velocidadOrbita
    );

    /*
     * MUY IMPORTANTE:
     *
     * NO hacemos animator.CrossFade aquí.
     *
     * La animación Aguila_Ataque ya se inició
     * cuando comenzó el Supergiro.
     *
     * Si hacemos CrossFade cada FixedUpdate,
     * la animación se reinicia constantemente.
     */
}


// =========================================================
// LANZAMIENTO DÚO
// =========================================================

private void ActualizarDuoLanzamiento()
{
    Vector2 diferencia =
        duoDestino -
        rb.position;

    // Si ya alcanzó aproximadamente el punto,
    // termina el ataque y vuelve arriba.
    if (diferencia.magnitude <=
        0.35f)
    {
        IniciarRetirada();

        return;
    }

    Vector2 direccion =
        diferencia.normalized;

    rb.velocity =
        direccion *
        duoVelocidadMovimiento;

    ActualizarDireccionVisual(
        rb.velocity.x
    );

    // Evitamos que termine incrustada
    // contra el suelo.
    if (SueloDemasiadoCerca())
    {
        IniciarRetirada();

        return;
    }

    /*
     * NO hacemos CrossFade aquí.
     *
     * La animación de ataque ya comenzó
     * en IniciarLanzamientoDuo().
     */
}


// =========================================================
// HOSTIGAMIENTO DÚO
// =========================================================

private void ActualizarDuoHostigamiento()
{
    Vector2 diferencia =
        duoDestino -
        rb.position;

    // Llegó a la posición desde donde
    // quiere cerrar al jugador.
    if (diferencia.magnitude <=
        distanciaLlegada)
    {
        rb.velocity =
            Vector2.zero;

        return;
    }

    MoverHacia(
        duoDestino,
        duoVelocidadMovimiento
    );

    /*
     * Tampoco llamamos ReproducirIdle() aquí.
     *
     * Ya se hizo UNA vez al comenzar
     * el hostigamiento.
     */
}



// =========================================================
// MODO JEFE DÚO
// =========================================================

public void ForzarModoJefeDuo(bool activo)
{
    modoJefeDuoActivo = activo;

    /*
     * Si entra en modo dúo, consideramos que el combate
     * queda activado aunque esta águila todavía no hubiera
     * detectado individualmente al jugador.
     */
    if (activo && !combateActivo)
    {
        ActivarCombate();
    }
}


// =========================================================
// SUPERGIRO / ÓRBITA
// =========================================================

public void IniciarOrbitaDuo(
    Vector2 centro,
    float radio,
    float velocidadAngular,
    float anguloInicial,
    int vueltasObjetivo)
{
    combateActivo = true;

    estadoActual =
        EstadoAguila.DuoOrbita;

    duoCentroOrbita =
        centro;

    duoRadioOrbita =
        Mathf.Max(
            0.1f,
            radio
        );

    duoVelocidadAngular =
        velocidadAngular;

    duoAnguloActual =
        anguloInicial;

    duoVueltasActuales =
        0f;

    duoVueltasObjetivo =
        Mathf.Max(
            1,
            vueltasObjetivo
        );

    /*
     * Solamente iniciamos la animación UNA VEZ.
     */
    animator.CrossFade(
        estadoAtaque,
        0.03f
    );
}


// =========================================================
// ACTUALIZAR CENTRO DE LA ÓRBITA
// =========================================================

public void ActualizarCentroOrbitaDuo(
    Vector2 centro)
{
    duoCentroOrbita =
        centro;
}


// =========================================================
// LANZAMIENTO DESDE EL SUPERGIRO
// =========================================================

public void IniciarLanzamientoDuo(
    Vector2 destino,
    float velocidad,
    bool ignorarEvasion)
{
    combateActivo = true;

    estadoActual =
        EstadoAguila.DuoLanzamiento;

    duoDestino =
        destino;

    duoVelocidadMovimiento =
        Mathf.Max(
            0.1f,
            velocidad
        );

    duoIgnorarEvasion =
        ignorarEvasion;

    animator.CrossFade(
        estadoAtaque,
        0.03f
    );
}


// =========================================================
// HOSTIGAMIENTO
// =========================================================

public void IniciarHostigamientoDuo(
    Vector2 destino,
    float velocidad)
{
    combateActivo = true;

    estadoActual =
        EstadoAguila.DuoHostigamiento;

    duoDestino =
        destino;

    duoVelocidadMovimiento =
        Mathf.Max(
            0.1f,
            velocidad
        );

    /*
     * El águila que hostiga sigue aleteando.
     */
    ReproducirIdle();
}


// =========================================================
// ABANDONAR CONTROL DEL COORDINADOR
// =========================================================

public void SalirDeControlDuo()
{
    if (!gameObject.activeInHierarchy)
        return;

    /*
     * Una vez termina su parte del patrón combinado,
     * regresa a la altura original.
     */
    IniciarRetirada();
}






    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        /*
         * Así podrás seleccionar el águila en Scene
         * y ver aproximadamente su rango.
         */

        Gizmos.DrawWireSphere(
            transform.position,
            rangoDeteccion
        );

        Vector3 izquierda =
            transform.position +
            Vector3.left * 4f;

        Vector3 derecha =
            transform.position +
            Vector3.right * 4f;

        Gizmos.DrawLine(
            izquierda,
            derecha
        );
    }
}