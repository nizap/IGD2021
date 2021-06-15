using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;

public class PlayerControllerLobby : MonoBehaviour
{

    public GameObject Minifig;

    private string controlScheme;
    private InputDevice device;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetInputDeviceAndControlScheme(string controlScheme, InputDevice device)
    {
        this.controlScheme = controlScheme;
        this.device = device;

        var playerInput = GetComponent<PlayerInput>();
        playerInput.SwitchCurrentControlScheme(controlScheme, device);
    }

    private void SavePlayerControlScheme()
    {
        //If control scheme already added, ignore

        //If control scheme still free, save it into PlayerPrefs
        Tuple<int, int, string> playerScheme = new Tuple<int, int, string>(1, device.deviceId, controlScheme);
        InputManager.Instance.AddControlScheme(playerScheme);
    }

    /**
     * This section listens to key presses
     */

    //This is for gamepads
    private void OnMove(InputValue value)
    {
        print("MY CHARACTER IS MOVING NOW");
        SavePlayerControlScheme();
    }

    //This is for keyboard
    private void OnMoveDpad(InputValue value)
    {
        print(controlScheme);
        SavePlayerControlScheme();
    }

    private void OnNorthRelease()
    {
        print("HERE IS MY OWN CHARACTER - NORTH");
        SavePlayerControlScheme();
    }

    private void OnSouthRelease()
    {
        print("THis is my own south release");
        SavePlayerControlScheme();
    }

    private void OnWestRelease()
    {
        print("OnWestPress");
        SavePlayerControlScheme();
    }

    private void OnEastRelease()
    {
        print("OnEastRelease");
        SavePlayerControlScheme();
    }


}
