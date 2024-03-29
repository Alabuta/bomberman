// ----------------------------------------------------------------------------
// The Proprietary or MIT-Red License
// Copyright (c) 2012-2022 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;

namespace Leopotam.Ecs.Ui.Components
{
    public struct EcsUiClickEvent
    {
        public string WidgetName;
        public GameObject Sender;
        public Vector2 Position;
        public PointerEventData.InputButton Button;
    }
}
