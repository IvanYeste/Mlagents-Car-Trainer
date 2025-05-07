using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaleManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1.0f; // Cambia esto a la velocidad deseada
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        
    }

}
