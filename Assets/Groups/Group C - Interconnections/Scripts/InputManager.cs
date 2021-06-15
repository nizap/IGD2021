using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;


public class InputManager : MonoBehaviour
{


    //public GameObject _menuPlayerPrefab;
    private Controls controls;

    private const string INPUT_DEVICE_PLAYER = "InputDevideIDPlayer";
    private const string CONTROL_SCHEME_PLAYER = "ControlSchemePlayer";

    private List<Tuple<int, int, string>> playerSchemes = new List<Tuple<int, int, string>>();

    public GameObject _menuPlayerPrefab;

    public static InputManager Instance;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);

        }
    }

    private void Start()
    {
        //SpawnKeyboardPlayers();
    }

    //Assign control scheme that was read from the character selection
    public List<PlayerInput> AssignPlayerInput(List<PlayerInput> players, List<string> playerIds)
    {

        for (int i = 0; i < players.Count; i++)
        {
            //Read values from PlayerPrefs
            int inputDeviceId = PlayerPrefs.GetInt(INPUT_DEVICE_PLAYER + playerIds[i]);
            string controlScheme = PlayerPrefs.GetString(CONTROL_SCHEME_PLAYER + playerIds[i]);

            players[i].SwitchCurrentControlScheme(controlScheme, InputSystem.GetDeviceById(inputDeviceId));
        }

        return players;

    }

    //This will assign player control schemes on the order they are received
    // WASD, ZGHJ, PLOA, NUM
    //The return is in case you need to save the modified instance again. However it should still work without using the returned value
    public List<PlayerInput> AssignPlayerInput(List<PlayerInput> players)
    {

        controls = new Controls();

        List<InputControlScheme> controlSchemes = new List<InputControlScheme> {
            controls.KeyboardWASDScheme,
            controls.KeyboardZGHJScheme,
            controls.KeyboardPLÖÄScheme,
            controls.KeyboardNumScheme
        };

        for (int i = 0; i < players.Count; i++)
        {
            players[i].SwitchCurrentControlScheme(controlSchemes[i].name, Keyboard.current);
        }


        return players;

    }


    private void SavePlayerControlScheme(string playerId, int inputDeviceId, string controlScheme)
    {
        PlayerPrefs.SetInt(INPUT_DEVICE_PLAYER + playerId, inputDeviceId);
        PlayerPrefs.SetString(CONTROL_SCHEME_PLAYER + playerId, controlScheme);
    }

    //This should be called at the end of the selection screen
    public void SavePlayersControlSchemes()
    {
        //Iterate over all tuples
        foreach (Tuple<int, int, string> player in playerSchemes)
        {
            SavePlayerControlScheme(player.Item1.ToString(), player.Item2, player.Item3);
        }
    }

    public void AddControlScheme(Tuple<int, int, string> playerScheme)
    {
        //Check if the tuple is already in the list
        if (playerSchemes.Count == 0)
        {
            playerSchemes.Add(playerScheme);
        }
        else
        {
            if (!playerSchemes.Contains(playerScheme))
            {
                playerSchemes.Add(playerScheme);
            }
        }
    }


    /*
    private void GetPlayerControlScheme(string playerId)
    {
        int inputDeviceId = PlayerPrefs.GetInt(INPUT_DEVICE_PLAYER + playerId);
        string controlScheme = PlayerPrefs.GetString(CONTROL_SCHEME_PLAYER + playerId);

        //var playerInput = PlayerInput.Instantiate( controlScheme, inputDeviceId);
        
    }
    */


    //Work in progress
    //This will be used when players select their character at the start screen

    private void SpawnKeyboardPlayers()
    {
        controls = new Controls();

        PlayerControllerLobby player;
        player = PlayerInput.Instantiate(_menuPlayerPrefab).GetComponent<PlayerControllerLobby>();
        player.SetInputDeviceAndControlScheme(controls.KeyboardWASDScheme.name, Keyboard.current);

        player = PlayerInput.Instantiate(_menuPlayerPrefab).GetComponent<PlayerControllerLobby>();
        player.SetInputDeviceAndControlScheme(controls.KeyboardZGHJScheme.name, Keyboard.current);

        player = PlayerInput.Instantiate(_menuPlayerPrefab).GetComponent<PlayerControllerLobby>();
        player.SetInputDeviceAndControlScheme(controls.KeyboardPLÖÄScheme.name, Keyboard.current);

        player = PlayerInput.Instantiate(_menuPlayerPrefab).GetComponent<PlayerControllerLobby>();
        player.SetInputDeviceAndControlScheme(controls.KeyboardNumScheme.name, Keyboard.current);


    }

    /*
    private void SpawnControllerPlayers()
    {
        PlayerControllerLobby player;
        ReadOnlyArray<Gamepad> gamepads = Gamepad.all;

        for (int i = 0; i < gamepads.Count; i++)
        {
            player = PlayerInput.Instantiate(_menuPlayerPrefab).GetComponent<PlayerControllerLobby>();
            player.SwitchCurrentControlScheme(controls.GamepadScheme.name, Gamepad.all[i]);
            
        }
    } */


}

