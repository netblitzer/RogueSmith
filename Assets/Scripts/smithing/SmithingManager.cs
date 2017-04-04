using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Resources;

public class SmithingManager : MonoBehaviour {

    private List<Vertex> vertices; // Stores all the vertices

    public GameObject handlePrefab; // Default handle for the vertices
    private GameObject vertexPointer; // Object that acts as the pointer

    private DebugLines debug;
    private Vector2 projPoint;

    public Mesh weapon;
    public int[] tris;
    public Vector2[] uvs;
    public Vector3[] verts;

    private string mode = "none";

    private Vertex moving;  // the point currently being moved


    // Use this for initialization
    void Start() {
        vertices = new List<Vertex>(5);
        //closestPoints = new List<GameObject>(5);
        vertexPointer = GameObject.Instantiate(handlePrefab);
        vertexPointer.name = "Pointer";

        debug = GameObject.Find("SmithingManager").GetComponent<DebugLines>();

        projPoint = new Vector2();

        moving = null;

        weapon = new Mesh();
        weapon.vertices = verts;
        weapon.uv = uvs;
        weapon.triangles = tris;
    }

    // Update is called once per frame
    void Update() {
        // Scoped variable declarations
        bool linkCorrection = false; // If true, will correct the links between vertices. Acts like a master switch.

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        #region Input Checks
        /// This area checks all inputs, starting with keyboard inputs first and moving to the mouse inputs.

        bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool spaceDown = Input.GetKey(KeyCode.Space);
        bool specialDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftCommand);
        bool controlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        foreach (Vertex vt in vertices) {
            vt.gameObject.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
            vt.mode = 0;
        }


        {
            // Closure for switch statement variables that are created and used
            Vertex vx;
            float closest = float.MaxValue;
            int selected = -1;
            int lineCheck = -1;
            
            // move the "cursor"
            Vertex vp = vertexPointer.GetComponent<Vertex>();

            vp.setVertexLocation(mousePos);

            switch (mode) {

                case "adding":
                    #region Adding
                    vertexPointer.GetComponent<SpriteRenderer>().color = new Color(0, 255, 0, 0.5f);

                    // see which line is closest to the mouse
                    for (int i = 0; i < vertices.Count; i++) {
                        if (vertices[i].PreviousVertex != null) {

                            // get distance to line
                            float dist = distanceFromLine(vertices[i].PreviousVertex.VertexLocation, vertices[i].VertexLocation, mousePos, closest);
                            // compare to current closest
                            if (dist < closest) {

                                // set new closest
                                lineCheck = i;
                                closest = dist;
                            }
                        }
                    }

                    // draw lines
                    if (lineCheck > -1) {

                        // create perpendicular line
                        debug.addLine(mousePos, projPoint, 3);
                        debug.addLine(mousePos, vertices[lineCheck].VertexLocation, 2);
                        debug.addLine(mousePos, vertices[lineCheck].PreviousVertex.VertexLocation, 2);

                        //Debug.Log("Distance: " + closestLineDist);
                    }

                    // add point if mouse is clicked
                    if (Input.GetKeyDown(KeyCode.Mouse0)) {
                        GameObject v = GameObject.Instantiate(handlePrefab);
                        vx = v.GetComponent<Vertex>();

                        // fail state if we can't create the point for some reason
                        if (!vx.setVertexLocation(mousePos)) {
                            GameObject.Destroy(v);
                        }
                        else if (lineCheck > -1) {
                            linkCorrection = true;

                            // counter-clockwise correction for only two vertices
                            if (vertices.Count < 3) {

                                // Checks to make sure the vertices are added in counter-clockwise pattern

                                Vector2 lineVec = (vertices[0].PreviousVertex.VertexLocation - vertices[0].VertexLocation).normalized;
                                lineVec = new Vector2(lineVec.y, -lineVec.x);

                                Vector2 adjMouse = mousePos - vertices[0].VertexLocation;
                                float dir = Vector2.Dot(adjMouse, lineVec);

                                if (dir > 0) {
                                    vertices.Insert(2, vx);
                                }
                                else {
                                    vertices.Insert(1, vx);
                                }
                            }
                            else {
                                // if there's more than three vertices, it will already be in C-CW order
                                vertices.Insert(lineCheck, vx);
                            }
                        }
                        else {
                            linkCorrection = true;

                            vertices.Insert(0, vx);
                        }
                    }
                    break;
                #endregion
                case "removing":
                    #region Removing
                    vertexPointer.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0, 0.5f);

                    closest = 20f;  // only allow selection of points 20f away

                    // find closest point to remove
                    foreach (Vertex v in vertices) {
                        float dist = Vector2.Distance(mousePos, v.VertexLocation);

                        if (dist < closest) {

                            selected = v.ID;

                            closest = dist;
                        }
                    }

                    // draw line between points
                    if (selected > -1 && vertices.Count > 1) {
                        debug.addLine(vertices[selected].PreviousVertex.VertexLocation, vertices[selected].NextVertex.VertexLocation, 1);
                        vertices[selected].GetComponentInParent<SpriteRenderer>().color = new Color(255, 0, 0, 1f);
                    }

                    // remove point if the mouse is clicked
                    if (Input.GetKeyDown(KeyCode.Mouse0)) {

                        if (selected > -1) {
                            linkCorrection = true;
                            vertices[selected].PreviousVertex.setNextVertex(null);
                            vertices[selected].NextVertex.setPreviousVertex(null);
                            vertices[selected].selfDestruct();
                            vertices.RemoveAt(selected);
                        }

                    }
                    break;
                #endregion
                case "moving":
                    #region Moving
                    vertexPointer.GetComponent<SpriteRenderer>().color = new Color(0, 0, 255, 0.5f);

                    // find the vertex to move while we're not clicking
                    if (!Input.GetKey(KeyCode.Mouse0)) {

                        // reset the moving vertex
                        if (moving != null) {
                            moving = null;
                        }

                        closest = 20f;  // only allow selection of points 20f away

                        // find closest point to remove
                        foreach (Vertex v in vertices) {
                            float dist = Vector2.Distance(mousePos, v.VertexLocation);

                            if (dist < closest) {

                                selected = v.ID;

                                closest = dist;
                            }
                        }

                        Debug.Log(selected);

                        vertices[selected].GetComponentInParent<SpriteRenderer>().color = new Color(100, 100, 255, 1f);
                    }
                    // move the point around while mouse is clicked
                    else {

                        if (moving == null) {
                            if (selected > -1) {
                                moving = vertices[selected];
                            }
                        }

                        if (moving != null) {
                            moving.gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                            moving.setVertexLocation(mousePos);
                        }

                    }

                    #endregion
                    break;
                default:
                    vertexPointer.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255, 0.5f);
                    break;
            }
        }

        #endregion

        // Make sure the links are all in place correctly
        if (linkCorrection) {
            correctVertexLinks();
            Debug.Log("Correcting links");
        }

        updateMesh();

        foreach (Vertex v in vertices) {
            if (v.PreviousVertex != null) {
                Debug.DrawLine(v.gameObject.transform.position, v.PreviousVertex.gameObject.transform.position, Color.red);

                //line.SetPosition(v.ID, v.gameObject.transform.position);
                debug.addLine(v.gameObject.transform.position, v.PreviousVertex.gameObject.transform.position, 0);
            }
        }

    }

    /// <summary>
    /// Corrects the ordering of the vertex links to order them in a counter-clockwise order.
    /// </summary>
    void correctVertexLinks() {

        if (vertices.Count > 1) {

            for (int i = 0; i < vertices.Count; i++) {
                if (vertices[i].ID != i)
                    vertices[i].setID(i);

                vertices[i].name = "Vertex #" + i;
            }
            
            for (int i = 0; i < vertices.Count; i++) {

                // Next vertex check
                if (i == vertices.Count - 1) {
                    if (vertices[i].NextVertex == null || vertices[vertices.Count - 1].NextVertex.ID != 0)
                        vertices[vertices.Count - 1].setNextVertex(vertices[0]);
                }
                else {
                    if (vertices[i].NextVertex == null || vertices[i].NextVertex.ID != i + 1)
                        vertices[i].setNextVertex(vertices[i + 1]);
                }

                // Previous vertex check
                if (i == 0) {
                    if (vertices[i].PreviousVertex == null || vertices[0].PreviousVertex.ID != vertices.Count - 1)
                        vertices[0].setPreviousVertex(vertices[vertices.Count - 1]);
                }
                else {
                    if (vertices[i].PreviousVertex == null || vertices[i].PreviousVertex.ID != i - 1)
                        vertices[i].setPreviousVertex(vertices[i - 1]);
                }

            }
        }
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
            } else {
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

    /// <summary>
    /// Check to see if the two lines are crossing. The first line is between p1 and p2, second line is between p3 and p4.
    /// </summary>
    /// <param name="_p1">Start of the fist line.</param>
    /// <param name="_p2">End of the first line.</param>
    /// <param name="_p3">Start of the second line.</param>
    /// <param name="_p4">End of the second line.</param>
    /// <returns>Returns true if the lines cross. Returns false if they don't cross, the lines are identical, or if a line is just a point.</returns>
    bool lineCrossingCheck(Vector2 _p1, Vector2 _p2, Vector2 _p3, Vector2 _p4) {

        bool crossed = false;

        // check to see if either line is just a point
        if (_p1 == _p2 || _p3 == _p4)
            return false;

        // check to see if the lines are identical
        if (_p1 == _p3 && _p2 == _p4)
            return true;

        // check for crossing
        bool sign;
            // check for if line 2 crosses line 1
        sign = (((_p3.x - _p1.x) * (_p2.y - _p1.y) - (_p3.y - _p1.y) * (_p2.x - _p1.x)) > 0f);
        if (((_p4.x - _p1.x) * (_p2.y - _p1.y) - (_p4.y - _p1.y) * (_p2.x - _p1.x)) > 0f) {

            // check if line 1 crosses line 2
            sign = (((_p1.x - _p3.x) * (_p4.y - _p3.y) - (_p1.y - _p3.y) * (_p4.x - _p3.x)) > 0f);
            if (((_p1.x - _p3.x) * (_p4.y - _p3.y) - (_p1.y - _p3.y) * (_p4.x - _p3.x)) > 0f)
                crossed = true;
        }

        return crossed;
    }

    /// <summary>
    /// A function to switch the mode of the manager with button presses.
    /// </summary>
    /// <param name="_s">The string to switch the mode to.</param>
    public void functionSwitch(string _s) {
        if (mode == _s) {
            mode = "none";
        } else {
            mode = _s;
        }
        Debug.Log("Mode is now in: " + mode);
    }
}
