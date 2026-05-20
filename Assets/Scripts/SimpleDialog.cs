using UnityEngine;
using UnityEngine.UI;

public class SimpleDialog : MonoBehaviour
{
    [Header("Настройки диалога")]
    public GameObject dialogCanvas;     
    public Text dialogText;              
    public string[] messages;            
    public float showDistance = 3f;      

    [Header("Управление")]
    public KeyCode interactKey = KeyCode.E;

    private Transform player;
    private int currentMessageIndex = 0;
    private bool isNearPlayer = false;
    private bool isDialogActive = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (dialogCanvas != null)
            dialogCanvas.SetActive(false);
    }

    void Update()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            isNearPlayer = distance <= showDistance;

            if (isNearPlayer && Input.GetKeyDown(interactKey))
            {
                ToggleDialog();
            }

            if (!isNearPlayer && isDialogActive)
            {
                HideDialog();
            }
        }
    }

    void ToggleDialog()
    {
        if (!isDialogActive)
        {
            ShowNextMessage();
        }
        else
        {
            currentMessageIndex++;
            if (currentMessageIndex < messages.Length)
            {
                ShowCurrentMessage();
            }
            else
            {
                HideDialog();
            }
        }
    }

    void ShowCurrentMessage()
    {
        if (dialogCanvas != null && dialogText != null)
        {
            dialogCanvas.SetActive(true);
            dialogText.text = messages[currentMessageIndex];
            isDialogActive = true;
        }
    }

    void ShowNextMessage()
    {
        currentMessageIndex = 0;
        ShowCurrentMessage();
    }

    void HideDialog()
    {
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
            isDialogActive = false;
            currentMessageIndex = 0;
        }
    }
}