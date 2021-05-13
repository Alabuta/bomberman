using Audio;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(SimpleAudioEvent))]
    public class AudioEventEditor : UnityEditor.Editor
    {
        private GameObject _audioPlayer;
        private AudioSource _audioSource;
        private const string AudioPlayerName = "AudioPlayer";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!GUILayout.Button("Play"))
                return;

            var audioEvent = (SimpleAudioEvent) target;

            if (_audioPlayer == null)
            {
                _audioPlayer = new GameObject(AudioPlayerName, typeof(AudioSource)) {hideFlags = HideFlags.HideAndDontSave};

                _audioSource = _audioPlayer.GetComponent<AudioSource>();
            }

            audioEvent.Play(_audioSource);
        }
    }
}
