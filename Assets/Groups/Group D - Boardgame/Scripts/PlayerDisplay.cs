using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDisplay : MonoBehaviour
{
    public Text creditDisplay;          // text display for the credit amount
    public Text brickDisplay;           // text display for the golden brick amount
    public TurnAnimationCredit credit;  // credit in the HUD
    public TurnAnimationBrick brick;    // golden brick in the HUD
    public StudBar trueParyBar;         // the bar used to display true party progress

    public List<Transform> inventorySlots; // 3 inventory slots

    public ItemDatabase itemDB; // the item data base

    private int currentCreditCost = 0; // currently displayed credit costs

    // number of credits/bricks which the player has currently
    private int credits = 0;
    private int bricks = 0;

    private List<ItemD.Type> items = new List<ItemD.Type>(3);
    private List<Object> itemObjects = new List<Object>(3);

    // used to add/remove credits/bricks in an animation, controlled by a FSM
    private int creditsToAdd = 0;
    private int bricksToAdd = 0;
    private double creditClock = 0.0;
    private double brickClock = 0.0;
    private GainingAnimation creditAnimationState = GainingAnimation.STOP;
    private GainingAnimation brickAnimationState = GainingAnimation.STOP;

    private enum GainingAnimation { // animation states for adding/removing golden bricks or credits
        START_GAINING,  // add a credit/golden brick
        START_LOSING,   // remove a credit/brick
        BOBBING,        // brick/credit is "bobbing"
        PAUSE,          // pause after bobbing
        STOP,           // the desired amount has been added/removed
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        animateCreditBobbing();
        animateBrickBobbing();

        string creditText =  ": " + credits.ToString();
        if (currentCreditCost > 0) {
            creditText += " (-" + currentCreditCost.ToString() + ")";
        }

        creditDisplay.text = creditText;
        brickDisplay.text = ": " + bricks.ToString();
    }

    // call this method to indicate that the "true party person" state has been achieved
    // use the parameter to indicate whether it has been achieved by this player or not
    public void setIsTruePartyPerson(bool isItThisPlayer) {
        trueParyBar.setStateTaken(isItThisPlayer);
    }

    /// updates the true party meter of this player
    /// pass a value between 0 and 1, 0 is an empty meter, 1 a full one
    public void updateTruePartyMeter(float newRatio) {
        trueParyBar.setFillRatio(newRatio);
    }

    // adds the given item to this player's inventory
    // does nothing, when the inventory is full
    public void addItem(ItemD.Type itemType) {
        ItemD item = itemDB.getItem(itemType);
        if (items.Count < 3) {
            itemObjects.Add(Instantiate(item.inventoryPrefab, inventorySlots[items.Count]));
            items.Add(itemType);
        }
    }

    /// whether the player's inventory contains the given item
    public bool hasItem(ItemD.Type itemType) {
        return items.Contains(itemType);
    }

    /// whether the player has still space for one item
    public bool hasSpaceForAnItem() {
        return items.Count < 3;
    }

    /// whether the player has at least one item
    public bool hasAnItem() {
        return items.Count > 0;
    }

    /// returns the first item the player currently has
    /// ensure that he hasAnItem() before calling this method
    public ItemD.Type getFirstItem() {
        return items[0];
    }

    /// returns all items this player currently has
    public List<ItemD.Type> getItems() {
        List<ItemD.Type> res = new List<ItemD.Type>();

        foreach (ItemD.Type item in items) {
            res.Add(item);
        }

        return res;
    }

    // removes the given item from this player's inventory
    // does nothing, when the inventory does not contain the item
    public void removeItem(ItemD.Type itemType) {
        if (hasItem(itemType)) {
            int index = items.IndexOf(itemType);
            items.RemoveAt(index);
            Destroy(itemObjects[index]);
            itemObjects.RemoveAt(index);
        }
    }

    /// returns true if and only if all animations have been completed
    public bool animationsAreDone() {
        return creditAnimationState == GainingAnimation.STOP && brickAnimationState == GainingAnimation.STOP;
    }

    /// updates the displayed credit costs for this player
    /// set newCosts = 0 to stop displaying costs
    public void setDisplayedCreditCosts(int newCosts) {
        currentCreditCost = newCosts;
    }

    /// adds the given amount of credits to this player
    /// use negative amounts for substraction
    /// DO NOT call this method when animationsAreDone() == false
    /// (an animation is used to add the amount over time)
    public void addCreditAmount(int amount) {
        if (amount != 0) {
            creditsToAdd += amount;
            creditAnimationState = creditsToAdd > 0 ? GainingAnimation.START_GAINING : GainingAnimation.START_LOSING;
        }
    }

    /// gives this player a golden brick
    /// DO NOT call this method when animationsAreDone() == false
    /// (an animation is used to add the brick)
    public void addGoldenBrick() {
        bricksToAdd++;
        brickAnimationState = GainingAnimation.START_GAINING;
    }

    /// returns the number of credits this player currently has
    public int creditAmount() {
        return credits;
    }

    /// returns the number of golden bricks this player currently has
    public int goldenBricks() {
        return bricks;
    }

    // this method is used to restore a previous state after a minigame
    public void restore(int numberOfCredits, int numberOfBricks) {
        credits = numberOfCredits;
        bricks = numberOfCredits;
    }

    private void animateCreditBobbing() {
        creditClock += Time.deltaTime;

        switch (creditAnimationState) {
            case GainingAnimation.START_GAINING:
                credit.setBobbing(true);
                creditsToAdd--;
                credits++;
                creditAnimationState = GainingAnimation.BOBBING;
                break;
            case GainingAnimation.START_LOSING:
                credit.setBobbing(true);
                creditsToAdd++;
                credits--;
                creditAnimationState = GainingAnimation.BOBBING;
                break;
            case GainingAnimation.BOBBING:
                if (creditClock > 0.4) {
                    credit.setBobbing(false);
                    creditAnimationState = GainingAnimation.PAUSE;
                }
                break;
            case GainingAnimation.PAUSE:
                if (creditClock > 0.5) {
                    if (creditsToAdd != 0) {
                        // add/remove the next credit
                        creditAnimationState = creditsToAdd > 0 ? GainingAnimation.START_GAINING : GainingAnimation.START_LOSING;
                        creditClock -= 0.5;
                    }
                    else {
                        // the desired amount of credits was added/removed
                        creditAnimationState = GainingAnimation.STOP;
                    }
                }
                break;
            case GainingAnimation.STOP:
                creditClock = 0.0; // prevent ugly large values
                break;
        }
    }

    private void animateBrickBobbing() {
        brickClock += Time.deltaTime;

        switch (brickAnimationState) {
            case GainingAnimation.START_GAINING:
                brick.setBobbing(true);
                bricksToAdd--;
                bricks++;
                brickAnimationState = GainingAnimation.BOBBING;
                break;
            case GainingAnimation.START_LOSING:
                brick.setBobbing(true);
                bricksToAdd++;
                bricks--;
                brickAnimationState = GainingAnimation.BOBBING;
                break;
            case GainingAnimation.BOBBING:
                if (brickClock > 0.4) {
                    brick.setBobbing(false);
                    brickAnimationState = GainingAnimation.PAUSE;
                }
                break;
            case GainingAnimation.PAUSE:
                if (brickClock > 0.5) {
                    if (bricksToAdd != 0) {
                        brickAnimationState = bricksToAdd > 0 ? GainingAnimation.START_GAINING : GainingAnimation.START_LOSING;
                        brickClock -= 0.5;
                    }
                    else {
                        brickAnimationState = GainingAnimation.STOP;
                    }
                }
                break;
            case GainingAnimation.STOP:
                brickClock = 0.0; // prevent ugly large values
                break;
        }
    }

}
