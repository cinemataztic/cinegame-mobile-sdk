using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CineGame.MobileComponents;
using Sfs2X.Entities.Data;
using UnityEngine;
using UnityEngine.Events;

public class TimingComponent : ReplicatedComponent
{
    [Serializable]
    public class SyncEvent : UnityEvent<float> { }
    [Serializable]
    public class SyncStringEvent : UnityEvent<string> { }

    [SerializeField] int syncAttempts = 10;
    [SerializeField] int maxSyncRetries = 3;
    [SerializeField] float timeoutDelay = 4f;
    [SerializeField] string syncKey = "sync";
    [SerializeField] UnityEvent syncFailedEvent;
    [SerializeField] SyncEvent syncEvent;
    [SerializeField] SyncStringEvent syncStringEvent;

    private bool syncReceived;
    private int syncTries = 0;
    private float syncOffset;
    private float timeout;
    private List<float[]> syncList;

    public void Synchronize()
    {
        syncList.Clear();
        StartCoroutine(SynchronizeRoutine());
    }

    public void SendKey(string key)
    {
        Send(key, Time.realtimeSinceStartup + syncOffset);
    }

    public void SendInt(string key, int value)
    {
        float[] data = new float[] { (float)value, Time.realtimeSinceStartup + syncOffset };
        Send(key, data);
    }

    private void Start()
    {
        syncList = new List<float[]>();
    }

    internal override void OnObjectMessage(ISFSObject dataObj, int senderId)
    {
        if (dataObj.ContainsKey(syncKey))
        {
            float[] timestamps = dataObj.GetFloatArray(syncKey);
            OnSyncReceived(timestamps);
        }
    }

    private void OnSyncReceived(float[] timestamps)
    {
        Array.Resize(ref timestamps, 4);
        timestamps[3] = Time.realtimeSinceStartup;
        syncList.Add(timestamps);
        syncReceived = true;
    }

    private IEnumerator SynchronizeRoutine()
    {
        for (int i = 0; i < syncAttempts; i++)
        {
            Debug.Log("Sending sync request at " + Time.realtimeSinceStartup);
            Send(syncKey, Time.realtimeSinceStartup);
            timeout = 0f;
            yield return new WaitUntil(() => syncReceived || CheckTimeout());
            syncReceived = false;
            if (timeout >= timeoutDelay)
            {
                syncTries++;
                if (syncTries >= maxSyncRetries)
                {
                    Debug.LogError("Couldn't sync, failed after " + syncTries + " retries.");
                    syncFailedEvent?.Invoke();
                    yield break;
                }
                Debug.LogWarning("Timeout after " + timeoutDelay + " seconds while synchronizing, retrying");
                StartCoroutine(SynchronizeRoutine());
                yield break;
            }
        }

        syncTries = 0;
        float[] offsets = GetOffsets(syncList);
        List<float> offsetList = RemoveOutliers(offsets.ToList());
        float[] roundTrips = GetRoundtrips(syncList);
        syncOffset = offsetList.Sum() / offsetList.Count;

        syncEvent?.Invoke(syncOffset);
        syncStringEvent?.Invoke(syncOffset.ToString());
    }

    private bool CheckTimeout()
    {
        timeout += Time.deltaTime;
        return timeout >= timeoutDelay;
    }

    private float[] GetOffsets(List<float[]> syncList)
    {
        float[] offsets = new float[syncList.Count];
        for (int i = 0; i < syncList.Count; i++)
        {
            offsets[i] = ((syncList[i][1] - syncList[i][0]) + (syncList[i][2] - syncList[i][3])) / 2f;
        }
        return offsets;
    }

    private float[] GetRoundtrips(List<float[]> syncList)
    {
        float[] roundTrips = new float[syncList.Count];
        for (int i = 0; i < syncList.Count; i++)
        {
            roundTrips[i] = (syncList[i][3] - syncList[i][0]) - (syncList[i][2] - syncList[i][1]);
        }
        return roundTrips;
    }

    private List<float> RemoveOutliers(List<float> offsets)
    {
        offsets.Sort();
        int halfOffsets = offsets.Count / 2;
        int oddOffset = offsets.Count % 2 == 0 ? 0 : 1;

        float[] leftOffsets = new float[halfOffsets];
        float[] rightOffsets = new float[halfOffsets];
        for (int i = 0; i < halfOffsets; i++)
        {
            leftOffsets[i] = offsets[i];
            rightOffsets[i] = offsets[i + halfOffsets + oddOffset];
        }

        oddOffset = leftOffsets.Length % 2 == 0 ? 0 : 1;
        float leftMedian = leftOffsets[leftOffsets.Length / 2 + oddOffset];
        float rightMedian = rightOffsets[rightOffsets.Length / 2 + oddOffset];

        float iqr = rightMedian - leftMedian; //interquartile range
        float mean = offsets.Sum() / offsets.Count;

        List<float> newSet = new List<float>();
        for (int i = 0; i < offsets.Count; i++)
        {
            if (Mathf.Abs(mean - offsets[i]) < 1.5f * iqr)
            {
                newSet.Add(offsets[i]);
            }
        }

        if (newSet.Count > 3 && offsets.Count != newSet.Count) //Check if we removed any outliers. If not, the set is good to go
        {
            return RemoveOutliers(newSet);
        }
        return newSet;
    }
}
