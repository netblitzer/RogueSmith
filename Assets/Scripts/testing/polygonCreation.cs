using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;


public class polygonCreation : MonoBehaviour {

    [Tooltip("The prefab for vertices.")]
    public GameObject vertPrefab;

    [Tooltip("The maximum amount of vertices a polygon can have.")]
    public int maxVertices = 10;

    [Tooltip("The minimum amount of vertices a polygon can have.")]
    public int minVertices = 5;

    [Tooltip("The largest angle between two lines that the shape will try to create.")]
    public float maxAngle = 70f;

    [Tooltip("The max number of tries the program will run to create the polygon.")]
    public int maxTries = 50;

    public List<Vertex> vertices;
	public List<Vertex> innerVertices;

    private DebugLines debug;
    private float timePassed;
    private List<Line> polyCuts;
    private List<Line> linesToDraw;
    private bool generated = false;
    private bool triangulated = false;
    private bool pause = false;
    
     public static void ClearLogConsole() {
        Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
        var logEntries = assembly.GetType("UnityEditorInternal.LogEntries");
        MethodInfo clearConsoleMethod = logEntries.GetMethod("Clear");
        clearConsoleMethod.Invoke(new object(), null);
    }

// Use this for initialization
void Start () {

        polyCuts = new List<Line>();
        linesToDraw = new List<Line>();
        debug = gameObject.GetComponent<DebugLines>();

	}
	
	// Update is called once per frame
	void Update () {
        timePassed += Time.deltaTime;
        /*
        if (!pause && timePassed > 0.5f) {
            
            if (!generated) {
                ClearLogConsole();
                while (vertices.Count > 0) {
                    Vertex v = vertices[0];
                    vertices.RemoveAt(0);
                    v.selfDestruct();
                }
                while (polyCuts.Count > 0) {
                    polyCuts.RemoveAt(0);
                }
                while (linesToDraw.Count > 0) {
                    polyCuts.RemoveAt(0);
                }

                generatePolygon();

                generated = true;
            }
            else {
                if (!triangulatePolygon()) {
                    pause = true;
                }
                generated = false;
            }

            timePassed -= 0.5f;
        }
        */
        /*
        while (vertices.Count > 0) {
            Vertex v = vertices[0];
            vertices.RemoveAt(0);
            v.selfDestruct();
        }
        generatePolygon();
        */

        if (Input.GetKeyDown(KeyCode.Space)) {

            if (!generated && !triangulated) {
                while (vertices.Count > 0) {
                    Vertex v = vertices[0];
                    vertices.RemoveAt(0);
                    v.selfDestruct();
                }
                while (innerVertices.Count > 0) {
                    Vertex v = innerVertices[0];
                    innerVertices.RemoveAt(0);
                    v.selfDestruct();
                }
                while (polyCuts.Count > 0) {
                    polyCuts.RemoveAt(0);
                }
                while (linesToDraw.Count > 0) {
                    linesToDraw.RemoveAt(0);
                }

                generatePolygon();

                generated = true;
            } else if (generated && !triangulated) {
                addInternalVertices();
                triangulated = true;
            } else {
                triangulatePolygon();
                generated = false;
                triangulated = false;
            }
        }
        
        for (int i = 0; i < vertices.Count; i++) {
            debug.addLine(vertices[i].VertexLocation, vertices[(i + 1) % vertices.Count].VertexLocation, 2);
            Debug.DrawLine(vertices[i].VertexLocation, vertices[(i + 1) % vertices.Count].VertexLocation, Color.green);
            //vertices[i].GetComponentInParent<SpriteRenderer>().color = new Color(255, 255, 255, 1f);
        }
        for (int i = 0; i < polyCuts.Count; i++) {
            debug.addLine(polyCuts[i].start, polyCuts[i].end, polyCuts[i].mode);
            Debug.DrawLine(polyCuts[i].start, polyCuts[i].end, Color.red);
        }
        for (int i = 0; i < linesToDraw.Count; i++) {
            debug.addLine(linesToDraw[i].start, linesToDraw[i].end, linesToDraw[i].mode);
            //Debug.DrawLine(polyCuts[i].start, polyCuts[i].end, Color.red);
        }
    }

    // Randomly generates a new polygon for testing
    bool generatePolygon () {
        
        bool succeeded = true;
        
        int count = Mathf.FloorToInt(Random.Range(minVertices, maxVertices));

        vertices = new List<Vertex>(count);
        innerVertices = new List<Vertex>(count);

        Vector2 center = new Vector2(16f, 9f);
        int tries = 0;

        for (int k = 0; k < count; k++) {
            bool linkCorrection = false; // If true, will correct the links between vertices. Acts like a master switch.
            int lineCheck = -1;
            tries = 0;

            Vector2 randPos = new Vector2(Random.Range(0, center.x * 2f), Random.Range(0, center.y * 2f));

            foreach (Vertex vt in vertices) {
                vt.gameObject.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                vt.mode = 0;
            }

            float closestLineDist = float.MaxValue; // initialize closest distance to max value

            // see which line is closest to the mouse
            for (int i = 0; i < vertices.Count; i++) {
                if (vertices[i].PreviousVertex != null) {

                    // get distance to line
                    float dist = distanceFromLine(vertices[i].PreviousVertex.VertexLocation, vertices[i].VertexLocation, randPos, closestLineDist);
                    // compare to current closest
                    if (dist < closestLineDist) {

                        // set new closest
                        lineCheck = i;
                        closestLineDist = dist;
                    }
                }
            }
            // Check to make sure the position won't cross any lines or make too sharp of angles)
            if (lineCheck > -1 && vertices.Count > 1 && tries < 10000) {
                float distNext = (randPos - vertices[lineCheck].NextVertex.VertexLocation).magnitude;
                float distPrev = (randPos - vertices[lineCheck].VertexLocation).magnitude;

                if (distPrev < 4f || distNext < 4f) {
                    k--;
                    tries++;
                    continue;
                }

                float newAng = Mathf.Abs(angleBetweenLines((randPos - vertices[lineCheck].VertexLocation), (randPos - vertices[lineCheck].NextVertex.VertexLocation)));
                //Debug.Log("Angle: " + newAng + "Vertex: " + k);
                if (newAng < 90 - maxAngle || newAng > 90 + maxAngle) {
                    k--;
                    tries++;
                    continue;
                }
                else if (vertices.Count > 2) {
                        float prevAng = Mathf.Abs(angleBetweenLines((vertices[lineCheck].VertexLocation - vertices[lineCheck].PreviousVertex.VertexLocation), (randPos - vertices[lineCheck].VertexLocation)));
                        float nextAng = Mathf.Abs(angleBetweenLines((randPos - vertices[lineCheck].NextVertex.VertexLocation), (vertices[lineCheck].NextVertex.NextVertex.VertexLocation - vertices[lineCheck].NextVertex.VertexLocation)));

                    if ((prevAng <  90 - maxAngle || prevAng > 90 + maxAngle) && (nextAng < 90 - maxAngle || nextAng > 90 + maxAngle)) {
                        k--;
                        tries++;
                        continue;
                    }
                }
            }
            else if (vertices.Count > 0) {
                float distPrev = (randPos - vertices[0].VertexLocation).magnitude;

                if (distPrev < 2f) {
                    k--;
                    tries++;
                    continue;
                }
            }

            if (tries > maxTries)
                Debug.Log("Max tries hit");

            if (lineCheck > -1 && vertices.Count > 4) {
                bool crossed = false;
                for (int i = 0; i < vertices.Count; i++) {
                    if (lineCrossingCheck(randPos, vertices[lineCheck].VertexLocation, vertices[(lineCheck + i + 1) % vertices.Count].VertexLocation, vertices[(lineCheck + i + 2) % vertices.Count].VertexLocation)) {
                        crossed = true;
                    }
                }
                if (crossed) {
                    k--;
                    tries++;
                    continue;
                }
            }

            // Adding new vertices

            GameObject v = GameObject.Instantiate(vertPrefab);
            Vertex vx = v.GetComponent<Vertex>();

            if (!vx.setVertexLocation(randPos)) {
                GameObject.Destroy(v);
            }
            else if (lineCheck > -1) {
                linkCorrection = true;

                if (vertices.Count < 3) {

                    // Checks to make sure the vertices are added in counter-clockwise pattern

                    Vector2 lineVec = (vertices[0].PreviousVertex.VertexLocation - vertices[0].VertexLocation).normalized;
                    lineVec = new Vector2(lineVec.y, -lineVec.x);

                    Vector2 adjMouse = randPos - vertices[0].VertexLocation;
                    float dir = Vector2.Dot(adjMouse, lineVec);

                    if (dir > 0) {
                        vertices.Insert(2, vx);
                    }
                    else {
                        vertices.Insert(1, vx);
                    }
                }
                else {
                    vertices.Insert(lineCheck, vx);
                }
            }
            else {
                linkCorrection = true;

                vertices.Insert(0, vx);
            }


            // Make sure the links are all in place correctly
            if (linkCorrection) {
                correctVertexLinks();
                //Debug.Log("Correcting links");
            }
        }

        return succeeded;
    }

    int findClosestPointAcross(Vector2 _point, List<int> _excluded) {

        float closest = float.MaxValue;
        int selected = -1;

        // find closest point to remove
        for (int i = 0; i < vertices.Count; i++) {

            // skip excluded vertices
            if (_excluded.IndexOf(i) > -1) {
                continue;
            }

            float dist = Vector2.Distance(_point, vertices[i].VertexLocation);

            if (dist < closest) {

                selected = vertices[i].ID;

                closest = dist;
            }
        }

        return selected;
    }

    bool triangulatePolygon() {

        #region Ear Cutting

        // four arrays
        List<Vertex> poly =   new List<Vertex>(innerVertices.Count);
        List<Vertex> reflex = new List<Vertex>(innerVertices.Count);
        List<Vertex> convex = new List<Vertex>(innerVertices.Count);
        List<Vertex> ears =   new List<Vertex>(innerVertices.Count);

        // populate lists
        for (int i = 0; i < innerVertices.Count; i++) {

            // every vertex goes in
            poly.Add(innerVertices[i]);

            // find whether the angle is convex or reflex
            float det = determinate(innerVertices[(i - 1 + innerVertices.Count) % innerVertices.Count].VertexLocation, innerVertices[i].VertexLocation, innerVertices[(i + 1) % innerVertices.Count].VertexLocation);

            if (det < 0) {
                convex.Add(innerVertices[i]);
                innerVertices[i].GetComponentInParent<SpriteRenderer>().color = new Color(255, 0, 0, 1f);
            } else {
                reflex.Add(innerVertices[i]);
                innerVertices[i].GetComponentInParent<SpriteRenderer>().color = new Color(0, 255, 0, 1f);
            }
        }

        // find ears
        for (int i = 0; i < convex.Count; i++) {
            Vertex prev = poly[(convex[i].ID - 1 + poly.Count) % poly.Count];
            Vertex next = poly[(convex[i].ID + 1) % poly.Count];

            Debug.Log("Prev: " + prev.ID + " Curr: " + convex[i].ID + " Next: " + next.ID);

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
                    } else {
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
        foreach(Vertex v in convex) {
            temp += v.ID + ", ";
        }
        Debug.Log(temp);
        temp = "Reflex: ";
        foreach (Vertex v in reflex) {
            temp += v.ID + ", ";
        }
        Debug.Log(temp);
        temp = "Ears: ";
        foreach (Vertex v in ears) {
            temp += v.ID + ", ";
        }
        Debug.Log(temp);

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
                    } else {
                        // still a reflex vertex
                        prev.GetComponentInParent<SpriteRenderer>().color = new Color(0, 255, 0, 1f);
                    }

                } else {    // convex already

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
                    } else {
                        // still a reflex vertex
                        next.GetComponentInParent<SpriteRenderer>().color = new Color(0, 255, 0, 1f);
                    }

                } else { // convex already

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
                            Debug.Log("Checking: #" + reflex[j].ID + " Previous: #" + prev.ID + " Next: #" + poly[(poly.IndexOf(next) + 1) % poly.Count].ID);

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
            Debug.Log(temp);
            temp = "Reflex: ";
            foreach (Vertex v in reflex) {
                temp += v.ID + ", ";
            }
            Debug.Log(temp);
            temp = "Ears: ";
            foreach (Vertex v in ears) {
                temp += v.ID + ", ";
            }
            Debug.Log(temp);

            tries++;
        }
        
        if (poly.Count > 3) {
            return false;
        } else {
            return true;
        }

        #endregion

        #region badTriangulation
        /*
        /// rules
        ///     alpha less than 180
        ///     must use new cut line
        ///     cannot create 2 lines at once
        ///     cannot create vertices
        /// process
        ///     1. start with first point in order
        ///     2. use previous new line (if any)
        ///     3. find nearest point that:
        ///         doesn't cross anywhere
        ///         a less than 180
        ///             closest to highest
        ///     4. if can't create line
        ///         repeat #1 with highest index on the last line created

        bool succeeded = true;
        
        int lastLineA = 1;
        int lastLineB = 0;

        List<int> excluded;
        List<int> cutOff = new List<int>();

        int cuts, tries;
        cuts = tries = 0;
        while (cuts < vertices.Count - 3 && tries < vertices.Count) {
            excluded = new List<int>();
            excluded.Add(lastLineA);
            excluded.Add((lastLineA + 1) % vertices.Count);
            excluded.Add(lastLineB);

            for (int i = 0; i < cutOff.Count; i++) {
                excluded.Add(cutOff[i]);
            }

            bool lineCreated = false;
            
            for (int j = 0; j < vertices.Count - 2; j++) {
                int closest = findClosestPointAcross(vertices[lastLineA].VertexLocation, excluded);

                // should never happen
                if (closest < 0) {
                    return false;
                }

                // check for crossed lines
                bool crossed = false;
                for (int i = 0; i < vertices.Count; i++) {

                    if (excluded.IndexOf(i) > -1 || excluded.IndexOf((i + 1) % vertices.Count) > -1) {
                        continue;
                    }

                    if (lineCrossingCheck(vertices[lastLineA].VertexLocation, vertices[closest].VertexLocation, vertices[i % vertices.Count].VertexLocation, vertices[(i + 1) % vertices.Count].VertexLocation)) {
                        Debug.Log("Crossed from " + lastLineA + " to " + closest);
                        crossed = true;
                        break;
                    }
                }
                    
                if (crossed) {
                    excluded.Add(closest);

                    if (excluded.Count >= vertices.Count) {
                        break;
                    }

                    continue;
                }

                // check if creating two lines at once
                    // if it doesn't share a line already made
                if ((vertices[lastLineA].NextVertex != vertices[closest] && vertices[lastLineA].PreviousVertex != vertices[closest]) && 
                    (vertices[lastLineB].NextVertex != vertices[closest] && vertices[lastLineB].PreviousVertex != vertices[closest])) {

                    excluded.Add(closest);

                    if (excluded.Count >= vertices.Count) {
                        break;
                    }

                    continue;
                }

                // find if the line is outside the polygon
                Vector2 lineNorm = (vertices[lastLineA].VertexLocation - vertices[lastLineB].VertexLocation).normalized;
                Vector2 lineRight = new Vector2(lineNorm.y, -lineNorm.x);
                Vector2 adjPoint = (vertices[closest].VertexLocation - vertices[lastLineA].VertexLocation);

                float alpha = Vector2.Dot(lineRight, adjPoint);

                if (alpha <= 0f) {
                    excluded.Add(closest);

                    if (excluded.Count >= vertices.Count) {
                        break;
                    }

                    continue;
                }

                // if other tests passed, create the line
                Debug.Log("Cutting a line from index: " + lastLineA + " to: " + closest);
                polyCuts.Add(new Line(vertices[lastLineA].VertexLocation, vertices[closest].VertexLocation, 4));
                lineCreated = true;

                // figure out which point to move
                    // if the point is a neighbor of A, move A
                    // if the point is a neighbor of B, move B
                if ((vertices[lastLineA].NextVertex == vertices[closest] || vertices[lastLineA].PreviousVertex == vertices[closest])) {
                    cutOff.Add(lastLineA);
                    lastLineA = closest;
                } else if ((vertices[lastLineB].NextVertex == vertices[closest] || vertices[lastLineB].PreviousVertex == vertices[closest])) {
                    cutOff.Add(lastLineB);
                    lastLineB = closest;
                }

                break;
            }

            // if a line was created
            if (lineCreated) {
                cuts++;
            } else {
                if (vertices[lastLineA].NextVertex == vertices[lastLineB] || vertices[lastLineA].PreviousVertex == vertices[lastLineB]) {
                    lastLineB = Mathf.Max(lastLineB, lastLineA);
                    lastLineA = (lastLineB + 1) % vertices.Count;
                }
                else {
                    lastLineB = Mathf.Min(lastLineA, lastLineB);
                    lastLineA = (lastLineB + 1) % vertices.Count;
                }
                tries++;
                Debug.Log("LastLineA: " + lastLineA + "   LastLineB: " + lastLineB);
            }
        }



        return succeeded;
        */
        #endregion
    }

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
						normalCrosses [i] = point;
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

            GameObject t = GameObject.Instantiate(vertPrefab);
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


        #region Bisecting Angles
        /*
        List<Line> bisectingAngles = new List<Line>();

        // set up bisecting lines
        foreach (Vertex v in vertices) {
            Vector2 bisect = bisectAngle(v.PreviousVertex.VertexLocation, v.VertexLocation, v.NextVertex.VertexLocation);
            
            bisectingAngles.Add(new Line(v.VertexLocation, bisect * 100000, v.ID));  // using the color int to store the id
        }

        // find the points where the lines would go out of the shape
        List<Line> crossSegments = new List<Line>();
        float shortestDist = float.MaxValue;

        for (int i = 0; i < bisectingAngles.Count; i++) {

            crossSegments.Add(new Line(bisectingAngles[i].start, bisectingAngles[i].end, 1));
            float closestCross = float.MaxValue;

            for (int j = 0; j < vertices.Count; j++) {

                // check to see if the line contains the point we're checking
                if (bisectingAngles[i].mode == j) {
                    continue;
                }
                if (vertices[j].NextVertex.ID == bisectingAngles[i].mode) {
                    continue;
                }

                // find out if the lines cross at any point
                Vector2 point = pointOnLines(bisectingAngles[i].start, bisectingAngles[i].end, vertices[j].VertexLocation, vertices[j].NextVertex.VertexLocation - vertices[j].VertexLocation);

                if (point.x == -1 && point.y == -1) {
                    // lines don't cross
                    continue;
                } else {
                    // lines cross, see if it's a closer crossing point
                    float dist = Vector2.SqrMagnitude((Vector2)bisectingAngles[i].start - point);
                    if (closestCross > dist) {
                        closestCross = dist;

                        crossSegments[i].end = point;
                        crossSegments[i].mode = 2;
                    }

                    if (shortestDist > dist) {
                        shortestDist = dist;
                    }
                }
            }
        }



        // find the points where the max position inside the shape is for each of them
        List<Line> innerCrosses = new List<Line>();

        for (int i = 0; i < crossSegments.Count; i++) {
            crossSegments[i].end = crossSegments[i].start + (bisectingAngles[i].end.normalized * Mathf.Sqrt(shortestDist) * 0.5f);
        }
        for (int i = 0; i < crossSegments.Count; i++) {
            innerCrosses.Add(new Line(crossSegments[i].end, crossSegments[(i + 1) % crossSegments.Count].end, 3));
        }

        for (int i = 0; i < innerCrosses.Count; i++) {

            linesToDraw.Add(innerCrosses[i]);
            linesToDraw.Add(new Line(crossSegments[i].start, crossSegments[i].end, 4));
        }

        */
        #endregion

        return true;
	}

	Vector2 bisectAngle(Vector2 _a, Vector2 _b, Vector2 _c) {
        float det = determinate(_a, _b, _c);

        int flip = 1;
        if (det > 0) {
            flip = -1;
        }
        
        return (((_a - _b).normalized + (_c - _b).normalized).normalized * flip);
	}

    float determinate(Vector2 _a, Vector2 _b, Vector2 _c) {
        return ((_b.x - _a.x) * (_c.y - _b.y) - (_c.x - _b.x) * (_b.y - _a.y));
    }

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
                //projPoint = _startPoint;
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
                //projPoint = _endPoint;
            }
        }
        // point is along the line
        else {
            dist = Mathf.Abs(upDist);

            if (dist < curMax) {
                //projPoint = _startPoint + (lineVec * lngDist);
            }
        }

        return dist;
    }

    // function to cross two 2D vectors
    float crossVec2(Vector2 _v, Vector2 _w) {
        return ((_v.x * _w.y) - (_v.y * _w.x));
    }

    // function to find the point where two lines cross
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
            } else {
                // lines are parallel
                return point;
            }
        } else {
            t /= cross;
            u /= cross;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) {
                // lines cross
                point = _p + (_r * t);
                return point;
            } else {
                // lines would cross in the future if they continued on
                return point;
            }
        }
        
    }

    bool lineCrossingCheck(Vector2 _p1, Vector2 _p2, Vector2 _p3, Vector2 _p4) {

        bool crossed = false;

        // check to see if either line is just a point
        if (_p1 == _p2 || _p3 == _p4)
            return false;
        
        // check to see if the lines share at least one point
        if (_p1 == _p3 || _p2 == _p4)
            return false;

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

    float angleBetweenLines(Vector2 _line1, Vector2 _line2) {
        return 180f * Mathf.Acos((Vector2.Dot(_line1.normalized, _line2.normalized) / (_line1.normalized.magnitude * _line2.normalized.magnitude))) / Mathf.PI;
    }
}
