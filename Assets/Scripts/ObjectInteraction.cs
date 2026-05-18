using UnityEngine;
using TMPro;

public class ObjectInteraction : MonoBehaviour
{
    public TextMeshProUGUI targetText;

    [TextArea(3, 10)]
    public string firstCharacterText;

    [TextArea(3, 10)]
    public string secondCharacterText;

    private bool isPlayerNearby = false;

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            ChangeText();
        }
    }

    void ChangeText()
    {
        if (targetText != null)
        {
            targetText.text = firstCharacterText + "/" + secondCharacterText;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
}