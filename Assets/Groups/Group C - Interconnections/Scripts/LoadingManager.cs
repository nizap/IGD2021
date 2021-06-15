using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;
    private Text freeForAll;
    private Text singleVsTeam;
    private Text teamVsTeam;
    private Text banner;

    private string nextScene;

    public GameObject _randomPicker;

    //this method is just for testing, it must be removed at the end.
    void Start()
    {   
        
    }

    private void Awake () {
        if(!Instance) {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        } else {
            Destroy(this.gameObject);
        }
    }

    public void LoadMiniGame (MiniGameType miniGameType) {

        int gameType = 0;
        List<MiniGame> games = null;

        switch (miniGameType)
        {
            case MiniGameType.freeForAll: 
                gameType = 0;
                games = getRandomElements(GameList.FREE_FOR_ALL_LIST); 
                break;
            case MiniGameType.singleVsTeam : 
                gameType = 1;
                games = getRandomElements(GameList.SINGLE_VS_TEAM_LIST); 
                break;
            case MiniGameType.teamVsTeam: 
                gameType = 2;
                games = getRandomElements(GameList.TEAM_VS_TEAM_LIST); 
                break;
        }

        int selectedGame = Random.Range(0, games.Count);
        this.nextScene = games[selectedGame].getSceneName();

        this.showPicker(gameType, games, selectedGame);
    }

    private List<MiniGame> getRandomElements(List<MiniGame> games)
    {
        int number = 0;
        bool flag = true;
        List<MiniGame> elements = new List<MiniGame>();
        List<int> indexes = new List<int>();

        int idx = Random.Range(0, games.Count);

        elements.Add(games[idx]);
        indexes.Add(idx);

        while(elements.Count<3){
            number = Random.Range(0, games.Count);
            for (int i = 0; i < indexes.Count; i++)
            {
                if(indexes[i] != number){
                    elements.Add(games[number]);
                    indexes.Add(number);
                    break;
                }
            }
        }
        return elements;
    }

    public void LoadMainBoardGame() {
        //Load main board if we are not in it
        SceneManager.LoadSceneAsync(GameList.MAIN_BOARD_SCENE);
    }
    
    private void DisplayLoadingScreen() {

    }

    private void HideLoadingScreen() {
        //MyGame game = new MyGame("My Awesome Game", "SceneName", MiniGameType.freeForAll);
        //game.sceneName = "WERR";
    }

    //Display UI that shows roulette to select from a random game
    private void showPicker(int gameType, List<MiniGame> games, int selectedGame) {
        var randomPicker = Instantiate(_randomPicker);
        //randomPicker.SetActive(true);

        freeForAll = GameObject.Find("FreeForAll").GetComponent<Text>();
        singleVsTeam = GameObject.Find("SingleVsTeam").GetComponent<Text>();
        teamVsTeam = GameObject.Find("TeamVsTeam").GetComponent<Text>();
        banner = GameObject.Find("Banner").GetComponent<Text>();
        banner.enabled= false;

        StartCoroutine(randomPickerAnimation(gameType, GameList.GAMES[gameType], games, selectedGame));
    }

    private IEnumerator randomPickerAnimation(int index, string bannerText, List<MiniGame> games, int selectedGame)
    {
        int counter = 0;

        while(true)
        {   
            teamVsTeam.color = Color.black;
            freeForAll.color = Color.red;
            if (index == 0) {
                counter+=1;
                if(counter == 5){
                    break;
                }
            }
            yield return new WaitForSeconds(.2f);
            freeForAll.color = Color.black;
            singleVsTeam.color = Color.red;
            if (index == 1) {
                counter+=1;
                if(counter == 5){
                    break;
                }
            }
            yield return new WaitForSeconds(.2f);
            singleVsTeam.color = Color.black;
            teamVsTeam.color = Color.red;
            if (index == 2) {
                counter+=1;
                if(counter == 5){
                    break;
                }
            }
            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(1f);
        //Show banner of selected game type
        freeForAll.enabled = false;
        singleVsTeam.enabled= false;
        teamVsTeam.enabled= false;
        banner.enabled= true;

        banner.text = bannerText;
        yield return new WaitForSeconds(1f);

        //Show game options
        banner.enabled= false;
        freeForAll.text = games[0].getDisplayName();
        singleVsTeam.text = games[1].getDisplayName();
        teamVsTeam.text = games[2].getDisplayName();

        freeForAll.enabled = true;
        singleVsTeam.enabled= true;
        teamVsTeam.enabled= true;

        StartCoroutine(randomPickerGameAnimation(selectedGame, games[selectedGame].getDisplayName()));
        
    }

    private IEnumerator randomPickerGameAnimation(int index, string bannerText)
    {
        int counter = 0;

        while(true)
        {   
            teamVsTeam.color = Color.black;
            freeForAll.color = Color.red;
            if (index == 0) {
                counter+=1;
                if(counter == 5){
                    break;
                }
            }
            yield return new WaitForSeconds(.2f);
            freeForAll.color = Color.black;
            singleVsTeam.color = Color.red;
            if (index == 1) {
                counter+=1;
                if(counter == 5){
                    break;
                }
            }
            yield return new WaitForSeconds(.2f);
            singleVsTeam.color = Color.black;
            teamVsTeam.color = Color.red;
            if (index == 2) {
                counter+=1;
                if(counter == 5){
                    break;
                }
            }
            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(1f);
        singleVsTeam.enabled= false;
        teamVsTeam.enabled= false;
        freeForAll.enabled= false;

        //show selected game
        banner.text = bannerText;
        banner.enabled= true;
        yield return new WaitForSeconds(1f);
        SceneManager.LoadSceneAsync(this.nextScene);
    }

}


