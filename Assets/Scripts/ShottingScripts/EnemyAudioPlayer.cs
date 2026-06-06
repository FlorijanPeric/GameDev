using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudioPlayer : MonoBehaviour
{
    public AudioClip footstepClip;
    public AudioClip attackClip;
    public AudioClip hurtClip;
    public AudioClip deathClip;

    [Range(0f,1f)] public float footstepVolume = 0.6f;
    [Range(0f,1f)] public float attackVolume = 0.9f;
    [Range(0f,1f)] public float hurtVolume = 0.9f;
    [Range(0f,1f)] public float deathVolume = 1f;

    private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 30f;
    }

    public void PlayFootstep()
    {
        if (footstepClip == null) return;
        src.PlayOneShot(footstepClip, footstepVolume);
    }

    public void PlayAttack()
    {
        if (attackClip == null) return;
        src.PlayOneShot(attackClip, attackVolume);
    }

    public void PlayHurt()
    {
        if (hurtClip == null) return;
        src.PlayOneShot(hurtClip, hurtVolume);
    }

    public void PlayDeath()
    {
        if (deathClip == null) return;
        src.PlayOneShot(deathClip, deathVolume);
    }
}
