using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuManager : MonoBehaviour
{
    
    public static PauseMenuManager Instance;

    private void Awake () {
        if(!Instance) {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        } else {
            Destroy(this.gameObject);
            
        }
    }

    public void PauseGame() {}

    public void ResumeGame() {}

    //May be implemented at the end
    /*
    public QuitGame() {}

    public MuteGameSound() {}

    public UnMuteSound() {}
    */
}
