using UnityEngine;

[RequireComponent(typeof(AguilaEnemy))]
public class AguilaDuoCoordinator : MonoBehaviour
{
    private enum EstadoPatron
    {
        Inactivo,
        Cooldown,
        Pinza,
        SuperGiro
    }

    // =========================================================
    // CONFIGURACIÓN
    // =========================================================

    [Header("Pareja")]
    [SerializeField] private float radioBuscarPareja = 10f;
    [SerializeField] private float radioActivacionJefe = 12f;

    [Header("Tiempo entre patrones")]
    [SerializeField] private float cooldownMin = 0.8f;
    [SerializeField] private float cooldownMax = 1.5f;

    [Header("Patrón Pinza")]
    [SerializeField] private float distanciaHostigamiento = 1.8f;
    [SerializeField] private float velocidadHostigamiento = 6f;
    [SerializeField] private float velocidadAtaquePinza = 10.5f;
    [SerializeField] private float duracionHostigamiento = 1.1f;

    [Header("Patrón Supergiro")]
    [SerializeField] private float radioOrbita = 2.2f;
    [SerializeField] private float velocidadAngular = 360f;
    [SerializeField] private int vueltasMinimas = 1;
    [SerializeField] private int vueltasMaximas = 5;
    [SerializeField] private float velocidadSalidaSupergiro = 12f;

    // =========================================================
    // REFERENCIAS
    // =========================================================

    private AguilaEnemy aguila;

    private AguilaDuoCoordinator parejaCoord;
    private AguilaEnemy pareja;

    private Transform jugador;

    // =========================================================
    // ESTADO GENERAL
    // =========================================================

    private EstadoPatron estadoPatron =
        EstadoPatron.Inactivo;

    private bool modoJefeActivo;

    private bool colisionesIgnoradas;

    private float timerCooldown;

    // =========================================================
    // PINZA
    // =========================================================

    private AguilaEnemy atacantePinza;
    private AguilaEnemy hostigadorPinza;

    private float timerHostigamiento;

    // =========================================================
    // SUPERGIRO
    // =========================================================

    private bool salidaAguilaA;
    private bool salidaAguilaB;

    /*
     * Este punto se calcula UNA SOLA VEZ
     * cuando empieza el Supergiro.
     *
     * Después NO seguirá al jugador.
     */
    private Vector2 centroSuperGiro;

    // =========================================================
    // LIDERAZGO
    // =========================================================

    /*
     * Ambas águilas tienen este script,
     * pero solamente una debe tomar decisiones.
     *
     * Usamos InstanceID para escoger automáticamente
     * cuál será la líder de la pareja.
     */
    private bool SoyLider =>
        parejaCoord != null &&
        GetInstanceID() <
        parejaCoord.GetInstanceID();

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        aguila =
            GetComponent<AguilaEnemy>();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        BuscarPareja();
        BuscarJugador();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Si perdimos la pareja o todavía
        // no encontramos una, volvemos a buscar.
        if (parejaCoord == null ||
            pareja == null)
        {
            BuscarPareja();
        }

        // Lo mismo con el jugador.
        if (jugador == null)
        {
            BuscarJugador();
        }

        // Evitamos que ambas águilas
        // choquen físicamente entre sí.
        if (parejaCoord != null &&
            !colisionesIgnoradas)
        {
            IgnorarColisionesConPareja();
        }
    }

    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (parejaCoord == null ||
            pareja == null ||
            jugador == null)
        {
            return;
        }

        /*
         * Solamente la líder ejecuta
         * la máquina de estados compartida.
         */
        if (!SoyLider)
            return;

        // Si alguna desapareció o murió,
        // cancelamos el modo jefe.
        if (!pareja.gameObject.activeInHierarchy ||
            !aguila.gameObject.activeInHierarchy)
        {
            DesactivarModoJefe();

            return;
        }

        // =====================================================
        // ACTIVAR MODO JEFE
        // =====================================================

        if (!modoJefeActivo &&
            DebeActivarModoJefe())
        {
            ActivarModoJefe();
        }

        if (!modoJefeActivo)
            return;

        // =====================================================
        // PATRÓN ACTUAL
        // =====================================================

        switch (estadoPatron)
        {
            case EstadoPatron.Cooldown:

                ActualizarCooldown();

                break;

            case EstadoPatron.Pinza:

                ActualizarPinza();

                break;

            case EstadoPatron.SuperGiro:

                ActualizarSuperGiro();

                break;
        }
    }

    // =========================================================
    // BUSCAR JUGADOR
    // =========================================================

    private void BuscarJugador()
    {
        GameObject obj =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (obj != null)
        {
            jugador =
                obj.transform;
        }
    }

    // =========================================================
    // BUSCAR PAREJA
    // =========================================================

    private void BuscarPareja()
    {
        AguilaDuoCoordinator[] todas =
            FindObjectsOfType<AguilaDuoCoordinator>();

        float mejorDistancia =
            float.MaxValue;

        AguilaDuoCoordinator mejor =
            null;

        foreach (
            AguilaDuoCoordinator otra
            in todas)
        {
            if (otra == this)
                continue;

            float dist =
                Vector2.Distance(
                    transform.position,
                    otra.transform.position
                );

            if (dist <= radioBuscarPareja &&
                dist < mejorDistancia)
            {
                mejorDistancia =
                    dist;

                mejor =
                    otra;
            }
        }

        if (mejor != null)
        {
            parejaCoord =
                mejor;

            pareja =
                mejor.GetComponent<AguilaEnemy>();
        }
    }

    // =========================================================
    // ACTIVACIÓN DEL MODO JEFE
    // =========================================================

    private bool DebeActivarModoJefe()
    {
        Vector2 centroPareja =
            (
                (Vector2)aguila.transform.position +
                (Vector2)pareja.transform.position
            ) *
            0.5f;

        float distanciaJugador =
            Vector2.Distance(
                jugador.position,
                centroPareja
            );

        /*
         * Se activa si:
         *
         * 1. El jugador entra al rango combinado.
         *
         * O
         *
         * 2. Alguna de las águilas ya estaba
         *    peleando individualmente.
         */
        return
            distanciaJugador <=
            radioActivacionJefe ||

            aguila.CombateActivo ||

            pareja.CombateActivo;
    }

    // =========================================================
    // ACTIVAR MODO JEFE
    // =========================================================

    private void ActivarModoJefe()
    {
        modoJefeActivo =
            true;

        aguila.ForzarModoJefeDuo(
            true
        );

        pareja.ForzarModoJefeDuo(
            true
        );

        EntrarCooldown();

        Debug.Log(
            "¡Las dos águilas entraron en modo dúo!"
        );
    }

    // =========================================================
    // DESACTIVAR MODO JEFE
    // =========================================================

    private void DesactivarModoJefe()
    {
        modoJefeActivo =
            false;

        if (aguila != null)
        {
            aguila.ForzarModoJefeDuo(
                false
            );
        }

        if (pareja != null)
        {
            pareja.ForzarModoJefeDuo(
                false
            );
        }

        estadoPatron =
            EstadoPatron.Inactivo;
    }

    // =========================================================
    // COOLDOWN
    // =========================================================

    private void EntrarCooldown()
    {
        estadoPatron =
            EstadoPatron.Cooldown;

        timerCooldown =
            Random.Range(
                cooldownMin,
                cooldownMax
            );
    }

    private void ActualizarCooldown()
    {
        /*
         * Esperamos a que ambas estén libres
         * antes de comenzar otro patrón.
         */
        if (!AmbasDisponibles())
            return;

        timerCooldown -=
            Time.fixedDeltaTime;

        if (timerCooldown > 0f)
            return;

        // =====================================================
        // ELEGIR PATRÓN
        // =====================================================

        float roll =
            Random.value;

        if (roll < 0.5f)
        {
            IniciarPinza();
        }
        else
        {
            IniciarSuperGiro();
        }
    }

    // =========================================================
    // DISPONIBILIDAD
    // =========================================================

    private bool AmbasDisponibles()
    {
        return
            aguila != null &&
            pareja != null &&
            aguila.PuedeSerDirigidaPorPareja &&
            pareja.PuedeSerDirigidaPorPareja;
    }

    // =========================================================
    // =========================================================
    // PATRÓN PINZA
    // =========================================================
    // =========================================================

    private void IniciarPinza()
    {
        estadoPatron =
            EstadoPatron.Pinza;

        // =====================================================
        // ELEGIR ROLES
        // =====================================================

        if (Random.value < 0.5f)
        {
            atacantePinza =
                aguila;

            hostigadorPinza =
                pareja;
        }
        else
        {
            atacantePinza =
                pareja;

            hostigadorPinza =
                aguila;
        }

        // =====================================================
        // ATACANTE
        // =====================================================

        Vector2 destinoAtaque =
            PredecirJugador(
                0.18f
            );

        /*
         * false:
         *
         * Este NO es el ataque especial
         * que ignora la evasión.
         *
         * La pinza respeta el sistema normal:
         *
         * esquiva
         * recibe
         * esquiva
         * recibe...
         */
        atacantePinza.IniciarLanzamientoDuo(
            destinoAtaque,
            velocidadAtaquePinza,
            false
        );

        // =====================================================
        // HOSTIGADOR
        // =====================================================

        float ladoAtaque =
            Mathf.Sign(
                atacantePinza
                    .transform.position.x -
                jugador.position.x
            );

        if (ladoAtaque == 0f)
        {
            ladoAtaque =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        /*
         * Si el atacante viene desde un lado,
         * el hostigador busca el contrario.
         */
        float ladoOpuesto =
            -ladoAtaque;

        Vector2 destinoHostigamiento =
            new Vector2(
                jugador.position.x +
                ladoOpuesto *
                distanciaHostigamiento,

                hostigadorPinza.AlturaBaseY
            );

        hostigadorPinza.IniciarHostigamientoDuo(
            destinoHostigamiento,
            velocidadHostigamiento
        );

        timerHostigamiento =
            duracionHostigamiento;
    }

    // =========================================================
    // ACTUALIZAR PINZA
    // =========================================================

    private void ActualizarPinza()
    {
        if (atacantePinza == null ||
            hostigadorPinza == null)
        {
            EntrarCooldown();

            return;
        }

        timerHostigamiento -=
            Time.fixedDeltaTime;

        /*
         * Cuando termina el tiempo de presión,
         * el hostigador abandona su posición
         * y regresa a su altura.
         */
        if (timerHostigamiento <= 0f &&
            hostigadorPinza.EstaBajoControlDuo)
        {
            hostigadorPinza
                .SalirDeControlDuo();
        }

        /*
         * Cuando ambas terminaron completamente
         * sus partes del patrón, continuamos.
         */
        if (!atacantePinza.EstaBajoControlDuo &&
            !hostigadorPinza.EstaBajoControlDuo)
        {
            EntrarCooldown();
        }
    }

    // =========================================================
    // =========================================================
    // SUPERGIRO
    // =========================================================
    // =========================================================

    private void IniciarSuperGiro()
    {
        estadoPatron =
            EstadoPatron.SuperGiro;

        salidaAguilaA =
            false;

        salidaAguilaB =
            false;

        // =====================================================
        // VUELTAS ALEATORIAS
        // =====================================================

        /*
         * Cada águila recibe SU PROPIO
         * número de vueltas.
         *
         * Ejemplo:
         *
         * Águila A = 1
         * Águila B = 5
         *
         * A saldrá mucho antes mientras
         * B continúa girando.
         */

        int vueltasA =
            Random.Range(
                vueltasMinimas,
                vueltasMaximas + 1
            );

        int vueltasB =
            Random.Range(
                vueltasMinimas,
                vueltasMaximas + 1
            );

        // =====================================================
        // CENTRO DEL SUPERGIRO
        // =====================================================

        /*
         * ESTA ES LA PARTE IMPORTANTE DEL CAMBIO.
         *
         * Antes:
         *
         * centro = jugador.position
         *
         * Y además se actualizaba mientras
         * el jugador caminaba.
         *
         * Ahora:
         *
         * X = posición horizontal del jugador
         *     AL INICIAR el ritual.
         *
         * Y = altura normal de las águilas.
         *
         * Así las dos montan el Supergiro
         * EN EL CIELO.
         */

        float centroX =
            jugador.position.x;

        float centroY =
            (
                aguila.AlturaBaseY +
                pareja.AlturaBaseY
            ) *
            0.5f;

        centroSuperGiro =
            new Vector2(
                centroX,
                centroY
            );

        // =====================================================
        // ÁGUILA A
        // =====================================================

        /*
         * Águila A comienza a la DERECHA
         * de la circunferencia.
         *
         *               A
         *        ●──────🦅
         *
         *             0°
         */

        aguila.IniciarOrbitaDuo(
            centroSuperGiro,
            radioOrbita,
            velocidadAngular,
            0f,
            vueltasA
        );

        // =====================================================
        // ÁGUILA B
        // =====================================================

        /*
         * Águila B comienza exactamente
         * en el lado contrario.
         *
         *        🦅──────●
         *        B
         *
         *        180°
         */

        pareja.IniciarOrbitaDuo(
            centroSuperGiro,
            radioOrbita,
            velocidadAngular,
            180f,
            vueltasB
        );

        Debug.Log(
            $"SUPERGIRO iniciado - " +
            $"A: {vueltasA} vuelta(s), " +
            $"B: {vueltasB} vuelta(s)."
        );
    }

    // =========================================================
    // ACTUALIZAR SUPERGIRO
    // =========================================================

    private void ActualizarSuperGiro()
    {
        /*
         * MUY IMPORTANTE:
         *
         * NO hacemos:
         *
         * centro = jugador.position
         *
         * El centro queda congelado en el cielo
         * durante todo este ataque.
         *
         * El jugador puede correr debajo.
         */

        // =====================================================
        // MANTENER ÁGUILA A EN LA ÓRBITA
        // =====================================================

        if (!salidaAguilaA &&
            aguila.EstaEnOrbitaDuo)
        {
            aguila.ActualizarCentroOrbitaDuo(
                centroSuperGiro
            );
        }

        // =====================================================
        // MANTENER ÁGUILA B EN LA ÓRBITA
        // =====================================================

        if (!salidaAguilaB &&
            pareja.EstaEnOrbitaDuo)
        {
            pareja.ActualizarCentroOrbitaDuo(
                centroSuperGiro
            );
        }

        // =====================================================
        // ÁGUILA A TERMINÓ
        // =====================================================

        if (!salidaAguilaA &&
            aguila.TerminoVueltasDuo)
        {
            salidaAguilaA =
                true;

            /*
             * AHORA sí miramos dónde está
             * actualmente el jugador.
             *
             * No importa dónde estaba cuando
             * comenzó el giro.
             */
            Vector2 objetivo =
                PredecirJugador(
                    0.15f
                );

            /*
             * true:
             *
             * La salida del Supergiro
             * SIEMPRE es vulnerable.
             *
             * Ignora temporalmente el turno
             * de evasión.
             */
            aguila.IniciarLanzamientoDuo(
                objetivo,
                velocidadSalidaSupergiro,
                true
            );
        }

        // =====================================================
        // ÁGUILA B TERMINÓ
        // =====================================================

        if (!salidaAguilaB &&
            pareja.TerminoVueltasDuo)
        {
            salidaAguilaB =
                true;

            Vector2 objetivo =
                PredecirJugador(
                    0.15f
                );

            pareja.IniciarLanzamientoDuo(
                objetivo,
                velocidadSalidaSupergiro,
                true
            );
        }

        // =====================================================
        // AMBAS TERMINARON TODO
        // =====================================================

        if (salidaAguilaA &&
            salidaAguilaB &&
            !aguila.EstaBajoControlDuo &&
            !pareja.EstaBajoControlDuo)
        {
            EntrarCooldown();
        }
    }

    // =========================================================
    // PREDECIR JUGADOR
    // =========================================================

    private Vector2 PredecirJugador(
        float tiempo)
    {
        Rigidbody2D rbJugador =
            jugador.GetComponent<Rigidbody2D>();

        Vector2 basePos =
            jugador.position;

        if (rbJugador == null)
        {
            return basePos;
        }

        return
            basePos +
            rbJugador.velocity *
            tiempo;
    }

    // =========================================================
    // IGNORAR COLISIONES ENTRE ÁGUILAS
    // =========================================================

    private void IgnorarColisionesConPareja()
    {
        Collider2D[] misColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] susColliders =
            parejaCoord
                .GetComponentsInChildren<Collider2D>();

        foreach (
            Collider2D mio
            in misColliders)
        {
            foreach (
                Collider2D suyo
                in susColliders)
            {
                if (mio != null &&
                    suyo != null)
                {
                    Physics2D.IgnoreCollision(
                        mio,
                        suyo,
                        true
                    );
                }
            }
        }

        colisionesIgnoradas =
            true;
    }
}