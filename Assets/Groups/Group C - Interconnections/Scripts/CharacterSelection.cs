using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class CharacterSelection : MonoBehaviour
{
	public GameObject[] characters;
	public int selectedCharacter = 0; //index: stores information of which character we have actually selected
    public string characterName;
    public GameObject inputField;
    public GameObject nameDisplay0;
    public GameObject nameDisplay1;
    public GameObject nameDisplay2;
    public GameObject nameDisplay3;

    
    //Naming Characters and storing their names
    public void StoreName()
    {
        characterName = inputField.GetComponent<Text>().text;
        if (selectedCharacter == 0)
        {
            nameDisplay0.GetComponent<Text>().text = "PLAYER " + selectedCharacter + " " + "_NAME" + " " + characterName;
        }

        else if (selectedCharacter == 1)
        {
            nameDisplay1.GetComponent<Text>().text = "PLAYER " + selectedCharacter + " " + "_NAME" + " " + characterName;
        }

        else if (selectedCharacter == 2)
        {
            nameDisplay2.GetComponent<Text>().text = "PLAYER " + selectedCharacter + " " + "_NAME" + " " + characterName;
        }
            
        else if (selectedCharacter == 3)
        {
            nameDisplay3.GetComponent<Text>().text = "PLAYER " + selectedCharacter + " " + "_NAME" + " " + characterName;
        }
       
    }


	public void NextCharacter()
	{
		characters[selectedCharacter].SetActive(false);
		selectedCharacter = (selectedCharacter + 1) % characters.Length;
		characters[selectedCharacter].SetActive(true);
	}

	public void PreviousCharacter()
	{
		characters[selectedCharacter].SetActive(false);
		selectedCharacter--;
		if (selectedCharacter < 0)
		{
			selectedCharacter += characters.Length;
		}
		characters[selectedCharacter].SetActive(true);
	}

	public void StartGame()
	{
		PlayerPrefs.SetString("PLAYER" + selectedCharacter.ToString() + "_NAME", characterName);
		//SceneManager.LoadScene(1, LoadSceneMode.Single);
	}
}
