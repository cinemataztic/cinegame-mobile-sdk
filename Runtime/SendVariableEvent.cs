using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Just a class for passing through values and attatching a key to them. Add more methods as needed.
/// </summary>
public class SendVariableEvent : MonoBehaviour
{
    [System.Serializable]
    public class SendVariable : UnityEvent<string, int> { }

    [SerializeField] string key;
    [SerializeField] SendVariable sendEvent;

    public void SendEvent(int value)
    {
        sendEvent?.Invoke(key, value);
    }
}
