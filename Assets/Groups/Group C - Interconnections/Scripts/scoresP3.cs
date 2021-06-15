using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class scoresP3 : MonoBehaviour
{
  private int score = 0;
  public Text scoreText;

  // Call this method to display your scores.
  public void ReceiveScores(int score){
    scoreText.text = score.ToString() + "    Lu";//Placeholder name
  }
}
