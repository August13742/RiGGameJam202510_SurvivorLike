using Survivor.Weapon;
using System;
using UnityEngine;

public class MemoryMonitor : MonoBehaviour
{
    private int _lastFrameCount;
    private long _lastMemory;

    void Update()
    {
        if (Time.frameCount % 60 == 0) // Every second at 60FPS
        {
            long currentMemory = GC.GetTotalMemory(false);
            long diff = currentMemory - _lastMemory;

            if (Math.Abs(diff) > 1024 * 1024) // More than 1MB change
            {
                Debug.Log($"Memory change: {diff / (1024 * 1024)} MB over 60 frames");
            }

            _lastMemory = currentMemory;
        }
    }
}