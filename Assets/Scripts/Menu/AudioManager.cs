using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Music")]
    public AudioClip musicaMenu;
    public AudioClip musicaJuego;

    [Header("Sonidos UI")]
    public AudioClip sonidoMover;
    public AudioClip sonidoSeleccionar;
    public AudioClip sonidoVolver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CargarVolumenes();
        ReproducirMusicaMenu();
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

    private void ReproducirSFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void CambiarVolumenMusica(float volumen)
    {
        musicSource.volume = volumen;

        PlayerPrefs.SetFloat("VolumenMusica", volumen);
        PlayerPrefs.Save();
    }

    public void CambiarVolumenSFX(float volumen)
    {
        sfxSource.volume = volumen;

        PlayerPrefs.SetFloat("VolumenSFX", volumen);
        PlayerPrefs.Save();
    }

    private void CargarVolumenes()
    {
        float volumenMusica =
            PlayerPrefs.GetFloat("VolumenMusica", 0.7f);

        float volumenSFX =
            PlayerPrefs.GetFloat("VolumenSFX", 1f);

        if (musicSource != null)
            musicSource.volume = volumenMusica;

        if (sfxSource != null)
            sfxSource.volume = volumenSFX;
    }

    public float ObtenerVolumenMusica()
    {
        return PlayerPrefs.GetFloat("VolumenMusica", 0.7f);
    }

    public float ObtenerVolumenSFX()
    {
        return PlayerPrefs.GetFloat("VolumenSFX", 1f);
    }
}