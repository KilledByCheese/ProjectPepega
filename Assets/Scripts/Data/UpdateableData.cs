using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class UpdateableData : ScriptableObject {

    public event System.Action OnValuesUpdated;

    public bool autoUpdate;

    public void NotifyOfUpdatedValues() {
        if(OnValuesUpdated != null) {
            OnValuesUpdated();
        }
    }

    protected virtual void OnValidate() {
        if(autoUpdate) {
            NotifyOfUpdatedValues();
        }
    }
    
}
