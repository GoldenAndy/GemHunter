using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip musicaMenu;
    [SerializeField] private AudioClip musicaJuego;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip sonidoMover;
    [SerializeField] private AudioClip sonidoSeleccionar;
    [SerializeField] private AudioClip sonidoVolver;

    private const string MUSIC_KEY = "VolumenMusica";
    private const string SFX_KEY = "VolumenSFX";

    private float volumenMusica = 0.7f;
    private float volumenSFX = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CargarVolumenes();
        AplicarVolumenes();
    }

    public void ReproducirMusicaMenu()
    {
        ReproducirMusica(musicaMenu);
    }

    public void ReproducirMusicaJuego()
    {
        ReproducirMusica(musicaJuego);
    }

    private void ReproducirMusica(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void SonidoMover()
    {
        ReproducirSFX(sonidoMover);
    }

    public void SonidoSeleccionar()
    {
        ReproducirSFX(sonidoSeleccionar);
    }

    public void SonidoVolver()
    {
        ReproducirSFX(sonidoVolver);
    }

    public void ReproducirSFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void CambiarVolumenMusica(float valor)
    {
        volumenMusica = Mathf.Clamp01(valor);

        if (musicSource != null)
            musicSource.volume = volumenMusica;

        PlayerPrefs.SetFloat(MUSIC_KEY, volumenMusica);
        PlayerPrefs.Save();
    }

    public void CambiarVolumenSFX(float valor)
    {
        volumenSFX = Mathf.Clamp01(valor);

        if (sfxSource != null)
            sfxSource.volume = volumenSFX;

        PlayerPrefs.SetFloat(SFX_KEY, volumenSFX);
        PlayerPrefs.Save();
    }

    public float ObtenerVolumenMusica()
    {
        return volumenMusica;
    }

    public float ObtenerVolumenSFX()
    {
        return volumenSFX;
    }

    private void CargarVolumenes()
    {
        volumenMusica = PlayerPrefs.GetFloat(MUSIC_KEY, 0.7f);
        volumenSFX = PlayerPrefs.GetFloat(SFX_KEY, 1f);
    }

    private void AplicarVolumenes()
    {
        if (musicSource != null)
            musicSource.volume = volumenMusica;

        if (sfxSource != null)
            sfxSource.volume = volumenSFX;
    }
}