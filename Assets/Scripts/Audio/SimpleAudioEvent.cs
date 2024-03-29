﻿using System.Linq;
using Core.Attributes;
using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(fileName = "SimpleAudioEvent", menuName = "Configs/Audio/Simple Audio Event")]
    public class SimpleAudioEvent : AudioEvent
    {
        [SerializeField]
        private AudioClip[] AudioClips;

        [SerializeField]
        [RangeFloatAttribute(0, 1)]
        private RangeFloat Volume = new RangeFloat(0.5f, 0.75f);

        [SerializeField]
        [RangeFloatAttribute(0, 2)]
        private RangeFloat Pitch = new RangeFloat(0.5f, 1f);

        public override void Play(AudioSource source)
        {
            source.Stop();

            if (!AudioClips.Any())
                return;

            source.clip = AudioClips[Random.Range(0, AudioClips.Length)]; // :TODO: replace by RandomGenerator
            source.volume = Volume;
            source.pitch = Pitch;

            source.Play();
        }
    }
}
