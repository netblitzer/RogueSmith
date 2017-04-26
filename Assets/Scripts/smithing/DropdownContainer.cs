using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DropdownContainer : MonoBehaviour {

	public bool open = false;
	public int tabCount = 1;
	public Vector2 targetPosition;

	private List<bool> tabs;
	private int curTab = 0;
	public string currentOpenTab;

	private Vector2 velocity;
	private RectTransform t;
	private Vector2 startPos;

	// Use this for initialization
	void Start () {
		tabs = new List<bool> (tabCount);

		for (int i = 0; i < tabCount; i++) {
			tabs.Add (false);
		}

		velocity = Vector2.zero;

		t = gameObject.GetComponent<RectTransform> ();
		startPos = t.anchoredPosition;
	}

	void Update () {
		if (!open) {
			t.anchoredPosition = Vector2.SmoothDamp (t.anchoredPosition, startPos, ref velocity, 0.25f, 1000f, Time.deltaTime);
		} else {
			t.anchoredPosition = Vector2.SmoothDamp (t.anchoredPosition, startPos + targetPosition, ref velocity, 0.25f, 1000f, Time.deltaTime);
		}
	}

	public void tabClicked(string _data) {

		string[] broken = _data.Split(':');

		if (tabs[int.Parse(broken[0])] && open) {
			
			tabs [int.Parse(broken[0])] = false;
			open = false;
			currentOpenTab = "none";

		} else if (!tabs[int.Parse(broken[0])] && open) {
			
			tabs [curTab] = false;
			tabs [int.Parse(broken[0])] = true;
			curTab = int.Parse(broken[0]);
			currentOpenTab = broken[1];

		} else if (!tabs[int.Parse(broken[0])] && !open) {

			open = true;
			tabs [curTab] = false;
			tabs [int.Parse(broken[0])] = true;
			curTab = int.Parse(broken[0]);
			currentOpenTab = broken[1];

		}
	}
}
