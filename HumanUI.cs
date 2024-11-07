using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class HumanUI : MonoBehaviour
{
    private InputAction humanUIScreen;
    private bool mouseOn = false;
    private bool humanClickUI = false;
    public TMP_Dropdown taskDropdown; 

    public GameObject taskUIPanel; 
    private HumaneAI currentAIObject; 

    void Start()
    {
        humanUIScreen = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/v");
        humanUIScreen.Enable();
        humanUIScreen.performed += ToggleMouseVisibility;
        
        // Define task options and add them to the TMP_Dropdown
        List<string> tasks = new List<string> { "Idle", "Collect Resources", "Build", "Attack" };
        taskDropdown.ClearOptions();
        taskDropdown.AddOptions(tasks);
        
        // Add listener for task selection
        taskDropdown.onValueChanged.AddListener(delegate { OnTaskSelected(); });
        
        // Initially hide the dropdown and task panel
        taskDropdown.gameObject.SetActive(false);
        taskUIPanel.SetActive(false);
    }

    void Update()
    {
        if (mouseOn && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick();
        }
    }

    private void ToggleMouseVisibility(InputAction.CallbackContext context)
    {
        mouseOn = !mouseOn;
        Cursor.visible = mouseOn;
        Cursor.lockState = mouseOn ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            HumaneAI humanAI = hit.transform.GetComponent<HumaneAI>();
            if (humanAI != null)
            {
                ShowUIPanel(humanAI); 
            }
        }
    }

    public void ShowUIPanel(HumaneAI humanAI)
    {
        taskUIPanel.SetActive(true); 
        taskDropdown.gameObject.SetActive(true);  // Show the dropdown
        currentAIObject = humanAI;
        humanClickUI = true;
        taskDropdown.value = 0; // Reset dropdown selection
    }

    private void OnTaskSelected()
    {
        if (currentAIObject != null)
        {
            string selectedTask = taskDropdown.options[taskDropdown.value].text;
            currentAIObject.SetTask(selectedTask);
            CloseUIPanel();
        }
    }

    private void CloseUIPanel()
    {
        taskUIPanel.SetActive(false); 
        taskDropdown.gameObject.SetActive(false);  // Hide the dropdown
        currentAIObject = null;
        humanClickUI = false;
    }

    private void OnDisable()
    {
        humanUIScreen.Disable();
    }
}
