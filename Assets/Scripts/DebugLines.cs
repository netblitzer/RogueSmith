using UnityEngine;
using System.Collections;

public class DebugLines : MonoBehaviour {

    private Queue lineRenderQueue;

    public Material debugMaterial;
    public Color[] colors;

    private class Line {
        public Vector3 start;
        public Vector3 end;
        public int mode;

        public Line (Vector3 _start, Vector3 _end, int _mode) {
            start = _start;
            end = _end;
            mode = _mode;
        }
    }

	// Use this for initialization
	void Start () {
        lineRenderQueue = new Queue();
        //colors = new Color[5];
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    // Function to add new lines to render
    public void addLine (Vector3 _startingPoint, Vector3 _endPoint, int _mode) {
        Line l = new Line(_startingPoint, _endPoint, _mode);
        lineRenderQueue.Enqueue(l);
    }

    void OnRenderObject () {
        while (lineRenderQueue.Count > 0) {
            Line temp = (Line) lineRenderQueue.Dequeue();

            debugMaterial.SetColor("_Color", colors[temp.mode]);
            debugMaterial.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Vertex(temp.start);
            GL.Vertex(temp.end);
            GL.End();
        }
    }
}
