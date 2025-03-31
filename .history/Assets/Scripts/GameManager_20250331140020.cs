using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int WindDirections = 8; // 8 для стандартного компаса
    
    [Header("Prefabs")]
    public GameObject battleshipPrefab;
    public GameObject cruiserPrefab;
    public GameObject destroyerPrefab;
    public GameObject submarinePrefab;
    
    [Header("UI References")]
    public TextMeshProUGUI turnText;
    public Transform windArrow;
    public Button endTurnButton;
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryText;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    public List<Ship> PlayerShips { get; private set; } = new List<Ship>();
    public List<Ship> EnemyShips { get; private set; } = new List<Ship>();
    public bool IsPlayerTurn { get; private set; } = true;
    
    public HexGrid hexGrid;
    private UIManager uiManager;
    private int currentTurn = 0; // 0 - ход игрока, 1 - ход противника
    private int windDirection = 0; // направление ветра (0-7 для 8 направлений)
    private int windStrength = 1; // сила ветра (1-3)
    private bool gameOver = false;
    private Ship selectedShip = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        hexGrid = FindObjectOfType<HexGrid>();
        uiManager = FindObjectOfType<UIManager>();
    }

    private void Start()
    {
        // Инициализация игры
        PlayerShips = new List<Ship>();
        EnemyShips = new List<Ship>();
        
        // Размещение начальных флотов
        SpawnInitialFleets();
        
        // Настройка UI
        SetupUI();
        
        // Начало игры
        StartPlayerTurn();
    }

    private void SetupUI()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    private void SpawnInitialFleets()
    {
        // Создаем флот игрока
        SpawnPlayerShips();
        
        // Создаем флот противника
        SpawnEnemyShips();
    }

    private void SpawnPlayerShips()
    {
        // Создаем броненосец игрока
        Ship battleship = CreateShip(ShipType.Battleship, true);
        PlaceShip(battleship, new Vector3Int(hexGrid.gridWidth / 2, 0, hexGrid.gridHeight - 3));
        
        // Создаем крейсер игрока
        Ship cruiser = CreateShip(ShipType.Cruiser, true);
        PlaceShip(cruiser, new Vector3Int(hexGrid.gridWidth / 2 - 2, 0, hexGrid.gridHeight - 2));
        
        // Создаем эсминец игрока
        Ship destroyer = CreateShip(ShipType.Destroyer, true);
        PlaceShip(destroyer, new Vector3Int(hexGrid.gridWidth / 2 + 2, 0, hexGrid.gridHeight - 2));
        
        // Создаем подводную лодку игрока
        Ship submarine = CreateShip(ShipType.Submarine, true);
        PlaceShip(submarine, new Vector3Int(hexGrid.gridWidth / 2, 0, hexGrid.gridHeight - 1));
    }

    private void SpawnEnemyShips()
    {
        // Создаем броненосец противника
        Ship battleship = CreateShip(ShipType.Battleship, false);
        PlaceShip(battleship, new Vector3Int(hexGrid.gridWidth / 2, 0, 2));
        
        // Создаем крейсер противника
        Ship cruiser = CreateShip(ShipType.Cruiser, false);
        PlaceShip(cruiser, new Vector3Int(hexGrid.gridWidth / 2 - 2, 0, 1));
        
        // Создаем эсминец противника
        Ship destroyer = CreateShip(ShipType.Destroyer, false);
        PlaceShip(destroyer, new Vector3Int(hexGrid.gridWidth / 2 + 2, 0, 1));
        
        // Создаем подводную лодку противника
        Ship submarine = CreateShip(ShipType.Submarine, false);
        PlaceShip(submarine, new Vector3Int(hexGrid.gridWidth / 2, 0, 3));
    }

    private Ship CreateShip(ShipType type, bool playerOwned)
    {
        GameObject prefab = null;
        
        // Выбираем префаб в зависимости от типа
        switch (type)
        {
            case ShipType.Battleship:
                prefab = battleshipPrefab;
                break;
            case ShipType.Cruiser:
                prefab = cruiserPrefab;
                break;
            case ShipType.Destroyer:
                prefab = destroyerPrefab;
                break;
            case ShipType.Submarine:
                prefab = submarinePrefab;
                break;
        }
        
        if (prefab == null)
        {
            Debug.LogError("Ship prefab not assigned for type: " + type);
            return null;
        }
        
        // Создаем корабль
        GameObject shipObject = Instantiate(prefab);
        Ship ship = shipObject.GetComponent<Ship>();
        
        if (ship == null)
        {
            Debug.LogError("Ship component not found on prefab: " + prefab.name);
            Destroy(shipObject);
            return null;
        }
        
        // Инициализируем корабль
        ship.Initialize(type, playerOwned);
        
        // Подключаем обработчики событий
        ship.OnSelected += OnShipSelected;
        ship.OnMoved += OnShipMoved;
        ship.OnAttacked += OnShipAttacked;
        ship.OnDestroyed += OnShipDestroyed;
        
        // Добавляем в соответствующий список
        if (playerOwned)
            PlayerShips.Add(ship);
        else
            EnemyShips.Add(ship);
        
        return ship;
    }

    private void PlaceShip(Ship ship, Vector3Int hexPosition)
    {
        // Устанавливаем позицию в сетке
        ship.HexPosition = hexPosition;
        
        // Получаем мировую позицию
        Vector3 worldPosition = hexGrid.CalculateHexPosition(hexPosition.x, hexPosition.y, hexPosition.z);
        worldPosition.y = 0.3f; // Поднимаем над сеткой
        ship.transform.position = worldPosition;
        
        // Устанавливаем корабль на ячейку
        if (hexGrid.hexGrid.ContainsKey(hexPosition))
            hexGrid.hexGrid[hexPosition].OccupyingShip = ship;
    }

    public void StartPlayerTurn()
    {
        if (gameOver)
            return;
            
        currentTurn = 0;
        IsPlayerTurn = true;
        
        // Обновляем ветер
        UpdateWind();
        
        // Сбрасываем параметры кораблей игрока
        ResetShips(PlayerShips);
        
        // Обновляем UI
        UpdateUI();
        
        // Проверяем условия победы
        CheckVictoryConditions();
    }

    public void StartEnemyTurn()
    {
        if (gameOver)
            return;
        
        // Снимаем выделение с текущего корабля, если есть
        if (selectedShip != null)
        {
            selectedShip.Deselect();
            selectedShip = null;
            
            if (uiManager != null)
                uiManager.ClearShipInfo();
                
            if (hexGrid != null)
                hexGrid.ClearHighlights();
        }
            
        currentTurn = 1;
        IsPlayerTurn = false;
        
        // Сбрасываем параметры кораблей противника
        ResetShips(EnemyShips);
        
        // Обновляем UI
        UpdateUI();
        
        // Проверяем условия победы
        CheckVictoryConditions();
        
        if (!gameOver)
        {
            // Запускаем ИИ противника
            PerformEnemyActions();
        }
    }

    private void UpdateWind()
    {
        // Меняем направление ветра (случайное изменение на ±1 или без изменений)
        windDirection = (windDirection + Random.Range(-1, 2)) % WindDirections;
        if (windDirection < 0)
            windDirection = WindDirections - 1;
            
        // Обновляем силу ветра (1-3)
        windStrength = Random.Range(1, 4);
        
        // Обновляем визуальное отображение
        UpdateWindUI();
    }

    private void UpdateWindUI()
    {
        if (windArrow != null)
        {
            // Поворачиваем стрелку ветра
            float angle = 360f * windDirection / WindDirections;
            windArrow.rotation = Quaternion.Euler(0, 0, angle);
            
            // Меняем цвет в зависимости от силы
            Image arrowImage = windArrow.GetComponent<Image>();
            if (arrowImage != null)
            {
                switch (windStrength)
                {
                    case 1:
                        arrowImage.color = new Color(0.5f, 0.5f, 1f);
                        break;
                    case 2:
                        arrowImage.color = new Color(0.3f, 0.3f, 1f);
                        break;
                    case 3:
                        arrowImage.color = new Color(0.1f, 0.1f, 1f);
                        break;
                }
            }
        }
    }

    private void ResetShips(List<Ship> ships)
    {
        foreach (Ship ship in ships)
        {
            ship.ResetForNewTurn();
            
            // Бонус от ветра (если он попутный)
            if (IsWindFavorable(ship))
            {
                ship.MovementPoints += 1;
            }
        }
    }

    private bool IsWindFavorable(Ship ship)
    {
        // Проверяем, попутный ли ветер для корабля
        // Для подводных лодок ветер не влияет
        if (ship.Type == ShipType.Submarine)
            return false;
            
        // Упрощенная версия для демонстрации
        return Random.value < 0.3f;  // 30% шанс попутного ветра
    }

    private void UpdateUI()
    {
        if (turnText != null)
        {
            turnText.text = IsPlayerTurn ? "Ваш ход" : "Ход противника";
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.interactable = IsPlayerTurn;
        }
        
        if (uiManager != null)
        {
            uiManager.UpdateTurnInfo();
        }
    }

    private void PerformEnemyActions()
    {
        // Создаем корутину для последовательного выполнения действий
        StartCoroutine(PerformEnemyActionsSequence());
    }

    private System.Collections.IEnumerator PerformEnemyActionsSequence()
    {
        // Небольшая задержка перед ходом противника
        yield return new WaitForSeconds(1f);
        
        // Перебираем все корабли противника
        foreach (Ship ship in new List<Ship>(EnemyShips))
        {
            // Пропускаем уничтоженные корабли
            if (ship == null || ship.Health <= 0)
                continue;
                
            // Ищем ближайший корабль игрока для атаки
            Ship targetShip = FindNearestTarget(ship, PlayerShips);
            
            if (targetShip != null)
            {
                // Двигаемся к цели
                MoveTowardsTarget(ship, targetShip);
                
                // Небольшая задержка между ходами кораблей
                yield return new WaitForSeconds(0.5f);
                
                // Атакуем, если в диапазоне
                if (hexGrid.IsInAttackRange(ship, targetShip))
                {
                    ship.Attack(targetShip);
                    
                    // Задержка после атаки
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            // Задержка перед следующим кораблем
            yield return new WaitForSeconds(0.5f);
        }
        
        // Завершаем ход противника
        yield return new WaitForSeconds(1f);
        StartPlayerTurn();
    }

    private Ship FindNearestTarget(Ship ship, List<Ship> targets)
    {
        if (targets.Count == 0)
            return null;
            
        // Фильтруем действительные цели
        List<Ship> validTargets = new List<Ship>();
        foreach (Ship target in targets)
        {
            if (target == null || target.Health <= 0)
                continue;
                
            // Проверяем, видна ли подводная лодка
            if (target.Type == ShipType.Submarine && target.IsStealth && ship.Type != ShipType.Destroyer)
                continue;  // Только эсминцы могут обнаружить скрытую подлодку
                
            validTargets.Add(target);
        }
        
        if (validTargets.Count == 0)
            return null;
            
        // Находим ближайшую цель
        Ship nearest = validTargets[0];
        int minDistance = hexGrid.CalculateHexDistance(ship.HexPosition, nearest.HexPosition);
        
        foreach (Ship target in validTargets)
        {
            int distance = hexGrid.CalculateHexDistance(ship.HexPosition, target.HexPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = target;
            }
        }
        
        return nearest;
    }

    private void MoveTowardsTarget(Ship ship, Ship target)
    {
        // Двигаемся к цели, используя доступные очки движения
        while (ship.MovementPoints > 0)
        {
            // Получаем следующий шаг к цели
            Vector3Int nextPosition = GetNextStepTowards(ship.HexPosition, target.HexPosition);
            
            // Проверяем, можно ли переместиться
            if (hexGrid.CanMoveTo(nextPosition, ship) && !hexGrid.IsOccupied(nextPosition))
            {
                // Перемещаем корабль
                hexGrid.MoveShip(ship, nextPosition);
                
                // Если достаточно близко к цели для атаки, прекращаем движение
                if (hexGrid.IsInAttackRange(ship, target))
                    break;
            }
            else
            {
                break;
            }
        }
    }

    private Vector3Int GetNextStepTowards(Vector3Int start, Vector3Int end)
    {
        // Получаем следующую ячейку на пути к цели
        Vector3 startCube = hexGrid.AxialToCube(new Vector2(start.x, start.z));
        Vector3 endCube = hexGrid.AxialToCube(new Vector2(end.x, end.z));
        
        // Направление к цели
        Vector3 direction = new Vector3(
            Mathf.Sign(endCube.x - startCube.x),
            Mathf.Sign(endCube.y - startCube.y),
            Mathf.Sign(endCube.z - startCube.z)
        );
        
        // Если направление нулевое, мы уже на месте
        if (direction == Vector3.zero)
            return start;
            
        // Выбираем направление с наибольшей разницей
        Vector3 nextStep;
        if (direction.x != 0 && direction.y != 0 && direction.z != 0)
        {
            float dx = Mathf.Abs(endCube.x - startCube.x);
            float dy = Mathf.Abs(endCube.y - startCube.y);
            float dz = Mathf.Abs(endCube.z - startCube.z);
            
            if (dx >= dy && dx >= dz)
                nextStep = new Vector3(direction.x, 0, 0);
            else if (dy >= dx && dy >= dz)
                nextStep = new Vector3(0, direction.y, 0);
            else
                nextStep = new Vector3(0, 0, direction.z);
        }
        else
        {
            nextStep = direction;
        }
        
        // Преобразуем обратно в координаты сетки
        Vector3 nextCube = startCube + nextStep;
        Vector2 nextAxial = hexGrid.CubeToAxial(nextCube);
        
        return new Vector3Int(Mathf.RoundToInt(nextAxial.x), 0, Mathf.RoundToInt(nextAxial.y));
    }

    public void CheckVictoryConditions()
    {
           if (gameOver)
        return;
    
    // Пропускаем проверку на первом ходе игры
    if (PlayerShips.Count == 0 || EnemyShips.Count == 0)
        return;
        
    // Проверяем, остались ли корабли у игрока
    bool playerHasShips = false;
    foreach (Ship ship in PlayerShips)
    {
        if (ship != null && ship.Health > 0)
        {
            playerHasShips = true;
            break;
        }
    }
        
        if (!playerHasShips)
        {
            gameOver = true;
            ShowVictoryScreen(false);  // Игрок проиграл
            return;
        }
        
        // Проверяем, остались ли корабли у противника
        bool enemyHasShips = false;
        foreach (Ship ship in EnemyShips)
        {
            if (ship != null && ship.Health > 0)
            {
                enemyHasShips = true;
                break;
            }
        }
        
        if (!enemyHasShips)
        {
            gameOver = true;
            ShowVictoryScreen(true);  // Игрок победил
            return;
        }
        
        // Проверяем, захвачены ли все форты и базы
        bool allFortsCaptured = true;
        bool playerHasBase = false;
        
        foreach (var hexEntry in hexGrid.hexGrid)
        {
            Hex hex = hexEntry.Value;
            
            if (hex.Type == HexType.Fort && hex.OwnerID == -1)
            {
                allFortsCaptured = false;
            }
            
            if (hex.Type == HexType.PlayerBase)
            {
                playerHasBase = true;
            }
        }
        
        // Если игрок потерял все базы или противник захватил все форты
        if (!playerHasBase)
        {
            gameOver = true;
            ShowVictoryScreen(false);  // Игрок проиграл
        }
        else if (allFortsCaptured)
        {
            gameOver = true;
            ShowVictoryScreen(true);  // Игрок победил
        }
    }

    public Ship GetSelectedShip()
{
    return selectedShip;
}

    private void ShowVictoryScreen(bool playerWon)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            
            if (victoryText != null)
            {
                if (playerWon)
                {
                    victoryText.text = "Победа! Вы захватили контроль над морем!";
                }
                else
                {
                    victoryText.text = "Поражение! Ваш флот разбит!";
                }
            }
        }
    }

    // Обработчик выбора корабля
    public void OnShipSelected(Ship ship)
    {
        Debug.Log("Выбран корабль: " + ship.name);
        
        // Если был выбран другой корабль, отменяем его выбор
        if (selectedShip != null && selectedShip != ship)
        {
            selectedShip.Deselect();
        }
        
        selectedShip = ship;
        
        // Обновляем UI для выбранного корабля
        if (uiManager != null)
        {
            uiManager.DisplayShipInfo(ship);
        }
        
        // Находим HexGrid для отображения доступных ходов
        if (hexGrid != null)
        {
            hexGrid.SelectShip(ship);
        }
    }

    // Метод для обработки отмены выбора корабля
    public void OnShipDeselected(Ship ship)
    {
        Debug.Log("Отменен выбор корабля: " + ship.name);
        
        if (selectedShip == ship)
        {
            selectedShip = null;
            
            // Очищаем UI информацию о корабле
            if (uiManager != null)
            {
                uiManager.ClearShipInfo();
            }
            
            // Очищаем подсветку на сетке
            if (hexGrid != null)
            {
                hexGrid.ClearHighlights();
            }
        }
    }

    private void OnShipMoved(Ship ship, Vector3Int oldPosition, Vector3Int newPosition)
    {
        Debug.Log(ship.name + " переместился из " + oldPosition + " в " + newPosition);
        
        // Обновить ссылки на ячейках
        if (hexGrid.hexGrid.ContainsKey(oldPosition))
            hexGrid.hexGrid[oldPosition].OccupyingShip = null;
            
        if (hexGrid.hexGrid.ContainsKey(newPosition))
            hexGrid.hexGrid[newPosition].OccupyingShip = ship;
            
        // Проверить особые взаимодействия с гексами (форты, базы)
        CheckSpecialTileInteractions(ship, newPosition);
    }

    private void CheckSpecialTileInteractions(Ship ship, Vector3Int pos)
    {
        if (!hexGrid.hexGrid.ContainsKey(pos))
            return;
            
        Hex hex = hexGrid.hexGrid[pos];
        
        // Проверяем, может ли корабль захватить форт/базу
        if ((hex.Type == HexType.Fort && hex.OwnerID != 0) || 
            (hex.Type == HexType.EnemyBase))
        {
            // Только броненосцы и крейсеры могут захватывать
            if (ship.Type == ShipType.Battleship || ship.Type == ShipType.Cruiser)
            {
                // Уменьшаем защиту
                hex.Defense -= ship.AttackDamage;
                Debug.Log("Форт под атакой! Оставшаяся защита: " + hex.Defense);
                
                // Если защита закончилась, захватываем объект
                if (hex.Defense <= 0)
                {
                    hex.OwnerID = 0;  // Игрок теперь владелец
                    
                    if (hex.Type == HexType.EnemyBase)
                    {
                        hex.SetType(HexType.PlayerBase);
                        Debug.Log("База противника захвачена!");
                    }
                    else
                    {
                        Debug.Log("Форт захвачен!");
                    }
                    
                    // Проверяем условия победы
                    CheckVictoryConditions();
                }
            }
        }
    }

    private void OnShipAttacked(Ship attacker, Ship target)
    {
        Debug.Log(attacker.name + " атаковал " + target.name);
        
        // Можно добавить обработку атаки, например, обновление UI или звуковые эффекты
        if (debugMode)
            Debug.Log($"{attacker.Type} атакует {target.Type}, нанося урон");
    }

    private void OnShipDestroyed(Ship ship)
    {
        Debug.Log(ship.name + " уничтожен!");
        
        // Очищаем ссылку на выбранный корабль, если он был уничтожен
        if (selectedShip == ship)
        {
            selectedShip = null;
            
            if (uiManager != null)
                uiManager.ClearShipInfo();
        }
        
        // Удаляем корабль из соответствующего списка
        RemoveShip(ship);
        
        // Проверяем условия победы
        CheckVictoryConditions();
    }
    
    // Удаление корабля из списка
    public void RemoveShip(Ship ship)
    {
        if (ship.PlayerOwned)
            PlayerShips.Remove(ship);
        else
            EnemyShips.Remove(ship);
    }

    public void RemovePlayerShip(Ship ship)
    {
        PlayerShips.Remove(ship);
    }

    public void RemoveEnemyShip(Ship ship)
    {
        EnemyShips.Remove(ship);
    }

    private void OnEndTurnButtonClicked()
    {
        if (IsPlayerTurn && !gameOver)
        {
            StartEnemyTurn();
        }
    }

    // Метод для перезапуска игры (можно вызывать из кнопки UI)
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}