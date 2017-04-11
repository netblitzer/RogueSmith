using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This class attaches to the design area of the screen in the smithing scene.
/// It handles all the inputs and passes the information along to the smithing manager to update.
/// </summary>

public class DesignField : MonoBehaviour,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler, 
    IPointerEnterHandler, 
    IPointerExitHandler, 
    IDragHandler, 
    IEndDragHandler {

    private SmithingManager sm;

	// Use this for initialization
	void Start () {
        sm = GameObject.FindObjectOfType<SmithingManager>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnPointerClick(PointerEventData e) {
    }

    public void OnPointerDown(PointerEventData e) {
        Debug.Log("clicked");

        if (e.button == PointerEventData.InputButton.Left) {
            sm.fieldClicked(e.pressPosition);
        }
    }

    public void OnPointerUp(PointerEventData e) {

    }

    public void OnPointerEnter(PointerEventData e) {

    }

    public void OnPointerExit(PointerEventData e) {

    }
    
    public void OnDrag(PointerEventData e) {

    }

    public void OnEndDrag(PointerEventData e) {

    }
}
