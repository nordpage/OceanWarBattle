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
    public GameObject SelectionIndicator;
    public Slider HealthBar;
    public MeshRenderer ShipMesh;

    [Header("Materials")]
    public Material PlayerMaterial;
    public Material EnemyMaterial;

    // События
    public System.Action<Ship> OnSelected;
    public System.Action<Ship, Vector3Int, Vector3Int> OnMoved;
    public System.Action<Ship, Ship> OnAttacked;
    public System.Action<Ship> OnDestroyed;

    // Словарь активных эффектов: имя эффекта -> оставшиеся ходы
    private Dictionary<string, int> activeEffects = new Dictionary<string, int>();

    private void Awake()
    {
        if (SelectionIndicator != null)
            SelectionIndicator.SetActive(false);
            
        // Если не назначены ссылки, попробуем найти компоненты
        if (ModelTransform == null)
            ModelTransform = transform.Find("Model");
            
        if (ShipMesh == null && ModelTransform != null)
            ShipMesh = ModelTransform.GetComponent<MeshRenderer>();
            
        if (HealthBar == null)
            HealthBar = GetComponentInChildren<Slider>();
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

        UpdateVisuals();
        UpdateHealthBar();
    }

    public void UpdateVisuals()
    {
        if (ShipMesh != null)
        {
            // Устанавливаем материал в зависимости от владельца
            ShipMesh.material = PlayerOwned ? PlayerMaterial : EnemyMaterial;
        }

        // Поворачиваем модель в зависимости от принадлежности
        if (ModelTransform != null)
        {
            ModelTransform.rotation = Quaternion.Euler(0, PlayerOwned ? 0 : 180, 0);
        }
    }

    public void UpdateHealthBar()
    {
        if (HealthBar != null)
        {
            HealthBar.value = (float)Health / MaxHealth;
            
            // Меняем цвет полоски здоровья
            Image fillImage = HealthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if ((float)Health / MaxHealth > 0.7f)
                    fillImage.color = Color.green;
                else if ((float)Health / MaxHealth > 0.3f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
    }

    public void Select()
    {
        if (SelectionIndicator != null)
            SelectionIndicator.SetActive(true);
            
        // Вызываем событие выбора
        if (OnSelected != null)
            OnSelected(this);
    }

    public void Deselect()
    {
        if (SelectionIndicator != null)
            SelectionIndicator.SetActive(false);
    }

    public void Attack(Ship target)
    {
        if (target == null || !CanAttack)
            return;
            
        // Поворачиваем корабль к цели
        Vector3 direction = target.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
        
        // Эффект выстрела (здесь можно добавить вызов визуальных эффектов)
        
        // Вызываем событие атаки
        if (OnAttacked != null)
            OnAttacked(this, target);
            
        // Рассчитываем урон
        int damage = CalculateDamage(target);
        target.TakeDamage(damage);
        
        // Применяем специальные эффекты в зависимости от типа корабля
        ApplySpecialEffects(target);
        
        // Указываем, что в этот ход больше нельзя атаковать
        CanAttack = false;
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
                    target.ApplyEffect("fire", 2);  // Горение на 2 хода
                break;
                
            case ShipType.Cruiser:
                // Шанс замедлить корабль
                if (Random.value < 0.4f)
                    target.ApplyEffect("slow", 1);  // Замедление на 1 ход
                break;
                
            case ShipType.Destroyer:
                // Шанс обнаружить подлодку
                if (target.Type == ShipType.Submarine && target.IsStealth)
                    target.IsStealth = false;
                break;
                
            case ShipType.Submarine:
                // Гарантированный критический удар
                target.TakeDamage(AttackDamage / 2);  // Дополнительный урон
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        UpdateHealthBar();
        
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
            Color originalColor = ShipMesh.material.color;
            ShipMesh.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            ShipMesh.material.color = originalColor;
        }
    }

    public void Destroy()
    {
        // Анимация уничтожения
        StartCoroutine(DestroyAnimation());
        
        // Вызываем событие уничтожения
        if (OnDestroyed != null)
            OnDestroyed(this);
            
        // Удаляем корабль из списка
        if (PlayerOwned)
            GameManager.Instance.RemovePlayerShip(this);
        else
            GameManager.Instance.RemoveEnemyShip(this);
            
        // Удаляем объект после анимации
        Destroy(gameObject, 2.0f);
    }

    private System.Collections.IEnumerator DestroyAnimation()
    {
        // Анимация уничтожения: уменьшение размера и прозрачность
        float duration = 1.0f;
        float elapsed = 0.0f;
        Vector3 originalScale = transform.localScale;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / duration);
            
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
        
        // Здесь можно добавить визуальные эффекты для каждого состояния
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
                        break;
                        
                    case "stealth":
                        if (Type == ShipType.Submarine)
                            IsStealth = false;
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
    }

    void OnMouseDown()
    {
        // Обработка клика по кораблю
        if (PlayerOwned && GameManager.Instance.IsPlayerTurn)
        {
            Select();
        }
    }
}