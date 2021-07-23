using System.Collections; 
using System.Collections.Generic; 
using UnityEngine; 
using System.IO; 
 
public class ScreenshotCapture : MonoBehaviour
{
    public int superSize = 4;
    private bool screenShotLock = false; 
 
    private void LateUpdate() 
    { 
        if (Input.GetKeyDown(KeyCode.T) && !screenShotLock) 
        { 
            screenShotLock = true; 
            StartCoroutine(SaveScreenshot()); 
        } 
    } 
 
    private IEnumerator SaveScreenshot() 
    { 
        yield return new WaitForEndOfFrame(); 
 
        var directory = new DirectoryInfo(Application.dataPath); 
        var path = Path.Combine(directory.Parent.FullName, string.Format("Screenshot_{0}.png", System.DateTime.Now.ToString("yyyyMMdd_Hmmss"))); 
        Debug.Log("Saving screenshot: " + path); 
        ScreenCapture.CaptureScreenshot(path, superSize); 
        screenShotLock = false; 
    } 
 
} 