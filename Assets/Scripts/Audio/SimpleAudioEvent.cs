using System.Linq;
using Core.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audio
{
    [CreateAssetMenu(fileName = "SimpleAudioEvent", menuName = "Configs/Audio/Simple Audio Event")]
    public class SimpleAudioEvent : AudioEvent
    {
        [SerializeField]
        private AudioClip[] AudioClips;

        [SerializeField]
        private RangeFloat Volume = new RangeFloat(0, 10);

        [SerializeField]
        private RangeFloat Pitch = new RangeFloat(0, 10);

        public override void Play(AudioSource source)
        {
            if (!AudioClips.Any())
                return;

            source.clip = AudioClips[Random.Range(0, AudioClips.Length)];
            source.volume = Volume;
            source.pitch = Pitch;

            source.Play();
        }
    }
}
