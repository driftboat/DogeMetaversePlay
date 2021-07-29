using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LandItem : CollectionItem<Land> {
	public Text landPosText;
	Rect screenRect;
    // Use this for initialization
    void Start () {
        screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
     //   this.scoreText.enabled = false;
    }
	
	// Update is called once per frame
	void Update () { 
	}

	public override void SetData (Land data)
	{
		landPosText.text = String.Format("{0}-({1},{2})", data.LandPos.x, data.LandPos.y, data.LandPos.z);

	} 
}
