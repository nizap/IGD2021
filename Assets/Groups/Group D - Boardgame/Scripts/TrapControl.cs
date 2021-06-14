using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapControl : MonoBehaviour
{
    private Vector3 offset = new Vector3(0f, 0.51f, 0f);    // hovering offset from players 
    public Transform[] players;                     // transforms of all four players, in order
    public PlayerData[] playerDatas;
    public float deadzone = 0.1f;                   // distance to target position where a movement is considered to be completed
    private float speed = 25.0f;                      // how many units the trap can move per second

    private Vector3 loiterPoint; // point where the item thief will loiter when it is not used
    private bool movementDone;   // whether all animations are done
    private Vector3 startingPos; // position where a movement was started
    private Vector3 targetPos;
    private double flyingTime;
    private double distance;
    public AudioClip dropAudioClip;
    public AudioClip triggerAudioClip;
    AudioSource audioSource;


    private void setNewTarget(Vector3 newTarget)
    {
        targetPos = newTarget;
        movementDone = false;
        startingPos = transform.position;
        flyingTime = 0.0;
        distance = Vector3.Distance(startingPos, targetPos);
    }

    public void moveToPlayerTile(int player)
    {
        setNewTarget(playerDatas[player].currentTile().getPosition() + offset);
    }

    public void moveAbovePlayerTile(int player)
    {
        Vector3 hoverposition = new Vector3(playerDatas[player].currentTile().getPosition().x,loiterPoint.y,playerDatas[player].currentTile().getPosition().z);
        setNewTarget(hoverposition);
    }
    
    public void returnToLoiterPoint()
    { 
        setNewTarget(loiterPoint);
    }

    public bool movementCompleted()
    {
        return movementDone;
    }
    
    public void playStealAudio()
    {
        audioSource.PlayOneShot(dropAudioClip);
    }

    // Start is called before the first frame update
    void Start()
    {
        loiterPoint = transform.position;
        targetPos = loiterPoint;
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        float remainingDistance = Vector3.Distance(transform.position, targetPos);
        if (remainingDistance >= deadzone)
        {
            flyingTime += Time.deltaTime;
            transform.position = Vector3.Lerp(startingPos, targetPos, (float) (speed * flyingTime / distance));
            transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));
        }
        else
        {
            movementDone = true;
        }
    }

    // this method is used to restore a previous state after a minigame
    public void restore(Vector3 location) {
        transform.position = location;
    }

    public void playDropAudio()
    {
        audioSource.PlayOneShot(dropAudioClip);
    }
    
    public void playTriggerAudio()
    {
        audioSource.PlayOneShot(triggerAudioClip);
    }
}
