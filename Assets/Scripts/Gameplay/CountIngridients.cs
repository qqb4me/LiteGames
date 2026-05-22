using UnityEngine;

public class HideUntilEnoughIngredients : MonoBehaviour
{
    public enum HideMode
    {
        SetActiveFalse,
        DisableRenderers,
        DisableCanvasGroups
    }

    [Header("Настройки")]
    public GameObject объектДляСкрытия;
    public HideMode режимСокрытия = HideMode.SetActiveFalse;

    private GameInventoryManager инвентарь;
    private bool объектСкрыт = false;

    void Start()
    {
        инвентарь = FindAnyObjectByType<GameInventoryManager>();

        if (инвентарь == null)
        {
            Debug.LogError("GameInventoryManager не найден!");
            return;
        }

        if (объектДляСкрытия == null)
            объектДляСкрытия = gameObject;

        if (инвентарь.AreAllIngredientsCollected())
        {
            СкрытьОбъект();
        }
    }

    void Update()
    {
        if (инвентарь != null && инвентарь.AreAllIngredientsCollected() && !объектСкрыт)
        {
            СкрытьОбъект();
        }
    }

    private void СкрытьОбъект()
    {
        if (объектДляСкрытия == null || объектСкрыт) return;

        switch (режимСокрытия)
        {
            case HideMode.SetActiveFalse:
                объектДляСкрытия.SetActive(false);
                break;

            case HideMode.DisableRenderers:
                var renderers = объектДляСкрытия.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                    r.enabled = false;

                var uiGraphics = объектДляСкрытия.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var g in uiGraphics)
                    g.enabled = false;
                break;

            case HideMode.DisableCanvasGroups:
                var groups = объектДляСкрытия.GetComponentsInChildren<CanvasGroup>(true);
                foreach (var cg in groups)
                {
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
                break;
        }

        объектСкрыт = true;
        Debug.Log($"Все предметы собраны. Объект {объектДляСкрытия.name} скрыт!");
    }
}