using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// place for stats and properties of the player (which are not displayed in the HUD)
// e.g. distance walked, tile the player is currently on, etc.
public class PlayerData : MonoBehaviour
{
    public Tile startTile;
    
    // TODO more stats for achievements
    // TODO true party person status flag

    private int distanceWalked = 0; // TODO: use this for the "most distance walked" achievement
    private Tile tile;
   
    public Tile currentTile() {
        return  tile;
    }

    public void moveTo(Tile newTile) {
        // separate from walk so it can be used to shuffle 
        // the player positions (master hand event)
        tile = newTile;
    }

    public void walk() {
        // actionPoints--;
        distanceWalked++;
    }

    public int getDistanceWalked() {
        return distanceWalked;
    }

    // use this method to restore an old state after a minigame
    public void restore(int oldDistanceWalked) {
        distanceWalked = oldDistanceWalked;
    }

    // Start is called before the first frame update
    void Start()
    {
        tile = startTile;
    }
}
