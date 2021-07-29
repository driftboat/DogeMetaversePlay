using System;
using Unity.Entities;
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
		this.data = data;
		landPosText.text = $"{data.LandPos.x}-({data.LandPos.y},{data.LandPos.z})";
	}

	public void OnSave()
	{
		EventManager.instance.OnSaveLand(data);
	}
}