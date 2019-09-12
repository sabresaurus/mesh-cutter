using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poser : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer source;
    // Start is called before the first frame update
    void Start()
    {
    	Mesh mesh = new Mesh();
        source.BakeMesh(mesh);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
