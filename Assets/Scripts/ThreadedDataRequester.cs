using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour {

    static ThreadedDataRequester instance;

    private Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    void Awake() {
        instance = FindObjectOfType<ThreadedDataRequester>();
    }
    
    public static void RequestData(Func<object> generateData, Action<object> callback) {
        //textureData.UpdateMeshHeights(terrainMaterial, meshSettings.minHeight, meshSettings.maxHeight);
        ThreadStart threadStart = delegate {
           instance.DataThread(generateData, callback);
        };
        new Thread(threadStart).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback) { //Thread start delegate
        object data = generateData();
        lock(dataQueue) {
            dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    void Update() {
        if(dataQueue.Count > 0) {
            for(int i = 0; i < dataQueue.Count; i++) {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
       
    }
  
    struct ThreadInfo {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
