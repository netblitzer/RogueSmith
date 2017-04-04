using UnityEngine;
using System.Collections;

public class Vertex : MonoBehaviour {

    private Vector2 vertLoc;
    public Vector2 VertexLocation
    {
        get {
            return vertLoc;
        }
    }

    private bool innerVert;
    public bool IsInnerVertex
    {
        get {
            return innerVert;
        }
    }

    public Vertex prevVert;
	public Vertex PreviousVertex
    {
        get {
            return prevVert;
        }
    }

    public Vertex nextVert;
	public Vertex NextVertex
    {
        get {
            return nextVert;
		}
    }

    // Unique identifier for linking purposes
    private int id;
    public int ID
    {
        get {
            return id;
        }
    }

    // Mode the vertex is in (TEMP)
    public int mode;

    // Use this for initialization
    void Awake () {
        vertLoc = Vector2.zero;
        innerVert = false;
        prevVert = null;
        nextVert = null;
        id = 0;
    }

	public bool setVertexLocation(Vector2 _loc) {

        vertLoc = _loc;

        gameObject.transform.position = _loc;//= new Vector3(32f * _loc.x / Display.main.systemWidth, 18f * _loc.y / Display.main.systemHeight, 0f);

	    return true;
    }

    public void setVertexType(bool _type) {
        innerVert = _type;
    }

	public bool setPreviousVertex(Vertex _prev) {
        prevVert = _prev;
        return true;
    }

	public bool setNextVertex(Vertex _next) {
		nextVert = _next;
        return true;

    }

    public bool setID(int _id) {
        if(_id > -1) {
            id = _id;
            return true;
        }

        return false;
    }

    public void selfDestruct() {
        nextVert = null;
        prevVert = null;

        Destroy(gameObject);
    }
}
