#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class MacTrackpad {
    public enum MacTouchPhase {
        Began,
        Moved,
        Stationary,
        Ended,
        Cancelled
    };

    public class Touch : IComparable<Touch> {
        public Vector2 deltaPosition;
        public float deltaTime;
        public int fingerId;
        public MacTouchPhase phase;
        public Vector2 position;
        public int tapCount;
        public float time;

        public int CompareTo (Touch other) {
            return (int)(time - other.time * 1000000f);
        }
    }

    public class Device {
        public int id;
        public Vector2 size;
        public int touchCount;
        public Touch [] touches;

        public Device (int id, Vector2 size) {
            this.id = id;
            this.size = size;
            touchCount = 0;
            touches = new Touch [0];
        }
    }

    static public int maxQueuedEvents = 128;

    static public Device [] devices {
        get {
            return deviceList.ToArray ();
        }
    }

    static public int deviceCount {
        get {
            return deviceList.Count;
        }
    }

    static protected int initState;
    static protected float lastPolled;

    static protected Dictionary<int, Device> devicesById =
      new Dictionary<int, Device> ();

    static protected Dictionary<int, List<Touch>> deviceTouches =
      new Dictionary<int, List<Touch>> ();

    static protected Dictionary<int, int> deviceTouchCount =
      new Dictionary<int, int> ();

    static protected List<Device> deviceList =
      new List<Device> ();

    protected enum ExtPhase {
        EXT_PHASE_BEGAN = 1 << 0,
        EXT_PHASE_MOVED = 1 << 1,
        EXT_PHASE_STATIONARY = 1 << 2,
        EXT_PHASE_ENDED = 1 << 3,
        EXT_PHASE_CANCELLED = 1 << 4,
        EXT_PHASE_TOUCHING = EXT_PHASE_BEGAN | EXT_PHASE_MOVED | EXT_PHASE_STATIONARY,
        EXT_PHASE_ANY = Int32.MaxValue
    };

    [StructLayout (LayoutKind.Sequential)]
    protected struct ExtTouch {
        public int id;
        public int deviceId;
        public ExtPhase phase;
        public int isResting;
        public float deviceWidth;
        public float deviceHeight;
        public float normalizedPosX;
        public float normalizedPosY;
        public IntPtr next;
    }

    [StructLayout (LayoutKind.Sequential)]
    protected struct ExtEvent {
        public float time;
        public int touchCount;
        public IntPtr touchesTail;
        public IntPtr touchesHead;
        public IntPtr next;
    }

    [StructLayout (LayoutKind.Sequential)]
    protected struct ExtBuffer {
        public float time_start;
        public int maxEvents;
        public int eventCount;
        public IntPtr tail;
        public IntPtr head;
    }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    [DllImport ("MacTrackpad")]
    static protected extern int ExtInit (int maxQueuedEvents);

    [DllImport ("MacTrackpad")]
    static protected extern int ExtClear ();

    [DllImport ("MacTrackpad")]
    static protected extern IntPtr ExtGetBuffer ();
#endif

    static public void Poll () {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        float deltaTime;
        IntPtr extBufferPtr;
        IntPtr extEventPtr;
        IntPtr extTouchPtr;
        ExtBuffer extBuffer;
        ExtEvent extEvent;
        ExtTouch extTouch;
        Device dev;
        List<Touch> touches;
        int stray;
        Dictionary<int, int> newDeviceTouchCount;

        if (initState == 0) {
            if (!(Application.platform == RuntimePlatform.OSXEditor ||
                  Application.platform == RuntimePlatform.OSXPlayer))
                initState = 3;
            else if (ExtInit (maxQueuedEvents) == 0) {
                initState = 1;
                devicesById = new Dictionary<int, Device> ();
                deviceTouches = new Dictionary<int, List<Touch>> ();
                deviceTouchCount = new Dictionary<int, int> ();
                deviceList = new List<Device> ();
            } else
                initState = 2;
        }

        if (initState == 1) {
            deltaTime = Time.time - lastPolled;

            /* cleanup/alter old (one-frame) touches */

            foreach (List<Touch> touchList in deviceTouches.Values) {
                touchList.RemoveAll (x =>
                   x.phase == MacTouchPhase.Ended ||
                   x.phase == MacTouchPhase.Cancelled);

                foreach (Touch t in touchList) {
                    if (t.phase == MacTouchPhase.Began || t.phase == MacTouchPhase.Moved)
                        t.phase = MacTouchPhase.Stationary;

                    t.deltaPosition = Vector2.zero;
                    t.deltaTime = 0;
                }
            }

            /* create temp new touch counts */

            newDeviceTouchCount = new Dictionary<int, int> ();

            /* consume new touches */

            extBufferPtr = ExtGetBuffer ();

            if (extBufferPtr != IntPtr.Zero) {
                extBuffer = (ExtBuffer)Marshal.PtrToStructure (extBufferPtr, typeof (ExtBuffer));
                if (extBuffer.eventCount > 0) {
                    extEventPtr = extBuffer.tail;
                    while (extEventPtr != IntPtr.Zero) {
                        extEvent = (ExtEvent)Marshal.PtrToStructure (extEventPtr, typeof (ExtEvent));
                        if (extEvent.touchCount > 0) {
                            extTouchPtr = extEvent.touchesTail;
                            while (extTouchPtr != IntPtr.Zero) {
                                extTouch = (ExtTouch)Marshal.PtrToStructure (extTouchPtr, typeof (ExtTouch));
                                if (!newDeviceTouchCount.ContainsKey (extTouch.deviceId))
                                    newDeviceTouchCount [extTouch.deviceId] = 0;
                                newDeviceTouchCount [extTouch.deviceId]++;
                                HandleExtTouch (extTouch, extEvent.time);
                                extTouchPtr = extTouch.next;
                            }
                        }
                        extEventPtr = extEvent.next;
                    }
                }
            }

            foreach (KeyValuePair<int, List<Touch>> kv in deviceTouches) {
                /* remove stray touches */
                touches = new List<Touch> (deviceTouches [kv.Key]);
                touches.Sort ();
                stray = touches.Count -
                  (newDeviceTouchCount.ContainsKey (kv.Key) ? newDeviceTouchCount [kv.Key] : touches.Count);
                if (stray < 0)
                    stray = 0;
                for (int i = 0; i < stray; i++)
                    touches [i].phase = MacTouchPhase.Ended;

                /* hand-set delta times for persistent touches */
                foreach (Touch t in touches) {
                    if (t.deltaTime == 0)
                        t.deltaTime = deltaTime;
                }

                /* update device list */
                dev = devicesById [kv.Key];
                dev.touchCount = kv.Value.Count;
                dev.touches = kv.Value.ToArray ();
            }

            ExtClear ();

            lastPolled = Time.time;
        }
#endif
    }

    static protected void HandleExtTouch (ExtTouch extTouch, float time) {
        Vector2 position, deviceSize;
        Device dev;
        List<Touch> touches;
        Touch touch;

        if (!devicesById.ContainsKey (extTouch.deviceId)) {
            deviceSize = new Vector2 (extTouch.deviceWidth, extTouch.deviceHeight);
            touches = new List<Touch> ();
            dev = new Device (extTouch.deviceId, deviceSize);
            devicesById.Add (dev.id, dev);
            deviceTouches.Add (dev.id, touches);
            deviceList.Add (dev);
        } else {
            dev = devicesById [extTouch.deviceId];
            touches = deviceTouches [extTouch.deviceId];
            deviceSize = dev.size;
        }

        if (!deviceTouchCount.ContainsKey (extTouch.deviceId))
            deviceTouchCount.Add (extTouch.deviceId, 0);
        deviceTouchCount [extTouch.deviceId]++;

        touch = touches.Find (x => x.fingerId == extTouch.id);
        position = new Vector2 (extTouch.normalizedPosX, extTouch.normalizedPosY);

        if (touch == null) {
            touch = new Touch ();
            touches.Add (touch);

            touch.deltaPosition = Vector2.zero;
            touch.deltaTime = 0;
            touch.fingerId = extTouch.id;
            touch.phase = MacTouchPhase.Began;
            touch.position = position;
            touch.tapCount = 1;
            touch.time = time;
        } else {
            touch.deltaTime = touch.deltaTime + (time - touch.time);
            touch.time = time;
            touch.deltaPosition = touch.deltaPosition + (position - touch.position);
            touch.position = position;
        }

        switch (extTouch.phase) {
        case ExtPhase.EXT_PHASE_BEGAN:
            touch.phase = MacTouchPhase.Began;
            break;

        case ExtPhase.EXT_PHASE_MOVED:
            touch.phase = MacTouchPhase.Moved;
            break;

        case ExtPhase.EXT_PHASE_STATIONARY:
            touch.phase = MacTouchPhase.Stationary;
            break;

        case ExtPhase.EXT_PHASE_ENDED:
            touch.phase = MacTouchPhase.Ended;
            break;

        case ExtPhase.EXT_PHASE_CANCELLED:
            touch.phase = MacTouchPhase.Cancelled;
            break;
        }
    }
}
#endif
