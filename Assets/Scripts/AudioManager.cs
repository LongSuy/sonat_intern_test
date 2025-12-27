using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour 
{
    public static AudioManager Instance { get; private set; }
    
    public AudioClip bottleClose;
    public AudioClip bottleDown;
    public AudioClip bottleUp;
    public AudioClip bottleFull;

    public AudioClip winClip;
    public AudioClip loseClip;

    private AudioSource _source;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _source = GetComponent<AudioSource>();
        Debug.Log($"[AudioManager] Awake, source = {_source}");

        DontDestroyOnLoad(gameObject);
    }

    public void PlaySelect() { PlayOneShot(bottleClose); }
    public void PlayInvalid() { PlayOneShot(bottleClose); }
    public void PlayPourStart() { PlayOneShot(bottleDown); }
    public void PlayPourEnd() { PlayOneShot(bottleUp); }
    public void PlayWin() { PlayOneShot(bottleFull); }
    public void PlayLose() { PlayOneShot(bottleDown); }

    private void PlayOneShot(AudioClip clip)
    {
        if(clip == null)
        {
            Debug.LogWarning($"[AudioManager] {tag}: clip is null");
            return;
        }
        if(_source == null)
        {
            Debug.LogError($"[AudioManager] {tag}: AudioSource is null");
            return;
        }

        _source.PlayOneShot(clip);
        Debug.Log($"[AudioManager] PlayOneShot: {clip.name} ({tag})");
    }
}
