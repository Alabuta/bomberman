using System.Collections.Generic;
using System.Linq;
using Configs;
using Entity;
using UnityEngine;

namespace Logic
{
    public class AnimationStateReporter : StateMachineBehaviour
    {
        private IAnimationStateReader _stateReader;

        [SerializeField]
        private AnimatorStateTagsResolverConfig TagsResolverConfig;

        private Dictionary<int, AnimatorState> _states;

        private void Awake()
        {
            _states = TagsResolverConfig.Tags.ToDictionary(p => Animator.StringToHash(p.TagName), p => p.State);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);

            FindReader(animator);

            var state = ResolveStateFromTagHash(stateInfo.tagHash);
            _stateReader.OnEnterState(state);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);

            FindReader(animator);

            var state = ResolveStateFromTagHash(stateInfo.tagHash);
            _stateReader.OnExitState(state);
        }

        private AnimatorState ResolveStateFromTagHash(int tagHash) =>
            _states.TryGetValue(tagHash, out var state) ? state : AnimatorState.Unknown;

        private void FindReader(Animator animator) =>
            _stateReader ??= animator.gameObject.GetComponent<IAnimationStateReader>();
    }
}
