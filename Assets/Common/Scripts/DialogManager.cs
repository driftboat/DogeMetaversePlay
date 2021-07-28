//  DialogManager.cs
// Administrator
using System;
using UnityEngine;


public class DialogManager
{
	private static T create<T>()
	{
		var prefab = Resources.Load<GameObject> ("Prefabs/"+typeof(T));
		GameObject self = GameObject.Instantiate(prefab) as GameObject;
		return self.GetComponent<T>();
	}

	public  static T Show<T>(){
		return create<T> ();
	}  
}

