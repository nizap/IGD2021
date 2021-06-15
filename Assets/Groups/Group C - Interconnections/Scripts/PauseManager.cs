using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuPrefab;
    public GameObject canvasPrefab;
    private Canvas _canvas;
    private bool pauseIsOpen=false;

/////////////////////////////////////////////////////////////
    public static PauseManager Instance;
    private void Awake () {
        if(!Instance) {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        } else {
            Destroy(this.gameObject);
        }
    }
////////////////////////////////////////////////////////////

    private Canvas SceneCanvas {
      get{
        // Check if canvas variable is set
        if(_canvas == null){
          // If not set, look in scene for canvas
          _canvas = FindObjectOfType<Canvas>();
          if(_canvas == null){
            // If no canvas in scene, create one
            _canvas = Instantiate(canvasPrefab).GetComponent<Canvas>();
          }
        }
        return _canvas;
      }
    }

    private GameObject pauseMenuInstance;

    public void OpenPauseMenu(){
      Time.timeScale = 0;// Pauses the game.
      // Checking to see if one exists
      if(pauseMenuInstance == null){
        //Creates a new game menu
      Instantiate(pauseMenuPrefab, SceneCanvas.transform);
    } else{
      // Set currently existing pause menu to true
      pauseMenuInstance.SetActive(true);
    }
    pauseIsOpen=true;
  }

  public void ClosePauseMenu(){
     Time.timeScale = 1;// Continues the game.
     Destroy(GameObject.Find("PauseMenu(Clone)"));
     //Destroy(this.gameObject);
     pauseIsOpen=false;
  }

  public void Update(){
    // Space is down
    if(Input.GetKeyDown(KeyCode.Space)){
      if(pauseIsOpen==false){
      OpenPauseMenu();
    }
    else if(pauseIsOpen==true){
      ClosePauseMenu();
    }
    }
  }
}
