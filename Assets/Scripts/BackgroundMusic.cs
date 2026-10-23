using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BackgroundMusic : MonoBehaviour
{
    private const float NormalVolume = 0.25f;
    private const float BossVolume = 0.4f;

    private AudioSource _source;
    private AudioClip _ambient;
    private AudioClip _boss;
    private Coroutine _fade;

    private void Start()
    {
        _ambient = Resources.Load<AudioClip>("Audio/FantasyAmbient");
        _boss = Resources.Load<AudioClip>("Audio/BossBattle");
        if (_ambient == null)
        {
            Debug.LogWarning("Musique FantasyAmbient introuvable.", this);
            return;
        }
        _source = gameObject.AddComponent<AudioSource>();
        _source.clip = _ambient;
        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.volume = NormalVolume;
        _source.ignoreListenerVolume = false;
        _source.Play();
    }

    public void PlayBossMusic() => CrossFadeTo(_boss, BossVolume);
    public void PlayAmbientMusic() => CrossFadeTo(_ambient, NormalVolume);

    private void CrossFadeTo(AudioClip clip, float volume)
    {
        if (_source == null || clip == null || _source.clip == clip) return;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(Fade(clip, volume));
    }

    private IEnumerator Fade(AudioClip clip, float volume)
    {
        for (float start = _source.volume, t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
        {
            _source.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        _source.clip = clip;
        _source.Play();
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.8f)
        {
            _source.volume = Mathf.Lerp(0f, volume, t);
            yield return null;
        }
        _source.volume = volume;
        _fade = null;
    }
}
