using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemShop : MonoBehaviour
{
    public ItemDatabase itemDB;
    public Text priceText;
    public Text descriptionText;
    public Text InsufficientCreditsText;
    public Text inventoryFullText;
    public Transform itemShowCase; // location for item display

    public Vector3 windowPosition;
    public Vector3 locationThatIsNeverSeen;

    private int displayedItem = -1; // currently displayed item/in the last frame, index in the DB
    private int nextItemToDisplay = 0; // item which will be displayed in the next frame, index in the DB
    private bool isActive = false; // whether the shop is opened or not
    private GameObject displayedItemObject = null; // game object of the currently shown item

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // update only when active and when the displayed item was changed
        if (isActive && nextItemToDisplay != displayedItem) {
            displayedItem = nextItemToDisplay;

            if (displayedItemObject != null) {
                Destroy(displayedItemObject);
            }

            ItemD item = itemDB.getItem(displayedItem);
            displayedItemObject = Instantiate(item.inventoryPrefab, itemShowCase);

            priceText.text = item.getPrice().ToString();
            descriptionText.text = item.getDescription();
        }
    }

    // offset must be -1 or +1
    private void changeItem(int offset) {
        nextItemToDisplay = nextItemToDisplay + offset;
        if (nextItemToDisplay < 0) {
            nextItemToDisplay += itemDB.getItemCount(); 
        }
        else if (nextItemToDisplay >= itemDB.getItemCount()) {
            nextItemToDisplay -= itemDB.getItemCount();
        }
    }

    public void onLeft() {
        changeItem(-1);
    }

    public void onRight() {
        changeItem(+1);
    }

    public void open() {
        isActive = true;
        displayedItem = -1;
        nextItemToDisplay = 0;
        this.transform.position = windowPosition;
    }

    public void close() {
        isActive = false;
        this.transform.position = locationThatIsNeverSeen;
        Destroy(displayedItemObject);
    }

    public ItemD.Type getSelectedItem() {
        return itemDB.getItem(displayedItem).type;
    }
}
