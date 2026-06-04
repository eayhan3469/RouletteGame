using System.Collections;
using UnityEngine;

/// <summary>
/// Plays the configured win particle sequence.
/// </summary>
[DisallowMultipleComponent]
public sealed class RouletteWinVfxController : MonoBehaviour
{
    [SerializeField]
    [Min(0f)]
    private float _explosionInterval = 0.4f;

    [SerializeField]
    private ParticleSystem _firstExplosion;

    [SerializeField]
    private ParticleSystem _secondExplosion;

    [SerializeField]
    private ParticleSystem _thirdExplosion;

    [SerializeField]
    private ParticleSystem _waterfall;

    [SerializeField]
    private AudioSource _waterfallChipsAudioSource;

    private Coroutine _playRoutine;

    private void Awake()
    {
        StopAndClear();
    }

    public void PlayWinSequence()
    {
        StopAndClear();
        _playRoutine = StartCoroutine(PlayWinSequenceRoutine());
    }

    public void StopAndClear()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        StopParticle(_firstExplosion);
        StopParticle(_secondExplosion);
        StopParticle(_thirdExplosion);
        StopParticle(_waterfall);
    }

    private IEnumerator PlayWinSequenceRoutine()
    {
        PlayParticle(_firstExplosion);
        yield return new WaitForSeconds(_explosionInterval);

        PlayParticle(_secondExplosion);
        yield return new WaitForSeconds(_explosionInterval);

        PlayParticle(_thirdExplosion);
        yield return new WaitForSeconds(_explosionInterval);

        PlayParticle(_waterfall);
        if (_waterfallChipsAudioSource != null)
        {
            _waterfallChipsAudioSource.Play();
        }
        
        _playRoutine = null;
    }

    private void PlayParticle(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        particleSystem.gameObject.SetActive(true);
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystem.Play(true);
    }

    private void StopParticle(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
