using System;
using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

    [Obsolete ("Deprecated. Use SendVariableComponent instead and hook it up with Slider's onValueChanged event")]
    public class SliderComponent : ReplicatedComponent {
        [Header ("Deprecated. Use SendVariableComponent instead and hook it up with Slider's onValueChanged event")]
        [Tooltip ("Name of the smartfox uservariable to replicate")]
        public string VariableName = "position";
        [Tooltip ("How often will the uservariable replicate")]
        public float UpdateInterval = .1f;
        [Tooltip ("Source slider. If left empty, will try to find component on this gameObject")]
        public Slider SourceSlider;

        float currentPosition = 0f;
        float prevPosition = 0f;
        float lastUpdateTime = 0f;

        void Update () {
            float t = Time.time;
            if (currentPosition != prevPosition && (lastUpdateTime + UpdateInterval) <= t) {
                Send (VariableName, currentPosition);
                lastUpdateTime = t;
                prevPosition = currentPosition;
            }
        }

        public void UpdatePosition (float position) {
            currentPosition = position;
        }

        /// <summary>
        /// In Unity 2018.4 it seems Unity changed the Slider Event to not include the normalizedValue as float argument. So we need to extract it manually
        /// </summary>
        public void UpdatePosition () {
            var ss = (SourceSlider != null) ? SourceSlider : GetComponent<Slider> ();
            currentPosition = ss.normalizedValue;
        }
    }

}
