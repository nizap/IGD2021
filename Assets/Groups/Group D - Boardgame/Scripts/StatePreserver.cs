using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatePreserver : MonoBehaviour
{
    public static StatePreserver Instance;

    public class PlayerState {
        public int credits;
        public int bricks;
        public List<ItemD.Type> items;
        public Vector3 position;
        public TileCoord currentTile;
        public int distanceWalked;
    }

    public class TileCoord {
        public float x;
        public float z;
    }

    public class BoardState {
        public int truePartyPerson;
        public int round;
        public TileCoord brickTile;
        public List<TileCoord> trapTiles;
    }

    public BoardState boardState;
    public List<PlayerState> playerStates;
    public bool gameStarted = true;

    private void Awake () {
        if(!Instance) {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        } else {
            Destroy(this.gameObject);
        }
    }
}
