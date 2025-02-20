using Unity.Netcode;
using UnityEngine;

public class GameAssets : NetworkBehaviour
{
    private static GameAssets instance;
    public static GameAssets Instance
    {
        get
        {
            if (instance == null) instance = new GameAssets();
            return instance;
        }
    }
    private void Awake()
    {
        instance = this;
        SoundManager.Initialize();
    }
    private void Start()
    {
        if (IsServer)
        {
            SoundManager.PlaySound(SoundManager.Sound.Theme);
        }
    }
    private void Update()
    {
        if (!IsServer) return;

        if (!SoundManager.audioSource.isPlaying)
        {
            SoundManager.PlaySound(SoundManager.Sound.Theme);
        }
    }

    public SoundAudioClip[] soundAudioClips;

    [System.Serializable]
    public class SoundAudioClip
    {
        public SoundManager.Sound sound;
        public AudioClip audioClip;
    }
}
