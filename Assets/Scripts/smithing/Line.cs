using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour {
    public Vector3 start;
    public Vector3 end;
    public int mode;    // used for color or just an extra int for storage

    public Line(Vector3 _start, Vector3 _end, int _mode) {
        start = _start;
        end = _end;
        mode = _mode;
    }
}
