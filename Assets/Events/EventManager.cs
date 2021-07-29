 
public class EventManager:Singleton<EventManager>
{
    public delegate void SaveLandEventHandler(Land land);
    public event  SaveLandEventHandler saveLand;
    public event  SaveLandEventHandler gotoLand;
    public event  SaveLandEventHandler uploadLand;

    public void OnSaveLand(Land land)
    {
        saveLand?.Invoke(land);
    }
}

