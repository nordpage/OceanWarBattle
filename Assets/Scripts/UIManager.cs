using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Turn Panel")]
    public TextMeshProUGUI turnLabel;
    public Transform windCompass;
    public RectTransform windArrow;
    public Button endTurnButton;
    
    [Header("Ship Info Panel")]
    public GameObject shipInfoPanel;
    public TextMeshProUGUI shipTypeText;
    public Slider healthBar;
    public TextMeshProUGUI movementText;
    public TextMeshProUGUI attackText;
    public Transform effectsContainer;
    public GameObject effectPrefab;
    
    private GameManager gameManager;
    private Ship selectedShip;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        // Скрываем панель информации о корабле
        if (shipInfoPanel != null)
            shipInfoPanel.SetActive(false);
    }

    private void Start()
    {
        // Подключаем кнопку завершения хода
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            
        UpdateTurnInfo();
    }

    private void OnEndTurnClicked()
    {
        if (gameManager != null && gameManager.IsPlayerTurn)
        {
            gameManager.StartEnemyTurn();
            UpdateTurnInfo();
        }
    }

    public void UpdateTurnInfo()
    {
        if (turnLabel != null && gameManager != null)
        {
            turnLabel.text = gameManager.IsPlayerTurn ? "Ваш ход" : "Ход противника";
        }
        
        if (endTurnButton != null && gameManager != null)
        {
            endTurnButton.interactable = gameManager.IsPlayerTurn;
        }
    }

    public void DisplayShipInfo(Ship ship)
    {
        if (ship == null || shipInfoPanel == null)
            return;
            
        selectedShip = ship;
        shipInfoPanel.SetActive(true);
        
        // Заполняем информацию о корабле
        if (shipTypeText != null)
            shipTypeText.text = GetShipTypeName(ship.Type);
            
        if (healthBar != null)
        {
            healthBar.value = (float)ship.Health / ship.MaxHealth;
            
            // Меняем цвет полоски здоровья
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if ((float)ship.Health / ship.MaxHealth > 0.7f)
                    fillImage.color = Color.green;
                else if ((float)ship.Health / ship.MaxHealth > 0.3f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
        
        if (movementText != null)
            movementText.text = $"Движение: {ship.MovementPoints}/{ship.MaxMovement}";
            
        if (attackText != null)
            attackText.text = $"Атака: {ship.AttackDamage} | Дальность: {ship.AttackRange}";
            
        // Обновляем список эффектов
        UpdateEffectsList(ship);
    }

    private void UpdateEffectsList(Ship ship)
    {
        if (effectsContainer == null)
            return;
            
        // Удаляем все существующие эффекты
        foreach (Transform child in effectsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Добавляем текущие эффекты
        foreach (var effect in ship.GetActiveEffects())
        {
            if (effectPrefab != null)
            {
                GameObject effectObj = Instantiate(effectPrefab, effectsContainer);
                TextMeshProUGUI effectText = effectObj.GetComponent<TextMeshProUGUI>();
                if (effectText != null)
                {
                    effectText.text = $"{effect.Key} ({effect.Value} ходов)";
                }
            }
        }
    }

    public void ClearShipInfo()
    {
        selectedShip = null;
        if (shipInfoPanel != null)
            shipInfoPanel.SetActive(false);
    }

    private string GetShipTypeName(ShipType type)
    {
        switch (type)
        {
            case ShipType.Battleship: return "Броненосец";
            case ShipType.Cruiser: return "Крейсер";
            case ShipType.Destroyer: return "Эсминец";
            case ShipType.Submarine: return "Подводная лодка";
            default: return "Неизвестно";
        }
    }
}