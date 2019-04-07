using UnityEngine;

public class Edge
{
    Vector3 a;
    Vector3 b;

    /// <summary>
    /// Adjacent edge to A
    /// </summary>
    public Edge EdgeAdjacentToA
    {
        get; set;
    }

    /// <summary>
    /// Adjacent edge to B
    /// </summary>
    public Edge EdgeAdjacentToB
    {
        get; set;
    }

    public Vector3 A
    {
        get
        {
            return a;
        }
    }

    public Vector3 B
    {
        get
        {
            return b;
        }
    }

    public Edge(Vector3 a, Vector3 b)
    {
        this.a = a;
        this.b = b;
    }

    /// <summary>
    /// Swap A and B around
    /// </summary>
    public void Flip()
    {
        var aCopy = a;
        a = b;
        b = aCopy;
    }
}