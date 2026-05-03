using UnityEngine;

public class HideUntilEnoughIngredients : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Сколько ингредиентов нужно собрать")]
    public int нужноИнгредиентов = 5;

    [Tooltip("Объект, который нужно скрывать (если не указан - скрывается этот объект)")]
    public GameObject объектДляСкрытия;

    [Tooltip("Проверять каждый кадр (если false - проверка только при старте и при сборе)")]
    public bool проверятьКаждыйКадр = false;

    private GameInventoryManager инвентарь;
    private bool ужеСкрыт = false;

    void Start()
    {
        // Находим менеджер инвентаря
        инвентарь = FindAnyObjectByType<GameInventoryManager>();

        if (инвентарь == null)
        {
            Debug.LogError("GameInventoryManager не найден!");
            return;
        }

        // Если объект для скрытия не указан, скрываем этот объект
        if (объектДляСкрытия == null)
        {
            объектДляСкрытия = gameObject;
        }

        // Проверяем при старте
        ПроверитьИнгредиенты();
    }

    void Update()
    {
        if (проверятьКаждыйКадр)
        {
            ПроверитьИнгредиенты();
        }
    }

    // Этот метод можно вызывать из других скриптов (например, при сборе ингредиента)
    public void ПроверитьИнгредиенты()
    {
        if (инвентарь == null) return;
        if (ужеСкрыт) return;

        // Получаем количество собранных ингредиентов через рефлексию
        int собрано = ПолучитьКоличествоИнгредиентов();

        if (собрано >= нужноИнгредиентов)
        {
            // Скрываем объект
            объектДляСкрытия.SetActive(false);
            ужеСкрыт = true;
            Debug.Log($"Собрано {собрано}/{нужноИнгредиентов}. Объект {объектДляСкрытия.name} скрыт!");
        }
    }

    private int ПолучитьКоличествоИнгредиентов()
    {
        // Получаем private поле _collectedCount через рефлексию
        var поле = typeof(GameInventoryManager).GetField("_collectedCount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (поле != null)
        {
            return (int)поле.GetValue(инвентарь);
        }

        return 0;
    }
}