using UnityEngine;
using TMPro;

public class LapTimer : MonoBehaviour
{
    private float lapTime = 0f;
    private bool isTiming = false;
    public TextMeshProUGUI timerText;

    void Update()
    {
        if (isTiming)
        {
            lapTime += Time.deltaTime;
            if (timerText != null)
                timerText.text = "Time: " + lapTime.ToString("F2") + "s";
        }
    }

    public void StartTimer()
    {
        lapTime = 0f; // Reinicia el tiempo
        isTiming = true;
    }

    public void StopTimer()
    {
        isTiming = false;
    }

    public float GetLapTime()
    {
        return lapTime; // Devuelve el tiempo final de la vuelta
    }
}