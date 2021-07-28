using UnityEngine;
using System.Collections;

public class CollectionItem<T> : MonoBehaviour {
	public T data;
	private Component context;

	public Component Context {
		get {
			return context;
		}
		set {
			context = value;
		}
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	} 

	public virtual void SetData(T  data){
		
	}

    public virtual void SetSelected()
    {

    }
}
