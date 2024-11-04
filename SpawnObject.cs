using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SpawnObject : MonoBehaviour
{
    public GameObject goatPrefab;  
    public GameObject darkgoatPrefab; 
    public GameObject bearPrefab;
    public Button goatButton;  
    public Button darkgoatButton;  
    public Button bearButton;

    public Transform spawnPoint; 
    public GameObject uiPanel; 

    public float pickupRadius = 10f; 
    private GameObject pickedUpObject;
    public Transform holdPosition; 

    private InputAction spawnAction;
    private InputAction pickupAction;
    private InputAction storeAction;  
    private InputAction inventoryAction;
    private InputAction dropAction;
    private bool isUIPanelVisible = false;  

    private Inventory inventory = new Inventory();  // Inventory instance

    private void Awake()
    {
        // Create InputAction for spawning, picking up, and storing
        spawnAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/c");
        pickupAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/p");
        storeAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/t");  // 'T' key for storing
        inventoryAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/i");
        dropAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/l");

        // Enable the actions
        spawnAction.Enable();
        pickupAction.Enable();
        storeAction.Enable();
        inventoryAction.Enable();
        dropAction.Enable();

        // Ensure the UI panel and buttons are hidden at start
        uiPanel.SetActive(false);
        goatButton.gameObject.SetActive(false);
        darkgoatButton.gameObject.SetActive(false);
        bearButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Register input action callbacks
        spawnAction.performed += OnSpawnPerformed;
        pickupAction.performed += OnPickupPerformed;
        storeAction.performed += OnStorePerformed;  // Add store action callback
        inventoryAction.performed += OnInventoryPerformed;
        dropAction.performed += OnDropPerformed;

        // Add listener to the UI buttons for spawning objects
        goatButton.onClick.AddListener(SpawnGoat);
        darkgoatButton.onClick.AddListener(SpawnDarkGoat);
        bearButton.onClick.AddListener(SpawnBear);

        // Set the UI navigation mode to automatic (this allows arrow key navigation)
        SetButtonNavigation(goatButton);
        SetButtonNavigation(darkgoatButton);
        SetButtonNavigation(bearButton);
    }

    private void OnDisable()
    {
        // Unregister input action callbacks
        spawnAction.performed -= OnSpawnPerformed;
        pickupAction.performed -= OnPickupPerformed;
        storeAction.performed -= OnStorePerformed;  // Remove store action callback
        inventoryAction.performed -=OnInventoryPerformed;
        dropAction.performed -= OnDropPerformed;

        // Disable input actions
        spawnAction.Disable();
        pickupAction.Disable();
        storeAction.Disable();
        inventoryAction.Disable();
        dropAction.Disable();
    }

    private void Update()
    {
        if (pickedUpObject != null)
        {
            // Keep the picked-up object in hand if holding it
            pickedUpObject.transform.position = holdPosition.position;
        }
    }

    private void OnStorePerformed(InputAction.CallbackContext context)
    {
        // Store the currently held object if any
        if (pickedUpObject != null)
        {
            StoreInInventory();
        }
    }

    // Called when the "Spawn" action is triggered (C key by default)
    private void OnSpawnPerformed(InputAction.CallbackContext context)
    {
        // Toggle the visibility of the UI panel
        isUIPanelVisible = !isUIPanelVisible;
        uiPanel.SetActive(isUIPanelVisible);

        // Show or hide the cursor when UI is open/closed
        Cursor.visible = isUIPanelVisible;
        Cursor.lockState = isUIPanelVisible ? CursorLockMode.None : CursorLockMode.Locked;

        if (isUIPanelVisible)
        {
            // Show the buttons when the UI panel is visible
            pickupAction.Disable();
            storeAction.Disable();
            inventoryAction.Disable();
            dropAction.Disable();
            goatButton.gameObject.SetActive(true);
            darkgoatButton.gameObject.SetActive(true);
            bearButton.gameObject.SetActive(true);

            // Set the first button as selected when UI opens
            if (EventSystem.current.currentSelectedGameObject != goatButton.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(goatButton.gameObject);
                pickupAction.Enable();
                storeAction.Enable();
                inventoryAction.Enable();
                dropAction.Enable();
            }
        }
        else
        {
            // Hide the buttons when the UI panel is closed
            goatButton.gameObject.SetActive(false);
            darkgoatButton.gameObject.SetActive(false);
            bearButton.gameObject.SetActive(false);
            pickupAction.Enable();
            storeAction.Enable();
            inventoryAction.Enable();
            dropAction.Enable();
        }
    }

    // Called when the "Pickup" action is triggered (P key by default)
    private void OnPickupPerformed(InputAction.CallbackContext context)
    {
        // Try to pick up an object in range
        TryPickupObject();
    }

    private void OnInventoryPerformed(InputAction.CallbackContext context)
    {
        DisplayInventory();
    }

    private void OnDropPerformed(InputAction.CallbackContext context)
    {
        DropCurrentObject();
    }

    // Function to spawn a goat when the goat button is clicked
    public void SpawnGoat()
    {
        Instantiate(goatPrefab, spawnPoint.position, Quaternion.identity);
        CloseUIPanel();
    }

    // Function to spawn some other object when the other button is clicked
    public void SpawnDarkGoat()
    {
        Instantiate(darkgoatPrefab, spawnPoint.position, Quaternion.identity);
        CloseUIPanel();
    }

    public void SpawnBear()
    {
        Instantiate(bearPrefab, spawnPoint.position, Quaternion.identity);
        CloseUIPanel();
    }

    // Close the UI panel after selecting an option
    private void CloseUIPanel()
    {
        isUIPanelVisible = false;
        uiPanel.SetActive(false);

        // Hide the buttons when UI is closed
        goatButton.gameObject.SetActive(false);
        darkgoatButton.gameObject.SetActive(false);
        bearButton.gameObject.SetActive(false);

        // Hide the cursor when UI is closed
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Try to pick up an object within the pickup radius
    private void TryPickupObject()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Pickupable"))
            {
                // If already holding an object, drop it first
                if (pickedUpObject != null)
                {
                    DropCurrentObject();
                }

                // Pick up the new object
                pickedUpObject = collider.gameObject;

                // Optionally, deactivate physics (so the object doesn't fall or move around)
                Rigidbody rb = pickedUpObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // Move the object to the player's hand
                pickedUpObject.transform.SetParent(holdPosition);
                Debug.Log("Picked up: " + pickedUpObject.name);  // Log the picked-up object
                return;  // Exit after picking up one object
            }
        }
        Debug.Log("No pickupable object found within range.");  // Log if no object is found
    }

    // Drop the current object
    private void DropCurrentObject()
    {
        if (pickedUpObject != null)
        {
            // Reset physics and position
            Rigidbody rb = pickedUpObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            // Detach the object from the player's hand
            pickedUpObject.transform.SetParent(null);
            pickedUpObject = null;
            Debug.Log("Dropped the object.");  // Log when dropping the object
        }
    }

    // Store the object in the inventory
    private void StoreInInventory()
    {
        if (pickedUpObject != null)
        {
            inventory.AddItem(pickedUpObject);  // Add the object to the inventory
            pickedUpObject.SetActive(false);
            DropCurrentObject();
            pickedUpObject=null;
        }
    }

    // Display the inventory contents (call this method from somewhere, e.g., when a specific key is pressed)
    public void DisplayInventory()
    {
        inventory.DisplayInventory();
    }

    // Set automatic navigation for buttons to allow arrow key navigation
    private void SetButtonNavigation(Button button)
    {
        Navigation navigation = new Navigation
        {
            mode = Navigation.Mode.Automatic  // Enable automatic navigation
        };
        button.navigation = navigation;
    }
}
