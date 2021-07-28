using System.Collections; 
public class LandDatasource : CollectionDataSource<Land> {  
	public int type = 1;
	// Use this for initialization
	protected override void Start () {
		base.Start();
	}

	// Update is called once per frame
	void Update () {

	} 

	protected override IEnumerator fetchDataInternal(){
		datas.Clear();
		Land l = new Land();
		l.LandPos.x = 1;
		l.LandPos.y = 0;
		l.LandPos.z = 0;
		datas.Add(l);
		yield return null;
	
	}
}
