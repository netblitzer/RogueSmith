using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Resources;

public class SmithingManager : MonoBehaviour {

    private List<Vertex> vertices; // Stores all the vertices
    private List<Vertex> innerVertices;

    public GameObject handlePrefab; // Default handle for the vertices
    private GameObject vertexPointer; // Object that acts as the pointer

    private DebugLines lines;
    private List<Line> polyCuts;
    private List<Line> linesToDraw;

    private Vector2 projPoint;

    public Mesh weapon;
    public int[] tris;
    public Vector2[] uvs;
    public Vector3[] verts;

    private string mode = "none";

    private Vertex moving;  // the point currently being moved
    private bool linkCorrection;


    // Use this for initialization
    void Start() {
        vertices = new List<Vertex>(5);
        innerVertices = new List<Vertex>(5);

        linesToDraw = new List<Line>();
        polyCuts = new List<Line>();

        vertexPointer = GameObject.Instantiate(handlePrefab);
        vertexPointer.name = "Pointer";

        lines = GameObject.Find("SmithingManager").GetComponent<DebugLines>();

        projPoint = new Vector2();

        moving = null;
        linkCorrection = false;
    }

    // Update is called once per frame
    void Update() {
        // Scoped variable declarations
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

        linkCorrection = false;

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
                        lines.addLine(mousePos, projPoint, 3);
                        lines.addLine(mousePos, vertices[lineCheck].VertexLocation, 2);
                        lines.addLine(mousePos, vertices[lineCheck].PreviousVertex.VertexLocation, 2);

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
                        lines.addLine(vertices[selected].PreviousVertex.VertexLocation, vertices[selected].NextVertex.VertexLocation, 1);
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

        //updateMesh();



        foreach (Vertex vt in vertices) {
            if (vt.nextVert != null)
                lines.addLine(vt.VertexLocation, vt.nextVert.VertexLocation, 0);
        }
    }

    #region Input Methods

    public void fieldClicked (Vector2 _pos) {
        Debug.Log(_pos);

        Vector2 pos = Camera.main.ScreenToWorldPoint(_pos);

        if (mode == "adding") {

            int lineCheck = -1;
            float closest = float.MaxValue;

            // see which line is closest to the mouse
            for (int i = 0; i < vertices.Count; i++) {
                if (vertices[i].PreviousVertex != null) {

                    // get distance to line
                    float dist = distanceFromLine(vertices[i].PreviousVertex.VertexLocation, vertices[i].VertexLocation, pos, closest);
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
                lines.addLine(pos, projPoint, 3);
                lines.addLine(pos, vertices[lineCheck].VertexLocation, 2);
                lines.addLine(pos, vertices[lineCheck].PreviousVertex.VertexLocation, 2);

                //Debug.Log("Distance: " + closestLineDist);
            }

            GameObject v = GameObject.Instantiate(handlePrefab);
            Vertex vx = v.GetComponent<Vertex>();

            // fail state if we can't create the point for some reason
            if (!vx.setVertexLocation(pos)) {
                GameObject.Destroy(v);
            }
            else if (lineCheck > -1) {
                linkCorrection = true;

                // counter-clockwise correction for only two vertices
                if (vertices.Count < 3) {

                    // Checks to make sure the vertices are added in counter-clockwise pattern

                    Vector2 lineVec = (vertices[0].PreviousVertex.VertexLocation - vertices[0].VertexLocation).normalized;
                    lineVec = new Vector2(lineVec.y, -lineVec.x);

                    Vector2 adjMouse = pos - vertices[0].VertexLocation;
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
    }



    #endregion

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

        lines.addLine(_startPoint, _startPoint + (lineUpVec * 1), 4);
        lines.addLine(_endPoint, _endPoint + (lineUpVec * 1), 4);

        return dist;
    }

    /// <summary>
    /// Returns the angle between two lines. Cannot go above 180 or below 0 (always chooses the positive value).
    /// </summary>
    /// <param name="_line1">The Vector2 representing the first line.</param>
    /// <param name="_line2">The Vector2 representing the second line.</param>
    /// <returns>The minimum angle between the two lines (0 to 180).</returns>
    float angleBetweenLines(Vector2 _line1, Vector2 _line2) {
        return 180f * Mathf.Acos((Vector2.Dot(_line1.normalized, _line2.normalized) / (_line1.normalized.magnitude * _line2.normalized.magnitude))) / Mathf.PI;
    }

    /// <summary>
    /// Returns the angle between two lines that form a corner and share a point. Can go over 180.
    /// </summary>
    /// <param name="_a">The point forming the first line between this point and _b.</param>
    /// <param name="_b">The shared point of the corner.</param>
    /// <param name="_c">The point forming the second line between itself and _b</param>
    /// <returns>The angle the corner is (can go over 180).</returns>
    float determinate(Vector2 _a, Vector2 _b, Vector2 _c) {
        return ((_b.x - _a.x) * (_c.y - _b.y) - (_c.x - _b.x) * (_b.y - _a.y));
    }

    /// <summary>
    /// Returns the bisecting Vector2 on a corner with a shared point.
    /// </summary>
    /// <param name="_a">The point forming the first line between this point and _b.</param>
    /// <param name="_b">The shared point of the corner.</param>
    /// <param name="_c">The point forming the second line between this point and _b.</param>
    /// <returns>The Vector2 representing the bisecting angle of the corner.</returns>
    Vector2 bisectAngle(Vector2 _a, Vector2 _b, Vector2 _c) {
        float det = determinate(_a, _b, _c);

        int flip = 1;
        if (det > 0) {
            flip = -1;
        }

        return (((_a - _b).normalized + (_c - _b).normalized).normalized * flip);
    }

    /// <summary>
    /// A function to get the cross product of two Vector2s.
    /// </summary>
    /// <param name="_v">The first Vector2.</param>
    /// <param name="_w">The second Vector2.</param>
    /// <returns>The cross product of the two Vector2s.</returns>
    float crossVec2(Vector2 _v, Vector2 _w) {
        return ((_v.x * _w.y) - (_v.y * _w.x));
    }

    /// <summary>
    /// A function to check if a Vector2 is within a triangle formed by _a, _b, and _c.
    /// </summary>
    /// <param name="_a">The first point on the triangle.</param>
    /// <param name="_b">The second point on the triangle.</param>
    /// <param name="_c">The third point on the triangle.</param>
    /// <param name="_d">The point to check if it's within the triangle.</param>
    /// <returns>Whether or not the point is in the triangle.</returns>
    bool pointInTriangle(Vector2 _a, Vector2 _b, Vector2 _c, Vector2 _d) {

        Vector2 lineVec = _b - _a;
        Vector2 targVec = _d - _a;
        float dot = lineVec.x * -targVec.y + lineVec.y * targVec.x;

        if (dot < 0) {
            return false;
        }

        lineVec = _c - _b;
        targVec = _d - _b;
        dot = lineVec.x * -targVec.y + lineVec.y * targVec.x;

        if (dot < 0) {
            return false;
        }

        lineVec = _a - _c;
        targVec = _d - _c;
        dot = lineVec.x * -targVec.y + lineVec.y * targVec.x;

        if (dot < 0) {
            return false;
        }

        return true;
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
    /// A function to find if the line segments formed by _p + _r and _q + _s cross at a point.
    /// </summary>
    /// <param name="_p">The starting point of the first line.</param>
    /// <param name="_r">The segment coming out of _p to form the first line.</param>
    /// <param name="_q">The starting point of the second line.</param>
    /// <param name="_s">The segment coming out of _q to form the second line.</param>
    /// <returns>The point where the two lines cross. If the return comes back with (-1, -1), the two lines do not cross.</returns>
    Vector2 pointOnLines(Vector2 _p, Vector2 _r, Vector2 _q, Vector2 _s) {

        // initialize variables
        Vector2 point = new Vector2(-1, -1);
        float t, u;

        // calcualte parts
        float cross = crossVec2(_r, _s);
        Vector2 mid = _q - _p;

        // find t and u where the two lines cross
        t = crossVec2(mid, _s);
        u = crossVec2(mid, _r);

        // find the crossing point or whether they don't cross
        if (cross == 0) {
            if (t == 0) {
                // lines are collinear
                return point;
            }
            else {
                // lines are parallel
                return point;
            }
        }
        else {
            t /= cross;
            u /= cross;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) {
                // lines cross
                point = _p + (_r * t);
                return point;
            }
            else {
                // lines would cross in the future if they continued on
                return point;
            }
        }

    }

    /// <summary>
    /// A function that adds a number of internal vertices in a polygon.
    /// Future upgrades should allow the vertices to be easily modified to be pushed further or closer to the shape's edge, and be able to cut off loops/areas when the distance becomes too great.
    /// Currently allows the points to go out of the shape if the origin point is too close to another edge.
    /// </summary>
    /// <returns>Returns true if it succeeds in adding the internal vertices.</returns>
    bool addInternalVertices() {

        // list of the normals from the center of every line
        // the end will be set to be where they would go out of the shape
        List<Line> lineNormals = new List<Line>();
        // list of the points where the normals cross
        List<Vector2> normalCrosses = new List<Vector2>();

        // find the normals
        for (int i = 0; i < vertices.Count; i++) {
            // find the midpoint and the normal
            Vector2 mid = (vertices[i].VertexLocation + vertices[i].NextVertex.VertexLocation) / 2;
            Vector2 lineVec = (vertices[i].NextVertex.VertexLocation - vertices[i].VertexLocation).normalized;
            Vector2 norm = new Vector2(lineVec.y, -lineVec.x);

            lineNormals.Add(new Line(mid, norm * 100, 1));

            //GameObject t = GameObject.Instantiate(vertPrefab);
            //t.transform.position = mid;
        }

        // float to see how far in we can push the shape
        float shortestSegment = float.MaxValue;

        // find where the normals cross the shape
        for (int i = 0; i < lineNormals.Count; i++) {

            normalCrosses.Add(Vector2.zero);
            float closest = float.MaxValue;

            for (int j = 0; j < vertices.Count; j++) {

                if (i == j) {
                    continue;
                }

                // find out if the lines cross at any point
                Vector2 point = pointOnLines(lineNormals[i].start, lineNormals[i].end, vertices[j].VertexLocation, vertices[j].NextVertex.VertexLocation - vertices[j].VertexLocation);


                if (point.x == -1 && point.y == -1) {
                    // lines don't cross
                    continue;
                }
                else {
                    // lines cross, see if it's a closer crossing point
                    float dist = ((Vector2)lineNormals[i].start - point).magnitude;
                    if (closest > dist) {
                        closest = dist;

                        // this is the shortest distance we can go in right now
                        if (closest < shortestSegment) {
                            shortestSegment = closest;
                        }

                        lineNormals[i].mode = 3;
                        normalCrosses[i] = point;
                    }
                }
            }

            lineNormals[i].end = normalCrosses[i];
        }

        // list of the inner lines
        List<Line> crossSegments = new List<Line>();
        // move the crossSegments out a distance from the line equal to the shortest segment / 2
        shortestSegment /= 4f;

        for (int i = 0; i < lineNormals.Count; i++) {
            Vector2 norm = (lineNormals[i].end - lineNormals[i].start).normalized;
            Vector2 right = new Vector2(norm.y, -norm.x);
            Vector2 _start = vertices[i].VertexLocation + (norm * shortestSegment) + (right * 10);
            Vector2 _end = vertices[i].NextVertex.VertexLocation + (norm * shortestSegment) + (right * -10);

            crossSegments.Add(new Line(_start, _end, 3));
        }

        int count = crossSegments.Count;
        // see where the lines cross and move the points
        for (int i = 0; i < count; i++) {

            // check for the new end vertex
            Vector2 point = pointOnLines(crossSegments[i].start, (crossSegments[i].end - crossSegments[i].start), crossSegments[(i + 1) % count].start, (crossSegments[(i + 1) % count].end - crossSegments[(i + 1) % count].start));

            if (point.x != -1 && point.y != -1) {
                crossSegments[i].end = point;
                crossSegments[(i + 1) % count].start = point;
            }

            // check for the new start vertex
            point = pointOnLines(crossSegments[i].start, (crossSegments[i].end - crossSegments[i].start), crossSegments[(i - 1 + count) % count].start, (crossSegments[(i - 1 + count) % count].end - crossSegments[(i - 1 + count) % count].start));
            //Debug.Log(point);

            if (point.x != -1 && point.y != -1) {
                crossSegments[i].start = point;
                crossSegments[(i - 1 + count) % count].end = point;
            }

            GameObject t = GameObject.Instantiate(handlePrefab);
            t.GetComponent<SpriteRenderer>().color = Color.red;
            Vertex v = t.GetComponent<Vertex>();
            innerVertices.Add(v);
        }

        for (int i = 0; i < lineNormals.Count; i++) {

            //linesToDraw.Add(lineNormals[i]);
            linesToDraw.Add(new Line(lineNormals[i].start, normalCrosses[i], 4));
            linesToDraw.Add(new Line(crossSegments[i].start, crossSegments[i].end, crossSegments[i].mode));

            innerVertices[i].setPreviousVertex(innerVertices[(i - 1 + innerVertices.Count) % innerVertices.Count]);
            innerVertices[i].setNextVertex(innerVertices[(i + 1) % innerVertices.Count]);
            innerVertices[i].setVertexLocation(crossSegments[i].start);
            innerVertices[i].gameObject.name = "Inner Vertex #" + i;
            innerVertices[i].setID(vertices[i].ID);
        }

        return true;
    }

    /// <summary>
    /// A function that triangulates the polygon through ear cutting and triangle strips.
    /// Currently can only handle one polygon with one internal "region" of space.
    /// Future upgrades should allow the shape to have multiple internal spaces and allow holes in the mesh.
    /// </summary>
    /// <returns>Returns true if it succeeds in triangulating the mesh.</returns>
    bool triangulatePolygon() {

        #region Ear Cutting

        // four arrays
        List<Vertex> poly = new List<Vertex>(innerVertices.Count);
        List<Vertex> reflex = new List<Vertex>(innerVertices.Count);
        List<Vertex> convex = new List<Vertex>(innerVertices.Count);
        List<Vertex> ears = new List<Vertex>(innerVertices.Count);

        // populate lists
        for (int i = 0; i < innerVertices.Count; i++) {

            // every vertex goes in
            poly.Add(innerVertices[i]);

            // find whether the angle is convex or reflex
            float det = determinate(innerVertices[(i - 1 + innerVertices.Count) % innerVertices.Count].VertexLocation, innerVertices[i].VertexLocation, innerVertices[(i + 1) % innerVertices.Count].VertexLocation);

            if (det < 0) {
                convex.Add(innerVertices[i]);
                innerVertices[i].GetComponentInParent<SpriteRenderer>().color = new Color(255, 0, 0, 1f);
            }
            else {
                reflex.Add(innerVertices[i]);
                innerVertices[i].GetComponentInParent<SpriteRenderer>().color = new Color(0, 255, 0, 1f);
            }
        }

        // find ears
        for (int i = 0; i < convex.Count; i++) {
            Vertex prev = poly[(convex[i].ID - 1 + poly.Count) % poly.Count];
            Vertex next = poly[(convex[i].ID + 1) % poly.Count];

            //Debug.Log("Prev: " + prev.ID + " Curr: " + convex[i].ID + " Next: " + next.ID);

            // if there's no reflex vertices, all of them are ears
            if (reflex.Count == 0) {
                ears.Add(convex[i]);
                convex[i].GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
            }
            else {
                bool isEar = true;
                for (int j = 0; j < reflex.Count; j++) {

                    // check to see if the reflex vertex is part of the triangle
                    if (reflex[j].ID == prev.ID || reflex[j].ID == next.ID) {
                        continue;
                    }

                    // check to see if the point is inside the triangle
                    if (pointInTriangle(prev.VertexLocation, convex[i].VertexLocation, next.VertexLocation, reflex[j].VertexLocation)) {
                        isEar = false;
                    }
                    else {
                        //Debug.Log("Point #" + reflex[j].ID + " not in the triangle");
                    }
                }

                if (isEar) {
                    ears.Add(convex[i]);
                    convex[i].GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                }
            }
        }

        // Debug Area
        string temp = "Convex: ";
        foreach (Vertex v in convex) {
            temp += v.ID + ", ";
        }
        //Debug.Log(temp);
        temp = "Reflex: ";
        foreach (Vertex v in reflex) {
            temp += v.ID + ", ";
        }
        //Debug.Log(temp);
        temp = "Ears: ";
        foreach (Vertex v in ears) {
            temp += v.ID + ", ";
        }
        //Debug.Log(temp);

        // start clipping ears
        int tries = 0;
        while (poly.Count > 3 && tries < 100) {

            if (ears.Count > 0) {
                // find triangle information
                Vertex clipped = ears[0];
                int position = poly.IndexOf(clipped);
                Vertex prev = poly[(position - 1 + poly.Count) % poly.Count];
                Vertex next = poly[(position + 1) % poly.Count];

                // debug and display
                //Debug.Log("Clipping triangle: <" + prev.ID + ", " + clipped.ID + ", " + next.ID + ">");
                polyCuts.Add(new Line(prev.VertexLocation, next.VertexLocation, 6));

                // remove cut vertex from lists
                poly.RemoveAt(position);
                ears.RemoveAt(ears.IndexOf(clipped));
                convex.RemoveAt(convex.IndexOf(clipped));

                // find out if next and previous vertices changed states
                // convex cannot change to reflex, reflex can change to convex

                /* -- PREVIOUS -- */

                if (reflex.IndexOf(prev) > -1) {    // relfex potentially changing
                    float det = determinate(poly[(poly.IndexOf(prev) - 1 + poly.Count) % poly.Count].VertexLocation, prev.VertexLocation, next.VertexLocation);

                    if (det < 0) {
                        convex.Add(prev);
                        reflex.RemoveAt(reflex.IndexOf(prev));


                        // if there's no reflex vertices, all of them are ears
                        if (reflex.Count == 0) {
                            ears.Add(prev);
                            prev.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                        }
                        else {
                            bool isEar = true;
                            for (int j = 0; j < reflex.Count; j++) {

                                // check to see if the reflex vertex is part of the triangle
                                if (reflex[j].ID == poly[(poly.IndexOf(prev) - 1 + poly.Count) % poly.Count].ID || reflex[j].ID == next.ID) {
                                    continue;
                                }

                                // check to see if the point is inside the triangle
                                if (pointInTriangle(poly[(poly.IndexOf(prev) - 1 + poly.Count) % poly.Count].VertexLocation, prev.VertexLocation, next.VertexLocation, reflex[j].VertexLocation)) {
                                    isEar = false;
                                }
                                else {
                                    //Debug.Log("Point #" + reflex[j].ID + " not in the triangle");
                                }
                            }

                            if (isEar) {
                                ears.Add(prev);
                                prev.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                            }
                            else {
                                prev.GetComponentInParent<SpriteRenderer>().color = new Color(255, 0, 0, 1f);
                            }
                        }
                    }
                    else {
                        // still a reflex vertex
                        prev.GetComponentInParent<SpriteRenderer>().color = new Color(0, 255, 0, 1f);
                    }

                }
                else {    // convex already

                    int earPos = ears.IndexOf(prev);
                    // if there's no reflex vertices, all of them are ears
                    if (reflex.Count == 0) {
                        if (earPos < 0) {
                            ears.Add(prev);
                        }
                        prev.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                    }
                    else {
                        bool isEar = true;
                        for (int j = 0; j < reflex.Count; j++) {

                            // check to see if the reflex vertex is part of the triangle
                            if (reflex[j].ID == poly[(poly.IndexOf(prev) - 1 + poly.Count) % poly.Count].ID || reflex[j].ID == next.ID) {
                                continue;
                            }

                            // check to see if the point is inside the triangle
                            if (pointInTriangle(poly[(poly.IndexOf(prev) - 1 + poly.Count) % poly.Count].VertexLocation, prev.VertexLocation, next.VertexLocation, reflex[j].VertexLocation)) {
                                isEar = false;
                            }
                            else {
                                //Debug.Log("Point #" + reflex[j].ID + " not in the triangle");
                            }
                        }

                        if (isEar) {
                            if (earPos < 0) {
                                ears.Add(prev);
                            }
                            prev.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                        }
                        else {
                            if (earPos > -1) {
                                ears.RemoveAt(earPos);
                            }
                            prev.GetComponentInParent<SpriteRenderer>().color = new Color(255, 0, 0, 1f);
                        }
                    }
                }

                /* -- NEXT -- */

                if (reflex.IndexOf(next) > -1) {    // reflex potentially changing
                    float det = determinate(prev.VertexLocation, next.VertexLocation, poly[(poly.IndexOf(next) + 1) % poly.Count].VertexLocation);

                    if (det < 0) {
                        convex.Insert(0, next);
                        reflex.RemoveAt(reflex.IndexOf(next));


                        // if there's no reflex vertices, all of them are ears
                        if (reflex.Count == 0) {
                            ears.Insert(0, next);
                            next.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                        }
                        else {
                            bool isEar = true;
                            for (int j = 0; j < reflex.Count; j++) {

                                // check to see if the reflex vertex is part of the triangle
                                if (reflex[j].ID == prev.ID || reflex[j].ID == poly[(poly.IndexOf(next) + 1) % poly.Count].ID) {
                                    continue;
                                }

                                // check to see if the point is inside the triangle
                                if (pointInTriangle(prev.VertexLocation, next.VertexLocation, poly[(poly.IndexOf(next) + 1) % poly.Count].VertexLocation, reflex[j].VertexLocation)) {
                                    isEar = false;
                                }
                                else {
                                    //Debug.Log("Point #" + reflex[j].ID + " not in the triangle");
                                }
                            }

                            if (isEar) {
                                ears.Insert(0, next);
                                next.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                            }
                            else {
                                next.GetComponentInParent<SpriteRenderer>().color = new Color(255, 0, 0, 1f);
                            }
                        }
                    }
                    else {
                        // still a reflex vertex
                        next.GetComponentInParent<SpriteRenderer>().color = new Color(0, 255, 0, 1f);
                    }

                }
                else { // convex already

                    int earPos = ears.IndexOf(next);
                    // if there's no reflex vertices, all of them are ears
                    if (reflex.Count == 0) {
                        if (earPos < 0) {
                            ears.Insert(0, next);
                        }
                        next.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                    }
                    else {
                        bool isEar = true;
                        for (int j = 0; j < reflex.Count; j++) {
                            //Debug.Log("Checking: #" + reflex[j].ID + " Previous: #" + prev.ID + " Next: #" + poly[(poly.IndexOf(next) + 1) % poly.Count].ID);

                            // check to see if the reflex vertex is part of the triangle
                            if (reflex[j].ID == prev.ID || reflex[j].ID == poly[(poly.IndexOf(next) + 1) % poly.Count].ID) {
                                continue;
                            }

                            // check to see if the point is inside the triangle
                            if (pointInTriangle(prev.VertexLocation, next.VertexLocation, poly[(poly.IndexOf(next) + 1) % poly.Count].VertexLocation, reflex[j].VertexLocation)) {
                                isEar = false;
                            }
                            else {
                                //Debug.Log("Point #" + reflex[j].ID + " not in the triangle");
                            }
                        }

                        if (isEar) {
                            if (earPos < 0) {
                                ears.Insert(0, next);
                            }
                            next.GetComponentInParent<SpriteRenderer>().color = new Color(0, 0, 255, 1f);
                        }
                        else {
                            if (earPos > -1) {
                                ears.RemoveAt(earPos);
                            }
                            next.GetComponentInParent<SpriteRenderer>().color = new Color(255, 0, 0, 1f);
                        }
                    }
                }
            }

            temp = "Convex: ";
            foreach (Vertex v in convex) {
                temp += v.ID + ", ";
            }
            //Debug.Log(temp);
            temp = "Reflex: ";
            foreach (Vertex v in reflex) {
                temp += v.ID + ", ";
            }
            //Debug.Log(temp);
            temp = "Ears: ";
            foreach (Vertex v in ears) {
                temp += v.ID + ", ";
            }
            //Debug.Log(temp);

            tries++;
        }

        if (poly.Count > 3) {
            return false;
        }
        else {
            return true;
        }

        #endregion
        
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
}
