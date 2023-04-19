using System;
using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents
{
    public class JoystickComponent : MonoBehaviour, IGameComponentIcon
    {
        public VariableJoystick JoystickPrefab;
        public JoystickType Type = JoystickType.Fixed;
        public AxisOptions AxisOption = AxisOptions.Both;
        public AxisSnap SnapType = AxisSnap.None;
        public Sprite AxisSpriteBoth;
        public Sprite AxisSpriteHorizontal;
        public Sprite AxisSpriteVertical;

        public enum AxisSnap { None, X, Y }

        private void Awake()
        {
            SetAxisType(Type);
            SetAxisOptions(AxisOption);
            switch (SnapType)
            {
                case AxisSnap.None: SnapX(false); SnapY(false); break;
                case AxisSnap.X: SnapX(true); SnapY(false); break;
                case AxisSnap.Y: SnapX(false); SnapY(true); break;
            }
        }

        public void SetAxisType(JoystickType type) => JoystickPrefab.SetMode(type);

        public void SetAxisOptions(AxisOptions type)
        {
            Image background = JoystickPrefab.background.GetComponent<Image>();
            JoystickPrefab.AxisOptions = type;
            background.sprite = type switch
            {
                AxisOptions.Both => AxisSpriteBoth,
                AxisOptions.Horizontal => AxisSpriteHorizontal,
                AxisOptions.Vertical => AxisSpriteVertical,
                _ => throw new ArgumentException($"Unknown AxisOptions Type '{Enum.GetName(typeof(AxisOptions), type)}'"),
            };
        }

        public void SnapX(bool value) => JoystickPrefab.SnapX = value;
        public void SnapY(bool value) => JoystickPrefab.SnapY = value;
    }
}
