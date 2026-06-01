using UnityEngine;

/// <summary>
/// Centralized audio playback controller for roulette feedback sounds.
/// </summary>
[DisallowMultipleComponent]
public sealed class RouletteAudioFeedbackController : MonoBehaviour
{
    [Header("Mix")]
    [SerializeField]
    [Range(0f, 1f)]
    private float _masterVolume = 0.85f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _wheelLoopVolume = 0.28f;

    [SerializeField]
    [Range(0.1f, 3f)]
    private float _wheelLoopMinPitch = 0.7f;

    [SerializeField]
    [Range(0.1f, 3f)]
    private float _wheelLoopMaxPitch = 1.2f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _wheelLoopMinVolumeFactor = 0.35f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _ballLoopVolume = 0.24f;

    [SerializeField]
    [Range(0.1f, 3f)]
    private float _ballLoopMinPitch = 0.72f;

    [SerializeField]
    [Range(0.1f, 3f)]
    private float _ballLoopMaxPitch = 1.18f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _ballLoopMinVolumeFactor = 0.3f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _ballReleaseVolume = 0.42f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _ballPocketVolume = 0.58f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _chipPickupVolume = 0.22f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _chipDropVolume = 0.32f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _winResultVolume = 0.5f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _loseResultVolume = 0.38f;

    [SerializeField]
    [Range(0f, 1f)]
    private float _neutralResultVolume = 0.25f;

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip _wheelLoopClip;

    [SerializeField]
    private AudioClip _ballLoopClip;

    [SerializeField]
    private AudioClip _ballReleaseClip;

    [SerializeField]
    private AudioClip _ballPocketClip;

    [SerializeField]
    private AudioClip _chipPickupClip;

    [SerializeField]
    private AudioClip _chipDropClip;

    [SerializeField]
    private AudioClip _winResultClip;

    [SerializeField]
    private AudioClip _loseResultClip;

    [SerializeField]
    private AudioClip _neutralResultClip;

    private AudioSource _wheelLoopSource;
    private AudioSource _ballLoopSource;
    private AudioSource _sfxSource;
    private AudioSource _resultSource;

    private void Awake()
    {
        EnsureAudioSources();
    }

    public void PlayWheelSpinLoop()
    {
        EnsureAudioSources();

        if (_wheelLoopClip == null)
        {
            return;
        }

        _wheelLoopSource.clip = _wheelLoopClip;
        SetWheelSpinIntensity(1f);

        if (!_wheelLoopSource.isPlaying)
        {
            _wheelLoopSource.Play();
        }
    }

    public void StopWheelSpinLoop()
    {
        if (_wheelLoopSource != null && _wheelLoopSource.isPlaying)
        {
            _wheelLoopSource.Stop();
        }
    }

    public void SetWheelSpinIntensity(float normalizedIntensity)
    {
        if (_wheelLoopSource == null)
        {
            return;
        }

        float clampedIntensity = Mathf.Clamp01(normalizedIntensity);
        _wheelLoopSource.pitch = Mathf.Lerp(_wheelLoopMinPitch, _wheelLoopMaxPitch, clampedIntensity);

        float volumeFactor = Mathf.Lerp(_wheelLoopMinVolumeFactor, 1f, clampedIntensity);
        _wheelLoopSource.volume = _masterVolume * _wheelLoopVolume * volumeFactor;
    }

    public void PlayBallTravelLoop()
    {
        EnsureAudioSources();

        if (_ballLoopClip == null)
        {
            return;
        }

        _ballLoopSource.clip = _ballLoopClip;
        SetBallTravelIntensity(1f);

        if (!_ballLoopSource.isPlaying)
        {
            _ballLoopSource.Play();
        }
    }

    public void StopBallTravelLoop()
    {
        if (_ballLoopSource != null && _ballLoopSource.isPlaying)
        {
            _ballLoopSource.Stop();
        }
    }

    public void SetBallTravelIntensity(float normalizedIntensity)
    {
        if (_ballLoopSource == null)
        {
            return;
        }

        float clampedIntensity = Mathf.Clamp01(normalizedIntensity);
        _ballLoopSource.pitch = Mathf.Lerp(_ballLoopMinPitch, _ballLoopMaxPitch, clampedIntensity);

        float volumeFactor = Mathf.Lerp(_ballLoopMinVolumeFactor, 1f, clampedIntensity);
        _ballLoopSource.volume = _masterVolume * _ballLoopVolume * volumeFactor;
    }

    public void PlayBallRelease()
    {
        PlayOneShot(_sfxSource, _ballReleaseClip, _ballReleaseVolume);
    }

    public void PlayBallPocketLand()
    {
        PlayOneShot(_sfxSource, _ballPocketClip, _ballPocketVolume);
    }

    public void PlayChipPickup()
    {
        PlayOneShot(_sfxSource, _chipPickupClip, _chipPickupVolume);
    }

    public void PlayChipDrop()
    {
        PlayOneShot(_sfxSource, _chipDropClip, _chipDropVolume);
    }

    public void PlayRoundResult(float roundResult)
    {
        if (roundResult > 0f)
        {
            PlayOneShot(_resultSource, _winResultClip, _winResultVolume);
            return;
        }

        if (roundResult < 0f)
        {
            PlayOneShot(_resultSource, _loseResultClip, _loseResultVolume);
            return;
        }

        PlayOneShot(_resultSource, _neutralResultClip, _neutralResultVolume);
    }

    private void EnsureAudioSources()
    {
        if (_wheelLoopSource == null)
        {
            _wheelLoopSource = CreateAudioSource("WheelLoopSource", true, 96);
        }

        if (_ballLoopSource == null)
        {
            _ballLoopSource = CreateAudioSource("BallLoopSource", true, 98);
        }

        if (_sfxSource == null)
        {
            _sfxSource = CreateAudioSource("SfxSource", false, 110);
        }

        if (_resultSource == null)
        {
            _resultSource = CreateAudioSource("ResultSource", false, 105);
        }
    }

    private AudioSource CreateAudioSource(string sourceName, bool shouldLoop, int priority)
    {
        GameObject childObject = new GameObject(sourceName);
        childObject.transform.SetParent(transform, false);

        AudioSource audioSource = childObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = shouldLoop;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.priority = priority;
        return audioSource;
    }

    private void PlayOneShot(AudioSource audioSource, AudioClip audioClip, float localVolume)
    {
        if (audioSource == null || audioClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(audioClip, _masterVolume * localVolume);
    }
}
