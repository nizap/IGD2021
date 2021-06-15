using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerScript : MonoBehaviour
{
  float currentTime = 0f;
  [SerializeField] float SetTimerInSeconds;// Duration of the game in seconds

  [SerializeField] Text countdownText;

  void Start(){
    currentTime = SetTimerInSeconds;
  }
  void Update(){
    currentTime -= 1 * Time.deltaTime;
    countdownText.text = currentTime.ToString("00");

    if (currentTime <= 5) {
    countdownText.color = Color.red;
    }

    if (currentTime <= 0) {    
      currentTime = 0;
      //Time.timeScale = 0;// Pauses the game.
    }
  }
}
