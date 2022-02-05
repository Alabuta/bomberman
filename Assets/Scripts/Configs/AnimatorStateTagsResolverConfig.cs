using System;
using Entity;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(menuName = "Configs/Animator State Tags Resolver", fileName = "AnimatorStateTagsResolver")]
    public sealed class AnimatorStateTagsResolverConfig : ConfigBase
    {
        [Serializable]
        public struct AnimatorStateTag
        {
            public string TagName;
            public AnimatorState State;
        }

        public AnimatorStateTag[] Tags;
    }
}
