using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle : MonoBehaviour {

    private Vertex[] points = new Vertex[3];

    public void formTriangle (Vertex _a, Vertex _b, Vertex _c) {
        points[0] = _a;
        points[1] = _b;
        points[2] = _c;
    }

    public Vertex[] getVertices () {
        return points;
    }
}
