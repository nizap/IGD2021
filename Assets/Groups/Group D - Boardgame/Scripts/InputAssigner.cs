using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class InputAssigner : MonoBehaviour
{
    // add player inputs in order
    public List<PlayerInput> players;

    // Start is called before the first frame update
    void Start()
    {
        InputManager.Instance.AssignPlayerInput(players);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
