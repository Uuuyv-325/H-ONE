using System;
using UnityEngine;

namespace HOne.Game
{
    public abstract class BoolSignal : MonoBehaviour
    {
        public bool Value { get; private set; }
        public event Action<bool> Changed;
        protected void SetValue(bool value)
        {
            if (Value == value) return;
            Value = value;
            Changed?.Invoke(value);
        }
    }
}
