using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class lineTesting : MonoBehaviour {

    public GameObject handlePrefab; // Default handle for the vertices
    public GameObject vertexPointer; // Object that acts as the pointer
    private Vertex vx;

    public GameObject V1, V2, V3;

    private DebugLines debug;
    private Vector2 projPoint;
    
    // Use this for initialization
    void Start() {
        //vertexPointer = GameObject.Instantiate(handlePrefab);
        vertexPointer.name = "Pointer";
        vertexPointer.GetComponent<Vertex>().setVertexLocation(vertexPointer.transform.position);

        vx = vertexPointer.GetComponent<Vertex>();
        Debug.Log(vertexPointer);

        debug = GameObject.Find("GameObject").GetComponent<DebugLines>();

        projPoint = new Vector2();

        V2.GetComponent<Vertex>().setPreviousVertex(V1.GetComponent<Vertex>());
        V1.GetComponent<Vertex>().setNextVertex(V2.GetComponent<Vertex>());
        V1.GetComponent<Vertex>().setVertexLocation(V1.transform.position);
        V2.GetComponent<Vertex>().setVertexLocation(V2.transform.position);
        V3.GetComponent<Vertex>().setVertexLocation(V3.transform.position);
    }

    // Update is called once per frame
    void Update() {
        // Scoped variable declarations

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        vertexPointer.GetComponent<Vertex>().setVertexLocation(mousePos);

        distanceFromLine(V1.GetComponent<Vertex>().VertexLocation, V2.GetComponent<Vertex>().VertexLocation, mousePos, 9999999999f);

        debug.addLine(mousePos, projPoint, 3);
        debug.addLine(V1.GetComponent<Vertex>().VertexLocation, V2.GetComponent<Vertex>().VertexLocation, 0);

        bool crossed = lineCrossingCheck(V1.transform.position, V2.transform.position, V3.transform.position, mousePos);

        if (crossed)
            debug.addLine(mousePos, V3.GetComponent<Vertex>().VertexLocation, 1);
        else
            debug.addLine(mousePos, V3.GetComponent<Vertex>().VertexLocation, 2);

    }
    
    /// <summary>
    /// Gets the distance of a point from a line described by the start point and end point.
    /// If the point is closer than the curMax, then the global projPoint will be replaced.
    /// </summary>
    /// <param name="_startPoint">Starting point of the line.</param>
    /// <param name="_endPoint">Ending point of the line.</param>
    /// <param name="_targetPoint">Point to test to the line.</param>
    /// <param name="curMax">Current maximum distance from the line. Used for comparison reasons.</param>
    /// <returns>Returns the distance from the line.</returns>
    float distanceFromLine(Vector2 _startPoint, Vector2 _endPoint, Vector2 _targetPoint, float curMax) {

        // calculate line vectors
        Vector2 lineVec = (_endPoint - _startPoint);
        float lineLength = lineVec.magnitude;
        lineVec.Normalize();
        Vector2 lineUpVec = new Vector2(lineVec.y, -lineVec.x);

        Vector2 targetVec = _targetPoint - _startPoint;

        // calculate distances in 2 directions from the line
        float upDist = Vector2.Dot(lineUpVec, targetVec);
        float lngDist = Vector2.Dot(lineVec, targetVec);

        float dist = 0;

        // behind start point
        if (lngDist < 0) {
            // check to see if the point is inside the shape but outside the segment
            if (upDist <= 0) {
                dist = targetVec.magnitude;
            }
            else {
                dist = float.MaxValue;
            }

            if (dist < curMax) {
                projPoint = _startPoint;
            }
        }
        // ahead of end point
        else if (lngDist > lineLength) {
            // check to see if the point is inside the shape but outside the segment
            if (upDist <= 0) {
                dist = (_targetPoint - _endPoint).magnitude;
            }
            else {
                dist = float.MaxValue;
            }

            if (dist < curMax) {
                projPoint = _endPoint;
            }
        }
        // point is along the line
        else {
            dist = Mathf.Abs(upDist);

            if (dist < curMax) {
                projPoint = _startPoint + (lineVec * lngDist);
            }
        }

        debug.addLine(_startPoint, _startPoint + (lineUpVec * 1), 4);
        debug.addLine(_endPoint, _endPoint + (lineUpVec * 1), 4);

        return dist;
    }
    /*
    void updateMesh() {
        weapon.Clear();
        verts = new Vector3[vertices.Count];

        for (int i = 0; i < vertices.Count; i++) {
            verts[i] = vertices[i].VertexLocation;
        }

        weapon.vertices = verts;
        weapon.uv = uvs;
        weapon.triangles = tris;
    }
    */
    bool lineCrossingCheck(Vector2 _p1, Vector2 _p2, Vector2 _p3, Vector2 _p4) {

        bool crossed = false;

        // check to see if either line is just a point
        if (_p1 == _p2 || _p3 == _p4)
            return false;

        // check to see if the lines are identical
        if (_p1 == _p3 && _p2 == _p4)
            return true;

        // check for crossing
        // check for if line 2 crosses line 1
        bool sign = (((_p3.x - _p1.x) * (_p2.y - _p1.y) - (_p3.y - _p1.y) * (_p2.x - _p1.x)) > 0f);
        bool secondSign = (((_p4.x - _p1.x) * (_p2.y - _p1.y) - (_p4.y - _p1.y) * (_p2.x - _p1.x)) > 0f);
        if (sign != secondSign) {

            // check if line 1 crosses line 2
            sign = (((_p1.x - _p3.x) * (_p4.y - _p3.y) - (_p1.y - _p3.y) * (_p4.x - _p3.x)) > 0f);
            secondSign = (((_p2.x - _p3.x) * (_p4.y - _p3.y) - (_p2.y - _p3.y) * (_p4.x - _p3.x)) > 0f);
            if (sign != secondSign)
                crossed = true;
        }

        return crossed;
    }
}
