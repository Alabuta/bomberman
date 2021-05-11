using System.Collections;
using Audio;
using Configs.Effects;
using UnityEngine;
using UnityEngine.Audio;

namespace Effects
{
    [CreateAssetMenu(menuName = "Effects/Destroy Effect")]
    public class DestroyEffect : DestructionSequence
    {
        private const string AudioPlayerName = "DestroyEffectAudioPlayer";

        [SerializeField]
        private DestroyEffectConfig Config;

        [SerializeField]
        public AudioMixerGroup AudioMixerGroup;

        protected override IEnumerator SequenceCoroutine(MonoBehaviour runner)
        {
            var transform = runner.transform;

            if (Config.Effect != null)
                Instantiate(Config.Effect, transform.position, transform.rotation);

            if (Config.AudioEvent != null)
            {
                var audioPlayer = new GameObject(AudioPlayerName, typeof(AudioSource));

                var audioSource = audioPlayer.GetComponent<AudioSource>();
                audioSource.transform.position = transform.position;
                audioSource.outputAudioMixerGroup = AudioMixerGroup;

                Config.AudioEvent.Play(audioSource);

                Destroy(audioPlayer);
            }

            yield return new WaitForSeconds(Config.DestroyAfterTimeSec);

            Destroy(runner.gameObject);
        }
    }
}
