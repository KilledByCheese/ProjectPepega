using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class UpdateableData : ScriptableObject {

    public event System.Action OnValuesUpdated;

    public bool autoUpdate;

    protected virtual void OnValidate() {
        if(autoUpdate) {
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
        }
    }

    public void NotifyOfUpdatedValues() {
        if(OnValuesUpdated != null) {
            UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
            OnValuesUpdated();
        }
    }        
}
