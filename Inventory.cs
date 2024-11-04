using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    private List<GameObject> items = new List<GameObject>();

    public void AddItem(GameObject item)
    {
        items.Add(item);
        Debug.Log("Added to inventory: " + item.name);
    }

    public void RemoveItem(GameObject item)
    {
        items.Remove(item);
        Debug.Log("Removed from inventory: " + item.name);
    }

    public void DisplayInventory()
    {
        if (items.Count == 0)
        {
            Debug.Log("Inventory is empty.");
            return;
        }

        Debug.Log("Inventory Contents:");
        foreach (var item in items)
        {
            Debug.Log("- " + item.name);
        }
        Debug.Log("Total items: " + items.Count);
    }
}
