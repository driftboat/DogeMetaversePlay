using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollectionDataSource<T> : MonoBehaviour {
	protected List<T> datas;

	public GameObject tableItemPrefab;
    public bool loadOnStart = true;
    private Component context;
	void Awake(){
		context = this.transform.root.GetComponent<CollectionDialog>();
	}

	// Use this for initialization
	protected virtual void Start () {
		datas = new List<T>();
        if (loadOnStart)
        {
            StartCoroutine(FetchData());
        }
	}

	// Update is called once per frame
	void Update () {

	}

    public void Refresh() {
        datas.Clear();
        StartCoroutine(FetchData());
    }

	protected virtual IEnumerator FetchData(){ 
		foreach (Transform child in transform) {
			GameObject.Destroy(child.gameObject);
		} 

		var it = fetchDataInternal();
		while(it.MoveNext()){
			yield return it.Current;
		}

		for(int i=0; i<datas.Count; i++){
			GameObject item = GameObject.Instantiate(tableItemPrefab) as GameObject;
			item.transform.SetParent(this.transform);
			item.transform.localScale = Vector3.one;
            CollectionItem<T> cItem = item.GetComponent<CollectionItem<T>>();

            cItem.Context = context;
            cItem.SetData(datas[i]);
            if(i == 0)
            {
                cItem.SetSelected();
            }
		} 
		 
	}

	protected virtual IEnumerator fetchDataInternal(){
		yield return null;
	}
}
