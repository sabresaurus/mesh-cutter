using UnityEngine;
using System.Collections;

public class IllustrateHierarchy : MonoBehaviour {

	void OnDrawGizmosSelected()
    {
        DrawForTransform(transform);    
    }

    void DrawForTransform(Transform transformToProcess)
    {
        for (int i = 0; i < transformToProcess.childCount; i++)
        {
            Transform childTransform = transformToProcess.GetChild(i);
            Gizmos.DrawLine(transformToProcess.position, childTransform.position);
            DrawForTransform(childTransform);
        }
    }
}
