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
    public int Direction { get; private set; } = 0; // 0-5 для шести направлений в гексагональной сетке
    public int MaxTurnAngle = 1; // Максимальное изменение направления за ход (в единицах направления)

    [Header("References")]
    public Transform ModelTransform;
    public MeshRenderer ShipMesh;
    public Image HealthBarImage; // Изображение вместо слайдера для полосы здоровья

    [Header("Materials")]
    public Material PlayerMaterial;
    public Material EnemyMaterial;
    public Material OutlineMaterial; // Материал контура

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
    private List<GameObject> outlineSegments = new List<GameObject>();
    
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

        // Установка начального направления
        Direction = PlayerOwned ? 0 : 3; // 0 для игрока (восток), 3 для врага (запад)

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

        // Поворачиваем модель в зависимости от принадлежности и направления
        if (ModelTransform != null)
        {
            ModelTransform.rotation = Quaternion.Euler(0, Direction * 60, 0);
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
        
        // Очищаем старые сегменты контура
        ClearOutlineSegments();
        
        // Создаем новый контур вокруг модели
        if (ModelTransform != null)
        {
            CreateOutline(ModelTransform, OutlineMaterial);
        }
        
        Debug.Log("Корабль " + gameObject.name + " выбран");
            
        // Вызываем событие выбора
        OnSelected?.Invoke(this);
    }

    private void CreateOutline(Transform modelTransform, Material outlineMat)
    {
        MeshFilter meshFilter = modelTransform.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Mesh mesh = meshFilter.sharedMesh;
            
            GameObject outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(transform);
            outlineObj.transform.position = modelTransform.position;
            outlineObj.transform.rotation = modelTransform.rotation;
            outlineObj.transform.localScale = modelTransform.lossyScale * 1.05f;
            
            MeshFilter outlineMeshFilter = outlineObj.AddComponent<MeshFilter>();
            outlineMeshFilter.mesh = mesh;
            
            MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
            outlineRenderer.material = outlineMat;
            
            // Настройка рендеринга контура
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            
            outlineSegments.Add(outlineObj);
        }
        
        // Рекурсивно обрабатываем дочерние объекты
        foreach (Transform child in modelTransform)
        {
            CreateOutline(child, outlineMat);
        }
    }

    private void ClearOutlineSegments()
    {
        foreach (var segment in outlineSegments)
        {
            if (segment != null)
                Destroy(segment);
        }
        
        outlineSegments.Clear();
    }

    public void Deselect()
    {
        if (!isSelected) return;
        
        isSelected = false;
        
        // Удаляем все сегменты контура
        ClearOutlineSegments();
            
        Debug.Log("Выделение корабля " + gameObject.name + " отменено");
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    // Метод для поворота корабля
    public bool TryRotate(int newDirection)
    {
        // Проверяем, находится ли новое направление в пределах допустимого поворота
        int turnAngle = Mathf.Abs((newDirection - Direction + 6) % 6);
        turnAngle = Mathf.Min(turnAngle, 6 - turnAngle);
        
        if (turnAngle <= MaxTurnAngle)
        {
            Direction = newDirection;
            
            // Обновляем визуальное направление модели
            if (ModelTransform != null)
            {
                ModelTransform.rotation = Quaternion.Euler(0, Direction * 60, 0);
            }
            
            return true;
        }
        
        return false;
    }

    public void MoveToPosition(Vector3Int newPos, int newDirection)
    {
        Debug.Log($"Перемещаем корабль {name} из {HexPosition} в {newPos}, направление: {Direction} -> {newDirection}");
        
        // Проверяем, возможен ли поворот
        if (!TryRotate(newDirection))
        {
            Debug.LogWarning("Невозможно повернуть корабль на требуемый угол");
            return;
        }
        
        Vector3Int oldPos = HexPosition;
        HexPosition = newPos;
        
        // Обновляем мировую позицию
        Vector3 worldPos = GameManager.Instance.hexGrid.CalculateHexPosition(newPos.x, newPos.y, newPos.z);
        worldPos.y = 0.3f; 
        
        // Плавное перемещение
        StartCoroutine(SmoothMovement(worldPos));
        
        // Уменьшаем очки движения
        MovementPoints -= GameManager.Instance.hexGrid.GetMovementCost(newPos, this);
        
        // Вызываем событие перемещения
        OnMoved?.Invoke(this, oldPos, newPos);
    }

    private System.Collections.IEnumerator SmoothMovement(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float speed = 5.0f; // Скорость движения
        float distanceCovered = 0.0f;
        
        // Сохраняем начальную и конечную ротацию
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(0, Direction * 60, 0); // 60 градусов на каждое направление
        
        while (distanceCovered < journeyLength)
        {
            float fractionOfJourney = distanceCovered / journeyLength;
            
            // Интерполируем позицию
            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            
            // Интерполируем поворот
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, fractionOfJourney);
            
            distanceCovered += speed * Time.deltaTime;
            yield return null;
        }
        
        // Финальная позиция и ротация
        transform.position = targetPosition;
        transform.rotation = endRotation;
    }

    public void FireAt(Vector3Int targetPos)
    {
        if (!CanAttack)
        {
            Debug.LogWarning("Корабль не может атаковать в этот ход");
            return;
        }
        
        Debug.Log($"Корабль {name} стреляет по позиции {targetPos}");
        
        // Проверяем, что цель в пределах дальности стрельбы
        int distance = GameManager.Instance.hexGrid.CalculateHexDistance(HexPosition, targetPos);
        if (distance > AttackRange)
        {
            Debug.LogWarning("Цель вне зоны досягаемости");
            return;
        }
        
        // Получаем линию огня
        List<Vector3Int> firingLine = GameManager.Instance.hexGrid.DrawLine(HexPosition, targetPos);
        
        // Проверяем каждую клетку на линии
        bool hitSomething = false;
        
        foreach (var pos in firingLine)
        {
            // Пропускаем первую клетку (где находится сам стреляющий)
            if (pos == HexPosition)
                continue;
            
            // Проверяем, есть ли корабль в этой клетке
            if (GameManager.Instance.hexGrid.hexGrid.TryGetValue(pos, out Hex hex) && hex.OccupyingShip != null)
            {
                Ship targetShip = hex.OccupyingShip;
                
                // Наносим урон (дружественный огонь тоже работает)
                targetShip.TakeDamage(AttackDamage);
                
                // Создаем визуальный эффект попадания
                GameManager.Instance.CreateExplosion(pos);
                
                hitSomething = true;
                break; // Снаряд останавливается при попадании
            }
            
            // Если достигли цели и ничего не попали, создаем эффект промаха
            if (pos == targetPos && !hitSomething)
            {
                GameManager.Instance.CreateSplash(pos);
            }
        }
        
        // Корабль не может больше атаковать в этот ход
        CanAttack = false;
    }

    public void Attack(Ship target)
    {
        if (target == null || !CanAttack)
            return;
            
        // Поворачиваем корабль к цели
        Vector3 direction = target.transform.position - transform.position;
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
        float angle = Vector3.SignedAngle(Vector3.forward, flatDirection, Vector3.up);
        transform.rotation = Quaternion.Euler(0, angle, 0);
        
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
            yield return