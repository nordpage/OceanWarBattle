using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum ShipType { Battleship, Cruiser, Destroyer, Submarine }

public class Ship : MonoBehaviour
{
    [Header("Ship Properties")]
    public ShipType Type;
    public bool PlayerOwned;
    public int Health;
    public int MaxHealth;
    public int MovementPoints;
    public int MaxMovement;
    public int AttackRange;
    public int AttackDamage;
    public bool IsStealth;
    public Vector3Int HexPosition;
    public bool CanAttack = true;

    [Header("References")]
    public Transform ModelTransform;
    public MeshRenderer ShipMesh;
    public Image HealthBarImage; // Изображение вместо слайдера для полосы здоровья

    [Header("Materials")]
    public Material PlayerMaterial;
    public Material EnemyMaterial;

    [Header("Selection Effect")]
    public GameObject SelectionOutline; // Визуальный эффект выделения (отдельный объект)
    public Color SelectionEmissionColor = Color.yellow; // Цвет свечения при выделении
    public float EmissionIntensity = 2.0f; // Сила свечения
    public float SelectionScaleFactor = 1.05f; // Множитель увеличения при выделении

    // События
    public System.Action<Ship> OnSelected;
    public System.Action<Ship, Vector3Int, Vector3Int> OnMoved;
    public System.Action<Ship, Ship> OnAttacked;
    public System.Action<Ship> OnDestroyed;

    // Приватные поля
    private bool isSelected = false;
    private Material originalMaterial;
    private Vector3 originalScale;
    private Dictionary<string, int> activeEffects = new Dictionary<string, int>();
    
    // Кэшированные ID свойств шейдера для эффективности
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionEnabled = Shader.PropertyToID("_EMISSION");

    private void Awake()
    {
        // Если не назначены ссылки, попробуем найти компоненты
        if (ModelTransform == null)
            ModelTransform = transform.Find("Model");
            
        if (ShipMesh == null && ModelTransform != null)
            ShipMesh = ModelTransform.GetComponent<MeshRenderer>();
            
        if (HealthBarImage == null)
        {
            Image img = GetComponentInChildren<Image>();
            if (img != null && img.type == Image.Type.Filled)
                HealthBarImage = img;
        }
        
        // Отключаем эффект выделения при старте
        if (SelectionOutline != null)
            SelectionOutline.SetActive(false);
            
        // Сохраняем оригинальный размер
        originalScale = transform.localScale;
    }

    void Start()
    {
        // Проверяем наличие коллайдера
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("На корабле " + gameObject.name + " отсутствует коллайдер. Добавляем BoxCollider.");
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            
            // Настраиваем размер коллайдера, если есть MeshRenderer
            if (ShipMesh != null)
            {
                boxCol.center = ShipMesh.bounds.center - transform.position;
                boxCol.size = ShipMesh.bounds.size;
            }
        }
    }

    // Включаем режим отладки для проверки коллайдера
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }

    public void Initialize(ShipType type, bool playerOwned)
    {
        Type = type;
        PlayerOwned = playerOwned;

        // Настройка параметров в зависимости от типа
        switch (type)
        {
            case ShipType.Battleship:
                MaxHealth = 150;
                Health = MaxHealth;
                MaxMovement = 2;
                MovementPoints = MaxMovement;
                AttackRange = 3;
                AttackDamage = 40;
                break;
            case ShipType.Cruiser:
                MaxHealth = 100;
                Health = MaxHealth;
                MaxMovement = 3;
                MovementPoints = MaxMovement;
                AttackRange = 2;
                AttackDamage = 25;
                break;
            case ShipType.Destroyer:
                MaxHealth = 75;
                Health = MaxHealth;
                MaxMovement = 4;
                MovementPoints = MaxMovement;
                AttackRange = 1;
                AttackDamage = 15;
                break;
            case ShipType.Submarine:
                MaxHealth = 85;
                Health = MaxHealth;
                MaxMovement = 3;
                MovementPoints = MaxMovement;
                AttackRange = 1;
                AttackDamage = 30;
                IsStealth = true;
                break;
        }

        // Сохраняем оригинальный материал
        if (ShipMesh != null)
            originalMaterial = PlayerOwned ? PlayerMaterial : EnemyMaterial;

        UpdateVisuals();
        UpdateHealthBar();
    }

    public void UpdateVisuals()
    {
        if (ShipMesh != null)
        {
            // Устанавливаем материал в зависимости от владельца
            ShipMesh.material = PlayerOwned ? PlayerMaterial : EnemyMaterial;
            
            // Обновляем оригинальный материал
            originalMaterial = ShipMesh.material;
        }

        // Поворачиваем модель в зависимости от принадлежности
        if (ModelTransform != null)
        {
            ModelTransform.rotation = Quaternion.Euler(0, PlayerOwned ? 0 : 180, 0);
        }
    }

    public void UpdateHealthBar()
    {
        if (HealthBarImage != null)
        {
            // Устанавливаем fillAmount для отображения здоровья
            HealthBarImage.fillAmount = (float)Health / MaxHealth;
            
            // Меняем цвет полоски здоровья
            if ((float)Health / MaxHealth > 0.7f)
                HealthBarImage.color = Color.green;
            else if ((float)Health / MaxHealth > 0.3f)
                HealthBarImage.color = Color.yellow;
            else
                HealthBarImage.color = Color.red;
        }
    }

    public void Select()
    {
        isSelected = true;
        
        // Активируем визуальный контур выделения
        if (SelectionOutline != null)
            SelectionOutline.SetActive(true);
            
        // Включаем эмиссию на материале для эффекта свечения
        if (ShipMesh != null && ShipMesh.material.HasProperty(EmissionColor))
        {
            ShipMesh.material.EnableKeyword("_EMISSION");
            ShipMesh.material.SetColor(EmissionColor, SelectionEmissionColor * EmissionIntensity);
        }
        
        // Немного увеличиваем корабль для визуального эффекта
        transform.localScale = originalScale * SelectionScaleFactor;
            
        Debug.Log("Корабль " + gameObject.name + " выбран");
            
        // Вызываем событие выбора
        OnSelected?.Invoke(this);
    }

    public void Deselect()
    {
        if (!isSelected) return;
        
        isSelected = false;
        
        // Деактивируем визуальный контур выделения
        if (SelectionOutline != null)
            SelectionOutline.SetActive(false);
            
        // Выключаем эмиссию на материале
        if (ShipMesh != null && ShipMesh.material.HasProperty(EmissionColor))
        {
            ShipMesh.material.DisableKeyword("_EMISSION");
        }
        
        // Возвращаем исходный размер
        transform.localScale = originalScale;
            
        Debug.Log("Выделение корабля " + gameObject.name + " отменено");
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public void Attack(Ship target)
    {
        if (target == null || !CanAttack)
            return;
            
        // Поворачиваем корабль к цели
        Vector3 direction = target.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
        
        // Запускаем визуальный эффект атаки
        StartCoroutine(AttackAnimation(target));
        
        // Вызываем событие атаки
        OnAttacked?.Invoke(this, target);
            
        // Рассчитываем урон
        int damage = CalculateDamage(target);
        target.TakeDamage(damage);
        
        // Применяем специальные эффекты в зависимости от типа корабля
        ApplySpecialEffects(target);
        
        // Указываем, что в этот ход больше нельзя атаковать
        CanAttack = false;
    }

    private System.Collections.IEnumerator AttackAnimation(Ship target)
    {
        // Простая анимация атаки - небольшое движение к цели и обратно
        Vector3 startPos = transform.position;
        Vector3 direction = (target.transform.position - startPos).normalized;
        Vector3 attackPos = startPos + direction * 0.3f;
        
        // Движение вперед
        float duration = 0.15f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, attackPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Движение назад
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(attackPos, startPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Восстанавливаем позицию
        transform.position = startPos;
    }

    private int CalculateDamage(Ship target)
    {
        float baseDamage = AttackDamage;
        
        // Модификаторы урона в зависимости от типов кораблей
        if (Type == ShipType.Destroyer && target.Type == ShipType.Submarine)
            baseDamage *= 1.5f;  // Эсминцы эффективны против подлодок
            
        if (Type == ShipType.Battleship && target.Type == ShipType.Cruiser)
            baseDamage *= 1.3f;  // Броненосцы эффективны против крейсеров
            
        if (Type == ShipType.Submarine && target.Type == ShipType.Battleship)
            baseDamage *= 1.4f;  // Подлодки эффективны против броненосцев
            
        // Случайный фактор (±10%)
        float randomFactor = 0.9f + Random.value * 0.2f;
        
        return Mathf.RoundToInt(baseDamage * randomFactor);
    }

    private void ApplySpecialEffects(Ship target)
    {
        switch (Type)
        {
            case ShipType.Battleship:
                // Шанс поджечь корабль
                if (Random.value < 0.3f)
                {
                    target.ApplyEffect("fire", 2);  // Горение на 2 хода
                    Debug.Log("Корабль " + target.gameObject.name + " подожжен!");
                }
                break;
                
            case ShipType.Cruiser:
                // Шанс замедлить корабль
                if (Random.value < 0.4f)
                {
                    target.ApplyEffect("slow", 1);  // Замедление на 1 ход
                    Debug.Log("Корабль " + target.gameObject.name + " замедлен!");
                }
                break;
                
            case ShipType.Destroyer:
                // Шанс обнаружить подлодку
                if (target.Type == ShipType.Submarine && target.IsStealth)
                {
                    target.IsStealth = false;
                    Debug.Log("Подлодка " + target.gameObject.name + " обнаружена!");
                }
                break;
                
            case ShipType.Submarine:
                // Гарантированный критический удар
                target.TakeDamage(AttackDamage / 2);  // Дополнительный урон
                Debug.Log("Корабль " + target.gameObject.name + " получил критический урон!");
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        Health = Mathf.Max(0, Health - damage);
        UpdateHealthBar();
        
        Debug.Log(gameObject.name + " получил " + damage + " урона. Осталось здоровья: " + Health);
        
        // Визуальный эффект получения урона (мигание красным)
        StartCoroutine(DamageFlashEffect());
        
        // Проверяем уничтожение
        if (Health <= 0)
            Destroy();
    }

    private System.Collections.IEnumerator DamageFlashEffect()
    {
        if (ShipMesh != null)
        {
            // Сохраняем цвет текущего материала
            Color originalColor = ShipMesh.material.color;
            
            // Делаем корабль красным
            ShipMesh.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            
            // Возвращаем оригинальный цвет
            ShipMesh.material.color = originalColor;
        }
    }

    public void Destroy()
    {
        Debug.Log("Корабль " + gameObject.name + " уничтожен!");
        
        // Анимация уничтожения
        StartCoroutine(DestroyAnimation());
        
        // Вызываем событие уничтожения
        OnDestroyed?.Invoke(this);
            
        // Удаляем корабль из списка
        if (PlayerOwned)
            GameManager.Instance.RemovePlayerShip(this);
        else
            GameManager.Instance.RemoveEnemyShip(this);
            
        // Удаляем объект после анимации
        Destroy(gameObject, 2.0f);
    }

    public void MoveToPosition(Vector3Int newPos)
{
    Debug.Log($"Перемещаем корабль {name} из {HexPosition} в {newPos}");
    Vector3Int oldPos = HexPosition;
    HexPosition = newPos;
    
    // Событие перемещения
    OnMoved?.Invoke(this, oldPos, newPos);
}

    private System.Collections.IEnumerator DestroyAnimation()
    {
        // Анимация уничтожения: уменьшение размера и прозрачность
        float duration = 1.0f;
        float elapsed = 0.0f;
        Vector3 startScale = transform.localScale;
        
        // Добавляем эффект покачивания
        float rotationSpeed = 180.0f; // градусов в секунду
        
        while (elapsed < duration)
        {
            // Уменьшаем размер
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            
            // Покачиваем корабль
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            
            // Делаем прозрачным, если есть материал
            if (ShipMesh != null && ShipMesh.material.HasProperty("_Color"))
            {
                Color color = ShipMesh.material.color;
                color.a = Mathf.Lerp(1, 0, elapsed / duration);
                ShipMesh.material.color = color;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void ApplyEffect(string effectName, int duration)
    {
        activeEffects[effectName] = duration;
        
        // Применяем эффект
        switch (effectName)
        {
            case "fire":
                // Горение наносит урон каждый ход
                TakeDamage(10);
                break;
                
            case "slow":
                // Замедление уменьшает скорость
                MovementPoints = Mathf.Max(MovementPoints - 1, 0);
                MaxMovement = Mathf.Max(MaxMovement - 1, 1);
                break;
                
            case "stealth":
                // Скрытность (для подлодок)
                IsStealth = true;
                break;
        }
        
        // Визуальные эффекты для состояний
        StartCoroutine(ShowEffectVisual(effectName));
    }
    
    private System.Collections.IEnumerator ShowEffectVisual(string effectName)
    {
        GameObject effectObject = null;
        
        switch (effectName)
        {
            case "fire":
                // Создаем простой эффект огня (красные частицы)
                effectObject = new GameObject("FireEffect");
                effectObject.transform.SetParent(transform);
                effectObject.transform.localPosition = Vector3.up * 0.5f;
                
                ParticleSystem firePS = effectObject.AddComponent<ParticleSystem>();
                var main = firePS.main;
                main.startColor = new Color(1, 0.3f, 0, 0.7f);
                main.startSize = 0.3f;
                main.startLifetime = 0.5f;
                main.startSpeed = 1f;
                
                var emission = firePS.emission;
                emission.rateOverTime = 10;
                
                break;
                
            case "slow":
                // Визуальный эффект замедления (синие частицы)
                effectObject = new GameObject("SlowEffect");
                effectObject.transform.SetParent(transform);
                effectObject.transform.localPosition = Vector3.up * 0.2f;
                
                ParticleSystem slowPS = effectObject.AddComponent<ParticleSystem>();
                var slowMain = slowPS.main;
                slowMain.startColor = new Color(0, 0.5f, 1, 0.7f);
                slowMain.startSize = 0.2f;
                slowMain.startLifetime = 0.8f;
                slowMain.startSpeed = 0.5f;
                
                var slowEmission = slowPS.emission;
                slowEmission.rateOverTime = 5;
                
                break;
        }
        
        // Ждем, пока эффект активен
        if (effectObject != null)
        {
            yield return new WaitForSeconds(1.5f);
            Destroy(effectObject);
        }
    }

    public void ProcessEffects()
    {
        // Создаем список эффектов для удаления
        List<string> effectsToRemove = new List<string>();
        
        foreach (KeyValuePair<string, int> effect in activeEffects)
        {
            string effectName = effect.Key;
            int remainingDuration = effect.Value - 1;
            
            if (remainingDuration <= 0)
            {
                effectsToRemove.Add(effectName);
                
                // Убираем эффект
                switch (effectName)
                {
                    case "slow":
                        MaxMovement += 1;  // Восстанавливаем скорость
                        Debug.Log(gameObject.name + ": эффект замедления закончился");
                        break;
                        
                    case "stealth":
                        if (Type == ShipType.Submarine)
                        {
                            IsStealth = false;
                            Debug.Log(gameObject.name + ": эффект скрытности закончился");
                        }
                        break;
                        
                    case "fire":
                        Debug.Log(gameObject.name + ": эффект горения закончился");
                        break;
                }
            }
            else
            {
                activeEffects[effectName] = remainingDuration;
                
                // Применяем эффект на этот ход
                switch (effectName)
                {
                    case "fire":
                        Debug.Log(gameObject.name + " получает урон от огня");
                        TakeDamage(10);  // Горение наносит урон
                        break;
                }
            }
        }
        
        // Удаляем истекшие эффекты
        foreach (string effect in effectsToRemove)
        {
            activeEffects.Remove(effect);
        }
    }

    public void ResetForNewTurn()
    {
        // Сбрасываем параметры для нового хода
        MovementPoints = MaxMovement;
        CanAttack = true;
        
        // Обрабатываем активные эффекты
        ProcessEffects();
        
        Debug.Log(gameObject.name + ": ход сброшен, MovementPoints=" + MovementPoints);
    }

    void OnMouseDown()
    {
        // Отладочная информация
        Debug.Log("Клик по " + gameObject.name + ", PlayerOwned: " + PlayerOwned);
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance не найден!");
            return;
        }
        
        Debug.Log("IsPlayerTurn: " + GameManager.Instance.IsPlayerTurn);
        
        // Обработка клика по кораблю
        if (PlayerOwned && GameManager.Instance.IsPlayerTurn)
        {
            // Переключаем выделение (toggle)
            if (isSelected)
            {
                Deselect();
                
                // Уведомляем GameManager об отмене выбора
                GameManager.Instance.OnShipDeselected(this);
            }
            else
            {
                Select();
            }
        }
    }

    // Для отладки наведения мыши
    void OnMouseEnter()
    {
        Debug.Log("Курсор наведен на " + gameObject.name);
    }

    void OnMouseExit()
    {
        Debug.Log("Курсор покинул " + gameObject.name);
    }

    public Dictionary<string, int> GetActiveEffects()
    {
        // Возвращает копию словаря активных эффектов
        return new Dictionary<string, int>(activeEffects);
    }
}