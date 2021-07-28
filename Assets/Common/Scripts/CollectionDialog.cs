using UnityEngine;
using System.Collections;
using DG.Tweening;
using System;


public class CollectionDialog : MonoBehaviour {
    // Use this for initialization
    public delegate void OnActionClose();
    public OnActionClose onActClose;
    public RectTransform backTransform; 
	protected  static String DlgPrefabName{
		get{
			return "CollectionDialog";
		}
	} 
	protected virtual void Start () {
        //backTransform.DOPunchScale(new Vector3(0.1f,0.1f,0),0.2f,0,0);
        backTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
         Tweener tweener =  backTransform.DOScale(new Vector3(1,1,1), 0.2f);
        tweener.SetEase(Ease.InOutBack);
    }
	
	// Update is called once per frame
	void Update () {
	
	}


	public void Close(){
		this.OnClose();
		GameObject.Destroy(this.gameObject);
	}
 
	protected virtual void OnClose(){
        if (this.onActClose != null) {
            this.onActClose();
        }
	}
}
