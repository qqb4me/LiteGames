using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class QuickDialog3 : MonoBehaviour
{
    [Header("Диалоговые окна")]
    public GameObject[] dialogCanvases = new GameObject[3];
    public TMP_Text[] dialogTexts = new TMP_Text[3];

    [Header("Список фраз")]
    public DialogMessage[] messages;

    private int currentMessageIndex = 0;
    private bool isDialogActive = false;

    [System.Serializable]
    public class DialogMessage
    {
        [Range(1, 3)]  // Ползунок от 1 до 3
        public int dialogNumber = 1;

        [TextArea(2, 4)]
        public string message = "Текст...";
    }

    void Start()
    {
        foreach (var canvas in dialogCanvases)
        {
            if (canvas != null) canvas.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleDialog();
        }
    }

    void ToggleDialog()
    {
        if (!isDialogActive)
        {
            currentMessageIndex = 0;
            ShowCurrentMessage();
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
                HideAllDialogs();
            }
        }
    }

    void ShowCurrentMessage()
    {
        DialogMessage msg = messages[currentMessageIndex];

        // Скрываем все окна
        HideAllDialogs();

        // Показываем выбранное окно (1, 2 или 3)
        int index = msg.dialogNumber - 1;
        if (index >= 0 && index < dialogCanvases.Length)
        {
            if (dialogCanvases[index] != null && dialogTexts[index] != null)
            {
                dialogCanvases[index].SetActive(true);
                dialogTexts[index].text = msg.message;
            }
        }

        isDialogActive = true;
    }

    void HideAllDialogs()
    {
        foreach (var canvas in dialogCanvases)
        {
            if (canvas != null) canvas.SetActive(false);
        }
        isDialogActive = false;
    }
}
