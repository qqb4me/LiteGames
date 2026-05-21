using UnityEngine;

public class DoorWithoutE : MonoBehaviour  
{
    [Tooltip("Имя сцены для загрузки при подходе игрока к двери.")]
    public string имяЦелевойСцены = "AlchemistHome";

    [Tooltip("Идентификатор точки появления в целевой сцене.")]
    public string идентификаторТочкиПоявления = "default";

    [Tooltip("Расстояние, на котором игрок будет телепортирован.")]
    public float дистанцияТелепортации = 2f;

    [Tooltip("Задержка перед телепортацией (в секундах).")]
    public float задержкаТелепортации = 0f;

    [Tooltip("Ссылка на трансформ игрока (определяется автоматически, если не указано).")]
    public Transform трансформИгрока;

    private bool ужеТелепортирован = false;
    private bool выполняетсяТелепортация = false;

    void Start()
    {
        if (трансформИгрока == null)
        {
            GameObject игрок = GameObject.FindGameObjectWithTag("Player");
            if (игрок != null)
                трансформИгрока = игрок.transform;
        }
    }

    void Update()
    {
        if (выполняетсяТелепортация) return;
        if (ужеТелепортирован) return;

        if (string.IsNullOrWhiteSpace(имяЦелевойСцены))
            return;

        if (трансформИгрока == null)
            return;

        float расстояние = Vector2.Distance(transform.position, трансформИгрока.position);

        if (расстояние <= дистанцияТелепортации)
        {
            if (задержкаТелепортации > 0f)
                StartCoroutine(ТелепортироватьСЗадержкой());
            else
                Телепортировать();
        }
    }

    void Телепортировать()
    {
        if (выполняетсяТелепортация) return;
        выполняетсяТелепортация = true;
        ужеТелепортирован = true;

        GameSession.Instance?.SetPendingSpawnPoint(идентификаторТочкиПоявления);
        GameSession.LoadScene(имяЦелевойСцены);
    }

    System.Collections.IEnumerator ТелепортироватьСЗадержкой()
    {
        if (выполняетсяТелепортация) yield break;
        выполняетсяТелепортация = true;
        ужеТелепортирован = true;

        yield return new WaitForSeconds(задержкаТелепортации);

        GameSession.Instance?.SetPendingSpawnPoint(идентификаторТочкиПоявления);
        GameSession.LoadScene(имяЦелевойСцены);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, дистанцияТелепортации);
    }
}