using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sabresaurus.SabreCSG;
public class MeshCutter
{

    public TempMesh PositiveMesh { get; private set; }
    public TempMesh NegativeMesh { get; private set; }

    private List<Vector3> addedPairs;
    private List<BoneWeight> addedBoneWeights;

    private readonly List<Vector3> ogVertices;
    private readonly List<int> ogTriangles;
    private readonly List<Vector3> ogNormals;
    private readonly List<Vector2> ogUvs;
    private readonly List<BoneWeight> ogBoneWeights;

    private readonly Vector3[] intersectPair;
    private readonly Vector3[] tempTriangle;

    private Intersections intersect;

    private readonly float threshold = 1e-6f;

    public MeshCutter(int initialArraySize)
    {
        PositiveMesh = new TempMesh(initialArraySize);
        NegativeMesh = new TempMesh(initialArraySize);

        addedPairs = new List<Vector3>(initialArraySize);
        addedBoneWeights = new List<BoneWeight>(initialArraySize);
        ogVertices = new List<Vector3>(initialArraySize);
        ogNormals = new List<Vector3>(initialArraySize);
        ogUvs = new List<Vector2>(initialArraySize);
        ogBoneWeights = new List<BoneWeight>(initialArraySize);
        ogTriangles = new List<int>(initialArraySize * 3);

        intersectPair = new Vector3[2];
        tempTriangle = new Vector3[3];

        intersect = new Intersections();
    }

    /// <summary>
    /// Slice a mesh by the slice plane.
    /// We assume the plane is already in the mesh's local coordinate frame
    /// Returns posMesh and negMesh, which are the resuling meshes on both sides of the plane 
    /// (posMesh on the same side as the plane's normal, negMesh on the opposite side)
    /// </summary>
    public bool SliceMesh(Mesh mesh, ref Plane slice)
    {

        // Let's always fill the vertices array so that we can access it even if the mesh didn't intersect
        mesh.GetVertices(ogVertices);

        // 1. Verify if the bounds intersect first
        if (!Intersections.BoundPlaneIntersect(mesh, ref slice))
            return false;

        mesh.GetTriangles(ogTriangles, 0);
        mesh.GetNormals(ogNormals);
        mesh.GetUVs(0, ogUvs);
        mesh.GetBoneWeights(ogBoneWeights);
        
        Matrix4x4[] bindposes = mesh.bindposes;
        
        PositiveMesh.Clear();
        NegativeMesh.Clear();
        addedPairs.Clear();
        addedBoneWeights.Clear();

        PositiveMesh.SetBindposes(bindposes);
        NegativeMesh.SetBindposes(bindposes);

        // 2. Separate old vertices in new meshes
        for (int i = 0; i < ogVertices.Count; ++i)
        {
            if (slice.GetDistanceToPoint(ogVertices[i]) >= 0)
                PositiveMesh.AddVertex(ogVertices, ogNormals, ogUvs, ogBoneWeights, i);
            else
                NegativeMesh.AddVertex(ogVertices, ogNormals, ogUvs, ogBoneWeights, i);
        }

        // 2.5 : If one of the mesh has no vertices, then it doesn't intersect
        if (NegativeMesh.vertices.Count == 0 || PositiveMesh.vertices.Count == 0)
            return false;

        // 3. Separate triangles and cut those that intersect the plane
        for (int i = 0; i < ogTriangles.Count; i += 3)
        {
            if (intersect.TrianglePlaneIntersect(ogVertices, 
                                                 ogUvs,
                                                 ogBoneWeights,
                                                 ogTriangles,
                                                 i,
                                                 ref slice, PositiveMesh, NegativeMesh, intersectPair))
            {
                addedPairs.AddRange(intersectPair);

                addedBoneWeights.Add(ogBoneWeights[ogTriangles[i]]);
            }
        }

        if (addedPairs.Count > 0)
        {
            //FillBoundaryGeneral(addedPairs);
            DetectFacesAndFill(addedPairs, addedBoneWeights);
            //FillBoundaryFace(addedPairs);
            return true;
        }
        else
        {
            throw new UnityException("Error: if added pairs is empty, we should have returned false earlier");
        }
    }

    /// <summary>
    /// This is not meant to be efficient, it's a proof of concept of supporting multiple faces that result from a split
    /// </summary>
    void DetectFacesAndFill(List<Vector3> addedPairs, List<BoneWeight> addedBoneWeights)
    {
        // Turn the list of vertex pairs into a list of edges
        List<Edge> edges = new List<Edge>();
        for (int i = 0; i < addedPairs.Count; i += 2)
        {
            edges.Add(new Edge(addedPairs[i], addedPairs[i + 1], addedBoneWeights[i/2]));
        }

        // Compute adjacency of the edges
        for (int i = 0; i < edges.Count; i++)
        {
            var sourceEdge = edges[i];

            // Compute A adjacency first
            for (int j = 0; j < edges.Count; j++)
            {
                if (i == j) continue;

                var targetEdge = edges[j];

                if (targetEdge.B == sourceEdge.A)
                {
                    sourceEdge.EdgeAdjacentToA = targetEdge;
                    break;
                }
                else if(targetEdge.A == sourceEdge.A)
                {
                    targetEdge.Flip(); // Target edge matches but is wrong way round, so flip it
                    sourceEdge.EdgeAdjacentToA = targetEdge;
                    break;
                }
            }

            // Compute B adjacency first
            for (int j = 0; j < edges.Count; j++)
            {
                if (i == j) continue;

                var targetEdge = edges[j];
                if (targetEdge.A == sourceEdge.B)
                {
                    sourceEdge.EdgeAdjacentToB = targetEdge;
                    break;
                }
                else if (targetEdge.B == sourceEdge.B)
                {
                    targetEdge.Flip(); // Target edge matches but is wrong way round, so flip it
                    sourceEdge.EdgeAdjacentToB = targetEdge;
                    break;
                }
            }

            if(sourceEdge.EdgeAdjacentToA == null || sourceEdge.EdgeAdjacentToB == null)
            {
                Debug.LogError("Could not compute adjacency for an edge");
                return;
            }
        }

        // Calculate islands
        List<List<Edge>> islands = new List<List<Edge>>();
        for (int i = 0; i < edges.Count; i++)
        {
            List<Edge> island = new List<Edge>();

            Edge sourceEdge = edges[0];
            Edge activeEdge = sourceEdge;
            do
            {
                island.Add(activeEdge);
                edges.Remove(activeEdge);
                activeEdge = activeEdge.EdgeAdjacentToB;
            }
            while (activeEdge != sourceEdge);
            if(island.Count > 0)
                islands.Add(island);
        }

        foreach (List<Edge> island in islands)
        {
            BoneWeight boneWeight = island[0].BoneWeight;
            List<Vector3> vertices = new List<Vector3>(island.Count);
            foreach (Edge pair in island)
            {
                vertices.Add(pair.A);
            }

            Vector3 center = Vector3.zero;
            foreach (Vector3 vertex in vertices)
            {
                center += vertex;
            }
            center /= vertices.Count;

            //Create triangle for each edge to the center
            tempTriangle[2] = center;

            for (int i = 0; i < vertices.Count; i ++)
            {
                // Add fronface triangle in meshPositive
                tempTriangle[0] = vertices[i];
                tempTriangle[1] = vertices[(i + 1) % vertices.Count];

                PositiveMesh.AddTriangle(tempTriangle, boneWeight);

                // Add backface triangle in meshNegative
                tempTriangle[0] = vertices[(i + 1) % vertices.Count];
                tempTriangle[1] = vertices[i];

                NegativeMesh.AddTriangle(tempTriangle, boneWeight);
            }
        }
    }

    public Vector3 GetFirstVertex()
    {
        if (ogVertices.Count == 0)
            throw new UnityException(
                "Error: Either the mesh has no vertices or GetFirstVertex was called before SliceMesh.");
        else
            return ogVertices[0];
    }

    #region Boundary fill method


    //private void FillBoundaryGeneral(List<Vector3> added)
    //{
    //    // 1. Reorder added so in order ot their occurence along the perimeter.
    //    MeshUtils.ReorderList(added);

    //    Vector3 center = MeshUtils.FindCenter(added);

    //    //Create triangle for each edge to the center
    //    tempTriangle[2] = center;

    //    for (int i = 0; i < added.Count; i += 2)
    //    {
    //        // Add fronface triangle in meshPositive
    //        tempTriangle[0] = added[i];
    //        tempTriangle[1] = added[i + 1];

    //        PositiveMesh.AddTriangle(tempTriangle);

    //        // Add backface triangle in meshNegative
    //        tempTriangle[0] = added[i + 1];
    //        tempTriangle[1] = added[i];

    //        NegativeMesh.AddTriangle(tempTriangle);
    //    }
    //}


    //private void FillBoundaryFace(List<Vector3> added)
    //{
    //    // 1. Reorder added so in order of their occurence along the perimeter.
    //    MeshUtils.ReorderList(added);

    //    // 2. Find actual face vertices
    //    var face = FindRealPolygon(added);

    //    // 3. Create triangle fans
    //    int t_fwd = 0,
    //        t_bwd = face.Count - 1,
    //        t_new = 1;
    //    bool incr_fwd = true;

    //    while (t_new != t_fwd && t_new != t_bwd)
    //    {
    //        AddTriangle(face, t_bwd, t_fwd, t_new);

    //        if (incr_fwd) t_fwd = t_new;
    //        else t_bwd = t_new;

    //        incr_fwd = !incr_fwd;
    //        t_new = incr_fwd ? t_fwd + 1 : t_bwd - 1;
    //    }
    //}

    ///// <summary>
    ///// Extract polygon from the pairs of vertices.
    ///// Per example, two vectors that are colinear is redundant and only forms one side of the polygon
    ///// </summary>
    //private List<Vector3> FindRealPolygon(List<Vector3> pairs)
    //{
    //    List<Vector3> vertices = new List<Vector3>();
    //    Vector3 edge1, edge2;

    //    // List should be ordered in the correct way
    //    for (int i = 0; i < pairs.Count; i += 2)
    //    {
    //        edge1 = (pairs[i + 1] - pairs[i]);
    //        if (i == pairs.Count - 2)
    //            edge2 = pairs[1] - pairs[0];
    //        else
    //            edge2 = pairs[i + 3] - pairs[i + 2];

    //        // Normalize edges
    //        edge1.Normalize();
    //        edge2.Normalize();

    //        if (Vector3.Angle(edge1, edge2) > threshold)
    //            // This is a corner
    //            vertices.Add(pairs[i + 1]);
    //    }

    //    return vertices;
    //}

    //private void AddTriangle(List<Vector3> face, int t1, int t2, int t3)
    //{
    //    tempTriangle[0] = face[t1];
    //    tempTriangle[1] = face[t2];
    //    tempTriangle[2] = face[t3];
    //    PositiveMesh.AddTriangle(tempTriangle);

    //    tempTriangle[1] = face[t3];
    //    tempTriangle[2] = face[t2];
    //    NegativeMesh.AddTriangle(tempTriangle);
    //}
    #endregion

}

