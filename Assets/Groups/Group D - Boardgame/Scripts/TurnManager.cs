using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

public class TurnManager : MonoBehaviour
{
    public enum Directions {
        LEFT,
        RIGHT, 
        UP,
        DOWN,
    }

    // whether only a dummy minigame should be loaded, 
    // should be used for testing this scene 
    public bool useTestMinigames = false;

    // number of rounds until the game ends
    public int numberOfRounds = 10;

    // the four player controllers (used to animate the player figures)
    public List<BoardgameController> players = new List<BoardgameController>(4);
    // their associated data
    public List<PlayerData> playerData = new List<PlayerData>(4);


    // stores the credits, golden bricks, and items of each player
    public List<PlayerDisplay> playerBelongings = new List<PlayerDisplay>(4);
    
    // number of credits required to become the "true party person"
    public int truePartyThreshold = 10;

    public TrueParty truePartySprite;

    public GoldenBrickManager brickManager;

    public Transform playerMarkerTransform;

    public ItemShop itemShop;
    public ItemDatabase itemDB;

    public CameraMovement camera;

    public HUD hud;
    public InteractionMenu interactions;

    public float playerMarkerHoverDistance = 3.0f;
    public float playerMarkerBobbleSpeed = 2.0f;
    public float playerMarkerBobbleAmplitude = 0.5f;

    private int truePartyPerson = -1;
    private int activePlayer = 0;
    private int round = 1;
    private int actionPoints = 0;
    private double sleepTimeAI = 0.0;
    private bool wantsToUseItemAI = false;

    private enum TurnState {MOVING_CAM_TO_DIE, ROLLING_DIE, MOVING_CAM_TO_PLAYER, ACCEPTING_INPUT, EXECUTING_ACTION, SHOWING_SHOP, APPLYING_TILE_EFFECT, TURN_ENDED, MINIGAME, SCOREBOARD, SCOREBOARD_END,};
    private TurnState currentState;

    // currently executed FSM
    // FSMs are control various animations and are executed in some turn states
    private FSM currentActionFSM = null;

    // Start is called before the first frame update
    void Start() {
        restoreGameState();
        // moveToDie();

        if (useTestMinigames) {
            // replace the real minigame list with dummies
            GameList.FREE_FOR_ALL_LIST.Clear();
            GameList.FREE_FOR_ALL_LIST.Add(new TestingGame());
            GameList.SINGLE_VS_TEAM_LIST.Clear();
            GameList.SINGLE_VS_TEAM_LIST.Add(new TestingGame());
            GameList.TEAM_VS_TEAM_LIST.Clear();
            GameList.TEAM_VS_TEAM_LIST.Add(new TestingGame());
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (currentState == TurnState.MOVING_CAM_TO_DIE && camera.movementCompleted()) {
            rollDie();
        }
        else if (currentState == TurnState.ROLLING_DIE && DieScript.isDone() && DieScript2.isDone())
        {
            actionPoints = DieScript.rollResult + DieScript2.rollResult;

            currentState = TurnState.MOVING_CAM_TO_PLAYER;
            camera.moveToPlayer(activePlayer);
        }
        else if (currentState == TurnState.MOVING_CAM_TO_PLAYER && camera.movementCompleted()) {
            startNewTurn();
        }
        else if (currentState == TurnState.ACCEPTING_INPUT && isAI() && actionPoints > 0) {
            takeActionAI();
        }
        else if (currentState == TurnState.ACCEPTING_INPUT && actionPoints <= 0) {
            applyTileEffect();
        }
        else if (currentState == TurnState.EXECUTING_ACTION && (currentActionFSM == null || currentActionFSM.update()) && playerBelongings[activePlayer].animationsAreDone()) {
            currentActionFSM = null;
            if (!updateTruePartyState())
            {
                if (playerData[activePlayer].currentTile().hasTrap()&&!(playerData[activePlayer].currentTile().getTrapOwner().Equals(activePlayer)))
                {
                    int creditsToRemove = Math.Min(5,playerBelongings[activePlayer].creditAmount());
                    playerBelongings[activePlayer].addCreditAmount(-creditsToRemove);
                    actionPoints = Math.Max(actionPoints-3,0);
                    playerData[activePlayer].currentTile().setTrap(false);
                    currentState = TurnState.EXECUTING_ACTION;
                    currentActionFSM = new RemoveTrap(activePlayer);
                }
                else if (actionPoints <= 0) 
                {
                    // turn is over!
                    applyTileEffect();
                }
                else
                {
                    // await next input/action
                    currentState = TurnState.ACCEPTING_INPUT;
                }
            }
            
        }
        else if (currentState == TurnState.APPLYING_TILE_EFFECT && currentActionFSM.update()) {
            if (!updateTruePartyState()) {    
                currentState = TurnState.TURN_ENDED;
                currentActionFSM = null;
            }
        }
        else if (currentState == TurnState.TURN_ENDED) {
            finishTurn();
        }
        else if (currentState == TurnState.SCOREBOARD) {

            // TODO: replace with an amount of credits based on the rank each player achieved in the last minigame
            playerBelongings.ForEach((belongings) => {
                belongings.addCreditAmount((int) UnityEngine.Random.Range(0f, 3.99f));
            });

            // TODO: does not work yet
            // proposed solution: add a minigame state, when the minigame + scorescreen are over, check whether there is a true party person
            // then do animations etc (before starting the next turn)
            updateTruePartyState();

            currentState = TurnState.SCOREBOARD_END;
        }
        else if (currentState == TurnState.SCOREBOARD_END && 
            playerBelongings[0].animationsAreDone() && 
            playerBelongings[1].animationsAreDone() && 
            playerBelongings[2].animationsAreDone() && 
            playerBelongings[3].animationsAreDone() &&
            currentActionFSM.update()) {
           
            finishRound();
        }
        
        foreach (PlayerAction action in interactions.actions) {        
            if (currentState == TurnState.ACCEPTING_INPUT && actionPoints > 0) {
                action.updateStatus(actionIsActive(action), canAfford(action));
            }
            else {
                action.updateStatus(false, false);
            }
        }

        updateHUD();
    }

    /// call this when a round (4 player turns + minigame) is over; it will either start a new round or trigger the "game over"
    private void finishRound() {
        activePlayer = 0;
        round++;

        if (round >= numberOfRounds) {
            endGame();
        } 
        else {
            moveToDie();
        }
    }

    /// returns whether the current player is an AI
    private bool isAI() {
        // TODO: real implementation, this is a dummy for testing
        //return activePlayer > 1;
        return false;
    }

    // call this method whenever an FSM ended which might modify the credit amounts of players
    // returns true when state transition must be delayed because a player has become the true party person 
    // (an animation is played in this case)
    private bool updateTruePartyState() {
        if (truePartyPerson == -1) {
            // there is no true party person yet
            for (int i = 0; i < 4; i++) {
                if (playerBelongings[i].creditAmount() >= truePartyThreshold) {
                    truePartyPerson = i;

                    for (int j = 0; j < 4; j++) {
                        playerBelongings[j].setIsTruePartyPerson(j == truePartyPerson);
                    }

                    currentActionFSM = new BecomingTruePartyPerson();
                    return true;
                }
            }
        }
        return false;
    }

    /// returns whether the given action is "in principle" currently available for the player,
    /// i.e. whether the action could be used in this state independently of the action's costs (AP + credits)
    private bool actionIsActive(PlayerAction action) {
        switch (action.type) {
            case PlayerAction.Type.END_TURN:
                return true;
            case PlayerAction.Type.BUY_GOLDEN_BRICK:
                return playerData[activePlayer].currentTile().hasGoldenBrick();
            case PlayerAction.Type.ITEM_CREDIT_THIEF:
                return playerBelongings[activePlayer].hasItem(ItemD.Type.CREDIT_THIEF);
            case PlayerAction.Type.BUY_AP:
                return truePartyPerson == activePlayer;
            case PlayerAction.Type.SHOP:
                return playerData[activePlayer].currentTile().hasItemShop();
            case PlayerAction.Type.SET_TRAP:
                return (playerBelongings[activePlayer].hasItem(ItemD.Type.TRAP))&&(!(playerData[activePlayer].currentTile().type.Equals(Tile.TileType.START)));
        }
        return false;
    }

    /// returns whether the player can afford the given action (i.e. whether they have enough AP and credits)
    private bool canAfford(PlayerAction action) {
        return action.requiredAP <= actionPoints && action.requiredCredits <= playerBelongings[activePlayer].creditAmount();
    }

    private void finishAction() {
        if (actionPoints <= 0) {
            // turn is over!
            applyTileEffect();
            return;
        }

        currentState = TurnState.ACCEPTING_INPUT;
    }

    private void applyTileEffect() {
        currentState = TurnState.APPLYING_TILE_EFFECT;
        
        switch(playerData[activePlayer].currentTile().type) {
            case Tile.TileType.GAIN_COINS:
                currentActionFSM = new TileGainCoins(playerBelongings[activePlayer]);
                break;
            case Tile.TileType.LOSE_COINS:
                currentActionFSM = new TileLoseCoins(playerBelongings[activePlayer]);
                break;
            case Tile.TileType.RANDOM_EVENT:
                currentActionFSM = new TileRandomEvent();
                Debug.Log("Standing on a purple tile");
                break;
            case Tile.TileType.MASTER_HAND:
                currentActionFSM = new TileMasterHand();
                Debug.Log("Standing on an orange tile");
                break;
            case Tile.TileType.START:
                // do nothing -> skip to next state
                currentState = TurnState.TURN_ENDED;
                break;
        }
    }

    private void finishTurn() {

        if (activePlayer == 3) {
            loadMinigame();
            // state is changed when the boardgame scene is reloaded
            // do nothing in the MINIGAME state, i.e. game is "paused"
            currentState = TurnState.MINIGAME; 
        }
        else {
            // next player 
            // note the order: keep activePlayer in a valid range {0,1,2,3}
            activePlayer++;
            moveToDie();
        }
    }

    private void moveToDie() {
        camera.moveToDice();
        currentState = TurnState.MOVING_CAM_TO_DIE;
    }

    private void rollDie() {
        currentState = TurnState.ROLLING_DIE;
        DieScript.rollDie();
        DieScript2.rollDie();
    }

    private void endGame() {
        // pass some parameters to the next scene
        for (int i = 0; i < 4; i++) {
            EndScreen.playerStats[i] = new EndScreen.PlayerStats(i, playerBelongings[i].goldenBricks(), playerBelongings[i].creditAmount());
        }

        SceneManager.LoadScene("Assets/Groups/Group D - Boardgame/Scenes/EndScreen");
    }

    private void startNewTurn() {
        currentState = TurnState.ACCEPTING_INPUT;
    }

    private void updateHUD() {
        hud.updateRound(round);

        if (interactions.anActionIsSelected()) { // display the costs of the selected action 
            hud.updateActionPoints(actionPoints, interactions.getSelectedActionAPCost());
            playerBelongings[activePlayer].setDisplayedCreditCosts(interactions.getSelectedActionCreditCost());
        } 
        else if (currentState == TurnState.SHOWING_SHOP) {
            hud.updateActionPoints(actionPoints);
            int price = itemDB.getItem(itemShop.getSelectedItem()).getPrice();
            playerBelongings[activePlayer].setDisplayedCreditCosts(price);
            if(playerBelongings[activePlayer].creditAmount() < price){
                itemShop.InsufficientCreditsText.enabled = true;
            }
            else{
                itemShop.InsufficientCreditsText.enabled = false;
            }
            if(playerBelongings[activePlayer].hasSpaceForAnItem()){
                itemShop.inventoryFullText.enabled = false;
            }
            else{
                itemShop.inventoryFullText.enabled = true;
            }
            
        }
        else {
            hud.updateActionPoints(actionPoints);
            playerBelongings[activePlayer].setDisplayedCreditCosts(0);
        }

        for (int i = 0; i < 4; i++) {
            if (truePartyPerson == -1) {
                // no truePartyPerson yet
                playerBelongings[i].updateTruePartyMeter(((float) playerBelongings[i].creditAmount()) / truePartyThreshold);
            }
        }

        // marker other active player
        float offset = Mathf.Sin(Time.timeSinceLevelLoad * playerMarkerBobbleSpeed);
        offset *= playerMarkerBobbleAmplitude;
        offset += playerMarkerHoverDistance;

        Vector3 markerPos = playerData[activePlayer].transform.position;
        markerPos.y += offset;
        playerMarkerTransform.position = markerPos;
    }

    private void loadMinigame() {
        // TODO: this is a dummy implementation
        int team1 = 0;
        int team2 = 0;

        playerData.ForEach((data) => {
            Tile.TileType t = data.currentTile().type;

            if (t == Tile.TileType.GAIN_COINS || t == Tile.TileType.RANDOM_EVENT) {
                team1++;
            }
            else {
                team2++;
            }
        });


        preserveGameState();

        if (team1 == 0 || team2 == 0) {
            Debug.Log("Loading a free-for-all minigame.");
            LoadingManager.Instance.LoadMiniGame(MiniGameType.freeForAll);
        }
        else if (team1 == team2) {
            Debug.Log("Loading a 2v2 minigame.");
            LoadingManager.Instance.LoadMiniGame(MiniGameType.teamVsTeam);
        } 
        else {
            Debug.Log("Loading a 1v3 minigame.");
            LoadingManager.Instance.LoadMiniGame(MiniGameType.singleVsTeam);
        }
    }

    /// executes the given action
    private void executeAction(PlayerAction action) {
        if (action == null || !action.isUsable()) {
            return;
        }

        switch (action.type) {
            case PlayerAction.Type.END_TURN:
                actionPoints = 0;
                break;
            case PlayerAction.Type.BUY_GOLDEN_BRICK:
                playerBelongings[activePlayer].addGoldenBrick();
                players[activePlayer].PlayPickupSound();
                brickManager.relocate();
                currentActionFSM = null;
                break;
            case PlayerAction.Type.ITEM_CREDIT_THIEF:
                // item is "used" -> remove it from the inventory
                playerBelongings[activePlayer].removeItem(ItemD.Type.CREDIT_THIEF);
                currentState = TurnState.EXECUTING_ACTION;
                currentActionFSM = new ItemCreditThief(camera, activePlayer, playerBelongings);
                break;
            case PlayerAction.Type.SET_TRAP:
                playerData[activePlayer].currentTile().setTrap(true,activePlayer);
                currentState = TurnState.EXECUTING_ACTION;
                currentActionFSM = new SetTrap(activePlayer);
                playerBelongings[activePlayer].removeItem(ItemD.Type.TRAP);
                break;
            case PlayerAction.Type.BUY_AP:
                currentState = TurnState.EXECUTING_ACTION;
                currentActionFSM = null;
                break;
            case PlayerAction.Type.SHOP:
                currentState = TurnState.SHOWING_SHOP;
                currentActionFSM = null;
                itemShop.open();
                break;

        }

        actionPoints -= action.requiredAP;
        playerBelongings[activePlayer].addCreditAmount(-action.requiredCredits);
    }

    private void buyItem() {
        ItemD.Type item = itemShop.getSelectedItem();
        int price = itemDB.getItem(item).getPrice();

        if (playerBelongings[activePlayer].creditAmount() >= price && playerBelongings[activePlayer].hasSpaceForAnItem()) {
            itemShop.InsufficientCreditsText.enabled = false;
            // buy the item
            playerBelongings[activePlayer].addCreditAmount(-price);
            playerBelongings[activePlayer].addItem(item);

            // dirty hack/shortcut: just switch to the EXECUTING_ACTON state, the credit animation is savely finished there
            currentActionFSM = null;
            currentState = TurnState.EXECUTING_ACTION;

            itemShop.close();           
        }
        // else: cannot buy item
    }

    public void reactToMove(Directions direction, int playerNumber)
    {
        if (playerNumber != activePlayer || currentState != TurnState.ACCEPTING_INPUT) {
            return;
        }

        Tile nextTile = null;

        switch (direction) {
            case Directions.LEFT:
                nextTile = playerData[activePlayer].currentTile().left;
                break;
            case Directions.RIGHT:
                nextTile = playerData[activePlayer].currentTile().right;
                break;
            case Directions.UP:
                nextTile = playerData[activePlayer].currentTile().up;
                break;
             case Directions.DOWN:
                nextTile = playerData[activePlayer].currentTile().down;
                break;
        }

        tryToWalkTo(nextTile);
    }

    private void walkTo(Tile nextTile) {
        currentState = TurnState.EXECUTING_ACTION;
        actionPoints--;
        currentActionFSM = new Walking(players[activePlayer], playerData[activePlayer], nextTile);
    }

    private bool tryToWalkTo(Tile nextTile) {
        if (nextTile == null) {
            // there is no neighboring tile in this direction
            return false;
        }

        walkTo(nextTile);
        return true;
    }

    public void reactToNorth(int playerNumber)
    {
        if (playerNumber != activePlayer || isAI()) {
            return;
        }
       
        if (currentState == TurnState.SHOWING_SHOP) {
            itemShop.onLeft();
        }
        else if (currentState == TurnState.ACCEPTING_INPUT) {
            interactions.nextAction();
        }
    }

    public void reactToEast(int playerNumber)
    {
        if (playerNumber != activePlayer || isAI()) {
            return;
        }
       
        if (currentState == TurnState.SHOWING_SHOP) {
            buyItem();
        }
        else if (currentState == TurnState.ACCEPTING_INPUT) {
            executeAction(interactions.getSelectedAction());
        }
    }

    public void reactToWest(int playerNumber) {
        if ((playerNumber != activePlayer && currentState != TurnState.SHOWING_SHOP) || isAI()) {
            return;
        }

        itemShop.close();
        // dirty hack/shortcut: just switch to the EXECUTING_ACTON state, the credit animation is savely finished there
        currentActionFSM = null;
        currentState = TurnState.EXECUTING_ACTION;
    } 

    public void reactToSouth(int playerNumber)
    {
        if (playerNumber != activePlayer || isAI()) {
            return;
        }
       
        if (currentState == TurnState.SHOWING_SHOP) {
            itemShop.onRight();
        }
        else if (currentState == TurnState.ACCEPTING_INPUT) {
            interactions.previousAction();
        }
    }

    /// Called during the ACCEPTING_INPUT state when the current player
    /// is an AI. The AI must take an action in this method.
    /// The AI walks to the golden brick and buys it. When the AI has not enough
    /// credits, it aborts its turn. Along the way, it used items it received
    /// randomly.
    private void takeActionAI() {
        // used for a delay between actions taken
        if (sleepTimeAI > 0.0) {
            sleepTimeAI -= Time.deltaTime;
            return;
        }

        Tile brickTile = brickManager.getBrickTile();
        Tile currentTile = playerData[activePlayer].currentTile();

        // when the AI is currently using an item, do that
        if (wantsToUseItemAI) {
            useItemAI();
            
        }
        else if (currentTile == brickTile) {
            // already on the goal tile
            if (interactions.canUse(PlayerAction.Type.BUY_GOLDEN_BRICK)) {
                // golden brick can be bought
                executeActionAI(PlayerAction.Type.BUY_GOLDEN_BRICK);
            }
            else { 
                useItemAttemptAI(); // might use an item randomly when available

                if (!wantsToUseItemAI) {
                    // cannot afford the golden brick, abort the turn
                    executeActionAI(PlayerAction.Type.END_TURN);
                }
            }
        }
        else {
            // when standing on the start tile, move to the next neighbor
            if (currentTile.type == Tile.TileType.START) {
                if (!tryToWalkTo(currentTile.right)) {
                    if (!tryToWalkTo(currentTile.left)) {
                        if (!tryToWalkTo(currentTile.up)) {
                            if (!tryToWalkTo(currentTile.down)) {
                                Debug.Log("The start tile is not connected to any neighbor tiles. This must not happen!");
                            }
                        }
                    }
                }
            }
            // move the the brick
            else { 
                useItemAttemptAI(); // might use an item randomly when available

                if (!wantsToUseItemAI) {
                    // move to the golden brick
                    walkTo(currentTile.nextOnPathToBrick());
                }
            }
        }

        sleepTimeAI = 0.5; // wait 0.5 seconds before the next action is taken
    }

    /// AI attempts to use an item; if an item should be used, wantsToUseItemAI is set to true.
    private void useItemAttemptAI() {
        if (UnityEngine.Random.value > 0.75 && playerBelongings[activePlayer].hasAnItem()) {
            PlayerAction.Type useItemAction = itemDB.getItem(playerBelongings[activePlayer].getFirstItem()).associatedAction;
            if (interactions.canUse(useItemAction)) {
                wantsToUseItemAI = true;
            }
        }
        else {
            wantsToUseItemAI = false; 
        }
    }

    /// AI uses an item
    private void useItemAI() {
        PlayerAction.Type useItemAction = itemDB.getItem(playerBelongings[activePlayer].getFirstItem()).associatedAction;
        // use an item
        executeActionAI(useItemAction);
    }

    private void executeActionAI(PlayerAction.Type action) {
        if (!interactions.anActionIsSelected()) {
            return;
        }
        
        if (interactions.getSelectedAction().type == action) {
            // buy the golden brick
            executeAction(interactions.getSelectedAction());
            wantsToUseItemAI = false; 
        }
        else {
            // wrong action is selected, next action
            interactions.nextAction();
        }
    }




     // this method is responsible for storing the game state before a minigame is loaded
    // the state is stored in the StatePreserver singleton
    // make sure that you add anything which must be stored between minigames
    private void preserveGameState() {
        StatePreserver.Instance.boardState = new StatePreserver.BoardState();

        StatePreserver.Instance.gameStarted = false;
        StatePreserver.Instance.boardState.truePartyPerson = truePartyPerson;
        StatePreserver.Instance.boardState.round = round;

        foreach (GameObject t in GameObject.FindGameObjectsWithTag("Tile")) { // for each tile
            if ((t.GetComponent(typeof(Tile)) as Tile).hasGoldenBrick()) {
                StatePreserver.Instance.boardState.brickTile = new StatePreserver.TileCoord();
                StatePreserver.Instance.boardState.brickTile.x = t.transform.position.x;
                StatePreserver.Instance.boardState.brickTile.z = t.transform.position.z;
            }

            // TODO: preserve traps
        }

        StatePreserver.Instance.playerStates = new List<StatePreserver.PlayerState>();
        for (int i = 0; i < 4; i++) {
            StatePreserver.Instance.playerStates.Add(new StatePreserver.PlayerState());
            StatePreserver.Instance.playerStates[i].position = players[i].getPlayerPosition();
            StatePreserver.Instance.playerStates[i].credits = playerBelongings[i].creditAmount();
            StatePreserver.Instance.playerStates[i].bricks = playerBelongings[i].goldenBricks();
            StatePreserver.Instance.playerStates[i].items = playerBelongings[i].getItems();
            StatePreserver.Instance.playerStates[i].currentTile = new StatePreserver.TileCoord();
            StatePreserver.Instance.playerStates[i].currentTile.x = playerData[i].currentTile().transform.position.x;
            StatePreserver.Instance.playerStates[i].currentTile.z = playerData[i].currentTile().transform.position.z;
            StatePreserver.Instance.playerStates[i].distanceWalked = playerData[i].getDistanceWalked();
        }
    }

    // this method is responsible for restoring the game state after a minigame has ended
    // the special handling of the first round (no previous state) is also handled here
    // make sure that you restore anything you have saved in preserveGameState() here 
    private void restoreGameState() {
        if (StatePreserver.Instance.gameStarted) {
            Debug.Log("First round!");
            // very first round
            moveToDie();
        }
        else {
            Debug.Log("Returning from a minigame!");

            // a minigame just ended, show the scoreboard
            
            currentState = TurnState.SCOREBOARD;

            // TODO: is the scoreboard implemented by group C or by us?

            // restore the last state
            // ======================

            // restore general data
            truePartyPerson = StatePreserver.Instance.boardState.truePartyPerson;
            round = StatePreserver.Instance.boardState.round;

            // restore the tiles, i.e. golden brick, trap and player locations
            foreach (GameObject t in GameObject.FindGameObjectsWithTag("Tile")) { // for each tile
                Tile tile = (t.GetComponent(typeof(Tile)) as Tile);

                if (t.transform.position.x == StatePreserver.Instance.boardState.brickTile.x && 
                    t.transform.position.z == StatePreserver.Instance.boardState.brickTile.z) {

                    brickManager.restore(tile, t.transform);

                    // TODO: restore traps
                }

                for (int i = 0; i < 4; i++) {
                    if (t.transform.position.x == StatePreserver.Instance.playerStates[i].currentTile.x && 
                        t.transform.position.z == StatePreserver.Instance.playerStates[i].currentTile.z) {

                        playerData[i].moveTo(tile);
                        players[i].TeleportTo(StatePreserver.Instance.playerStates[i].position);
                    }
                }
            }

            // restore player data and belongings except the tiles/positions, this was done above
            for (int i = 0; i < 4; i++) {
                foreach (ItemD.Type item in StatePreserver.Instance.playerStates[i].items) {
                    playerBelongings[i].addItem(item);
                }

                playerBelongings[i].restore(StatePreserver.Instance.playerStates[i].credits, StatePreserver.Instance.playerStates[i].bricks);
                playerData[i].restore(StatePreserver.Instance.playerStates[i].distanceWalked);
            }
        }
    }
}
