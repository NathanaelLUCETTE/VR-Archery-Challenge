using UnityEngine;

[DisallowMultipleComponent]
public class BackgroundMusic : MonoBehaviour
{
    private void Start()
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/FantasyAmbient");
        if (clip == null)
        {
            Debug.LogWarning("Musique FantasyAmbient introuvable.", this);
            return;
        }
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.25f;
        source.ignoreListenerVolume = false;
        source.Play();
    }
}
