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
    [Tooltip("Сколько ингредиентов нужно собрать")]
    public int нужноИнгредиентов = 5;

    [Tooltip("Объект, который нужно скрывать (если не указан - скрывается этот объект)")]
    public GameObject объектДляСкрытия;

    [Tooltip("Проверять каждый кадр (если false - проверка только при старте и при сборе)")]
    public bool проверятьКаждыйКадр = false;

    [Tooltip("Как скрывать объект: полностью отключить GameObject или только визуальные компоненты")]
    public HideMode режимСокрытия = HideMode.DisableRenderers;

    private GameInventoryManager инвентарь;
    private bool ужеСкрыт = false;

    void Start()
    {
        
        инвентарь = FindAnyObjectByType<GameInventoryManager>();

        if (инвентарь == null)
        {
            Debug.LogError("GameInventoryManager не найден!");
            return;
        }

        
        if (объектДляСкрытия == null)
        {
            объектДляСкрытия = gameObject;
        }

        
        инвентарь.OnCollectedChanged += OnInventoryChanged;

        
        ПроверитьИнгредиенты();
    }

    void OnDestroy()
    {
        if (инвентарь != null)
            инвентарь.OnCollectedChanged -= OnInventoryChanged;
    }

    void Update()
    {
        if (проверятьКаждыйКадр)
        {
            ПроверитьИнгредиенты();
        }
    }

    void OnInventoryChanged(int count)
    {
        ПроверитьИнгредиенты();
    }

    
    public void ПроверитьИнгредиенты()
    {
        if (инвентарь == null) return;
        if (ужеСкрыт) return;

        int собрано = ПолучитьКоличествоИнгредиентов();

        if (собрано >= нужноИнгредиентов)
        {
            HideTarget();
            ужеСкрыт = true;
            Debug.Log($"Собрано {собрано}/{нужноИнгредиентов}. Объект {объектДляСкрытия.name} скрыт!");
        }
    }

    private void HideTarget()
    {
        if (объектДляСкрытия == null) return;

        switch (режимСокрытия)
        {
            case HideMode.SetActiveFalse:
                объектДляСкрытия.SetActive(false);
                break;

            case HideMode.DisableRenderers:
                var renderers = объектДляСкрытия.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.enabled = false;
                }
                
                var uis = объектДляСкрытия.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var g in uis)
                {
                    g.enabled = false;
                }
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
    }

    private int ПолучитьКоличествоИнгредиентов()
    {
        
        try
        {
            if (инвентарь != null)
            {
                return инвентарь.CollectedCount;
            }
        }
        catch { }

        
        var поле = typeof(GameInventoryManager).GetField("_collectedCount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (поле != null && инвентарь != null)
        {
            return (int)поле.GetValue(инвентарь);
        }

        return 0;
    }
}