using UnityEngine;

namespace CineGame.MobileComponents {
    public class HapticPatternAsset : ScriptableObject {
        [SerializeField, HideInInspector]
        private string ahapJson;

        public string AhapJson => ahapJson;

        public void Initialize (string jsonContent) {
            ahapJson = jsonContent;
        }
    }
}
