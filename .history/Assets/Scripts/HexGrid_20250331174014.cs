using UnityEngine;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    public GameObject hexPrefab;
    public int gridWidth = 12;
    public int gridHeight = 8;
    public float hexWidth = 2.12f;
    public float hexHeight = 1.93f;
    public float hexSpacing = 0.05f;
    
    [Header("Materials")]
    public Material waterMaterial;
    public Material shallowMaterial;
    public Material islandMaterial;
    public Material reefMaterial;
    public Material fortMaterial;
    public Material playerBaseMaterial;
    public Material enemyBaseMaterial;
    
    [Header("Highlighting")]
    public Material movementHighlightMaterial;
    public Material attackHighlightMaterial;
    
    public Dictionary<Vector3Int, Hex> hexGrid = new Dictionary<Vector3Int, Hex>();
    private List<GameObject> highlightedHexes = new List<GameObject>();
    
    public Ship SelectedShip { get; private set; }
    
    void Start()
    {
        GenerateGrid();
    }
    
    void GenerateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Создание шестиугольника
                Vector3 position = CalculateHexPosition(x, 0, z);
                GameObject hexObj = Instantiate(hexPrefab, position, Quaternion.identity, transform);
                hexObj.name = $"Hex_{x}_{z}";
                
                // Создание компонента Hex
                Hex hex = hexObj.AddComponent<Hex>();
                hex.Initialize(new Vector3Int(x, 0, z), HexType.Water);
                
                // Добавление в словарь
                hexGrid[new Vector3Int(x, 0, z)] = hex;
            }
        }
        
        // Генерация ландшафта
        GenerateTerrain();
    }
    
    public Vector3 CalculateHexPosition(int x, int y, int z)
    {
        // Позиция для pointy-top шестиугольников
        float xPos = x * (hexWidth * 0.75f + hexSpacing);
        float zPos = z * (hexHeight + hexSpacing);
        
        // Смещение для нечетных столбцов
        if (x % 2 == 1)
            zPos += hexHeight * 0.5f;
        
        return new Vector3(xPos, y, zPos);
    }

    // Добавить в класс HexGrid
// Метод для получения гексов в определенном радиусе от центра
public List<Vector3Int> GetHexesInRange(Vector3Int center, int range)
{
    List<Vector3Int> results = new List<Vector3Int>();
    
    for (int dx = -range; dx <= range; dx++)
    {
        for (int dz = Mathf.Max(-range, -dx-range); dz <= Mathf.Min(range, -dx+range); dz++)
        {
            int dy = -dx-dz;
            Vector3 cube = new Vector3(dx, dy, dz);
            Vector2 axial = CubeToAxial(cube + AxialToCube(new Vector2(center.x, center.z)));
            Vector3Int cell = new Vector3Int(Mathf.RoundToInt(axial.x), 0, Mathf.RoundToInt(axial.y));
            
            if (hexGrid.ContainsKey(cell))
                results.Add(cell);
        }
    }
    
    return results;
}

// Метод для построения линии между двумя гексами (для стрельбы)
public List<Vector3Int> DrawLine(Vector3Int start, Vector3Int end)
{
    List<Vector3Int> line = new List<Vector3Int>();
    
    // Преобразуем в кубические координаты
    Vector3 startCube = AxialToCube(new Vector2(start.x, start.z));
    Vector3 endCube = AxialToCube(new Vector2(end.x, end.z));
    
    // Количество шагов = расстояние между гексами
    int distance = CalculateHexDistance(start, end);
    
    // Добавляем стартовую позицию
    line.Add(start);
    
    // Если расстояние = 0, возвращаем только стартовую точку
    if (distance == 0)
        return line;
    
    // Строим линию по шагам
    for (int i = 1; i <= distance; i++)
    {
        // Интерполируем между началом и концом
        float t = (float)i / distance;
        Vector3 interpolated = Vector3.Lerp(startCube, endCube, t);
        
        // Округляем до ближайшего гекса
        Vector3 roundedCube = CubeRound(interpolated);
        Vector2 axial = CubeToAxial(roundedCube);
        Vector3Int hexPosition = new Vector3Int(Mathf.RoundToInt(axial.x), 0, Mathf.RoundToInt(axial.y));
        
        line.Add(hexPosition);
    }
    
    return line;
}
    
    public Vector3Int WorldToHexPosition(Vector3 worldPosition)
    {
        // Обратное преобразование из мировых координат в координаты сетки
        float x = worldPosition.x / (hexWidth * 0.75f + hexSpacing);
        float z = worldPosition.z;
        
        // Коррекция для смещения в нечетных столбцах
        if (Mathf.Round(x) % 2 == 1)
            z -= hexHeight * 0.5f;
        
        z /= (hexHeight + hexSpacing);
        
        // Используем кубические координаты для более точного определения
        Vector3 cubeCoord = AxialToCube(new Vector2(x, z));
        cubeCoord = CubeRound(cubeCoord);
        Vector2 axialCoord = CubeToAxial(cubeCoord);
        
        return new Vector3Int(Mathf.RoundToInt(axialCoord.x), 0, Mathf.RoundToInt(axialCoord.y));
    }
    
    public Vector3 AxialToCube(Vector2 hex)
    {
        float q = hex.x;
        float r = hex.y;
        float s = -q - r;
        return new Vector3(q, s, r);
    }
    
    private Vector3 CubeRound(Vector3 cube)
    {
        float rx = Mathf.Round(cube.x);
        float ry = Mathf.Round(cube.y);
        float rz = Mathf.Round(cube.z);
        
        float x_diff = Mathf.Abs(rx - cube.x);
        float y_diff = Mathf.Abs(ry - cube.y);
        float z_diff = Mathf.Abs(rz - cube.z);
        
        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;
        
        return new Vector3(rx, ry, rz);
    }
    
    public Vector2 CubeToAxial(Vector3 cube)
    {
        float q = cube.x;
        float r = cube.z;
        return new Vector2(q, r);
    }
    
    void GenerateTerrain()
    {
        // Заполнить всё водой
        FillLayer(HexType.Water);
        
        // Добавить острова
        AddIslands(3);
        
        // Добавить рифы
        AddReefs(4);
        
        // Добавить мелководья
        AddShallowWaters(2);
        
        // Добавить базы
        PlacePlayerBase();
        PlaceEnemyBase();
        
        // Добавить нейтральные форты
        AddForts(2);
    }
    
    void FillLayer(HexType type)
    {
        foreach (var hex in hexGrid.Values)
        {
            hex.SetType(type);
        }
    }
    
    void AddIslands(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int x = Random.Range(2, gridWidth - 3);
            int z = Random.Range(2, gridHeight - 3);
            
            // Создаем остров из нескольких ячеек
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    // Не все ячейки будут островом для более натурального вида
                    if (Random.value < 0.7f)
                    {
                        int nx = x + dx;
                        int nz = z + dz;
                        if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight)
                        {
                            var position = new Vector3Int(nx, 0, nz);
                            if (hexGrid.ContainsKey(position))
                            {
                                hexGrid[position].SetType(HexType.Island);
                            }
                        }
                    }
                }
            }
        }
    }
    
    void AddReefs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int x = Random.Range(0, gridWidth);
            int z = Random.Range(0, gridHeight);
            var position = new Vector3Int(x, 0, z);
            
            // Если ячейка уже занята, пропускаем
            if (!hexGrid.ContainsKey(position) || hexGrid[position].Type != HexType.Water)
                continue;
            
            // Создаем небольшую группу рифов
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (Random.value < 0.5f)
                    {
                        int nx = x + dx;
                        int nz = z + dz;
                        var newPos = new Vector3Int(nx, 0, nz);
                        if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight && 
                            hexGrid.ContainsKey(newPos) && hexGrid[newPos].Type == HexType.Water)
                        {
                            hexGrid[newPos].SetType(HexType.Reef);
                        }
                    }
                }
            }
        }
    }
    
    void AddShallowWaters(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int x = Random.Range(0, gridWidth);
            int z = Random.Range(0, gridHeight);
            var position = new Vector3Int(x, 0, z);
            
            // Если ячейка уже занята, пропускаем
            if (!hexGrid.ContainsKey(position) || hexGrid[position].Type != HexType.Water)
                continue;
            
            // Создаем область мелководья
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    if (Random.value < 0.6f)
                    {
                        int nx = x + dx;
                        int nz = z + dz;
                        var newPos = new Vector3Int(nx, 0, nz);
                        if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight && 
                            hexGrid.ContainsKey(newPos) && hexGrid[newPos].Type == HexType.Water)
                        {
                            hexGrid[newPos].SetType(HexType.Shallow);
                        }
                    }
                }
            }
        }
    }
    
    void PlacePlayerBase()
    {
        // Размещаем базу игрока в нижней части карты
        int x = Random.Range(gridWidth / 4, gridWidth * 3 / 4);
        int z = gridHeight - 2;
        var position = new Vector3Int(x, 0, z);
        
        if (hexGrid.ContainsKey(position))
        {
            hexGrid[position].SetType(HexType.PlayerBase);
            hexGrid[position].OwnerID = 0;
            hexGrid[position].Defense = 100;
            
            // Добавляем небольшой остров вокруг базы
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;  // Пропускаем центр, так как там уже база
                    
                    int nx = x + dx;
                    int nz = z + dz;
                    var newPos = new Vector3Int(nx, 0, nz);
                    if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight && 
                        hexGrid.ContainsKey(newPos))
                    {
                        hexGrid[newPos].SetType(HexType.Island);
                    }
                }
            }
        }
    }
    
    void PlaceEnemyBase()
    {
        // Размещаем базу противника в верхней части карты
        int x = Random.Range(gridWidth / 4, gridWidth * 3 / 4);
        int z = 1;
        var position = new Vector3Int(x, 0, z);
        
        if (hexGrid.ContainsKey(position))
        {
            hexGrid[position].SetType(HexType.EnemyBase);
            hexGrid[position].OwnerID = 1;
            hexGrid[position].Defense = 100;
            
            // Добавляем небольшой остров вокруг базы
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;
                    
                    int nx = x + dx;
                    int nz = z + dz;
                    var newPos = new Vector3Int(nx, 0, nz);
                    if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight && 
                        hexGrid.ContainsKey(newPos))
                    {
                        hexGrid[newPos].SetType(HexType.Island);
                    }
                }
            }
        }
    }
    
    void AddForts(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Размещаем форты примерно посередине карты
            int x = Random.Range(2, gridWidth - 3);
            int z = Random.Range(gridHeight / 4, gridHeight * 3 / 4);
            var position = new Vector3Int(x, 0, z);
            
            // Убедимся, что место свободно
            if (!hexGrid.ContainsKey(position) || hexGrid[position].Type != HexType.Water)
                continue;
            
            hexGrid[position].SetType(HexType.Fort);
            hexGrid[position].OwnerID = -1;  // Нейтральный форт
            hexGrid[position].Defense = 50;  // Меньше защиты, чем у базы
        }
    }
    
    public Material GetMaterialForHexType(HexType type)
    {
        switch (type)
        {
            case HexType.Water: return waterMaterial;
            case HexType.Shallow: return shallowMaterial;
            case HexType.Island: return islandMaterial;
            case HexType.Reef: return reefMaterial;
            case HexType.Fort: return fortMaterial;
            case HexType.PlayerBase: return playerBaseMaterial;
            case HexType.EnemyBase: return enemyBaseMaterial;
            default: return waterMaterial;
        }
    }
    
    public void SelectShip(Ship ship)
    {
        SelectedShip = ship;
        
        ClearHighlights();
        if (ship != null)
        {
            HighlightMovementRange(ship);
            HighlightAttackRange(ship);
        }
    }
    
    public void ClearHighlights()
    {
        foreach (var highlightObject in highlightedHexes)
        {
            Destroy(highlightObject);
        }
        highlightedHexes.Clear();
    }
    
    public void HighlightMovementRange(Ship ship)
    {
        if (ship.MovementPoints <= 0)
            return;
        
        var movementCells = GetMovementRange(ship);
        
        foreach (var cellPos in movementCells)
        {
            if (hexGrid.ContainsKey(cellPos))
            {
                // Создаем объект подсветки
                GameObject highlight = CreateHighlight(hexGrid[cellPos].transform.position, movementHighlightMaterial);
                highlightedHexes.Add(highlight);
            }
        }
    }
    
    public void HighlightAttackRange(Ship ship)
    {
        var attackCells = GetAttackRange(ship);
        
        foreach (var cellPos in attackCells)
        {
            if (hexGrid.ContainsKey(cellPos))
            {
                // Создаем объект подсветки
                GameObject highlight = CreateHighlight(hexGrid[cellPos].transform.position, attackHighlightMaterial);
                highlightedHexes.Add(highlight);
            }
        }
    }
    
    private GameObject CreateHighlight(Vector3 position, Material material)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlight.transform.position = position + Vector3.up * 0.05f;
        highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
        highlight.transform.localScale = new Vector3(hexWidth * 0.8f, hexHeight * 0.8f, 1);
        
        MeshRenderer renderer = highlight.GetComponent<MeshRenderer>();
        renderer.material = material;
        
        // Делаем полупрозрачным и отключаем коллизию
        renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 0.5f);
        Destroy(highlight.GetComponent<Collider>());
        
        return highlight;
    }
    
    public List<Vector3Int> GetMovementRange(Ship ship)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        Dictionary<Vector3Int, int> visited = new Dictionary<Vector3Int, int>();
        Queue<MovementNode> queue = new Queue<MovementNode>();
        
        // Начинаем с текущей позиции
        queue.Enqueue(new MovementNode { Position = ship.HexPosition, RemainingMP = ship.MovementPoints });
        
        while (queue.Count > 0)
        {
            MovementNode current = queue.Dequeue();
            
            // Уникальный ключ для позиции
            if (visited.ContainsKey(current.Position) && visited[current.Position] >= current.RemainingMP)
                continue;
            
            // Добавляем в список посещенных
            visited[current.Position] = current.RemainingMP;
            
            // Если это не начальная позиция, добавляем в результат
            if (current.Position != ship.HexPosition)
                result.Add(current.Position);
            
            // Если не осталось очков движения, не проверяем соседей
            if (current.RemainingMP <= 0)
                continue;
            
            // Проверяем соседние ячейки
            foreach (Vector2Int direction in GetHexDirections())
            {
                Vector3Int nextPos = new Vector3Int(
                    current.Position.x + direction.x,
                    current.Position.y,
                    current.Position.z + direction.y
                );
                
                // Стоимость движения
                int moveCost = GetMovementCost(nextPos, ship);
                
                // Если можно переместиться и есть достаточно очков движения
                if (moveCost > 0 && current.RemainingMP >= moveCost && !IsOccupied(nextPos))
                {
                    queue.Enqueue(new MovementNode { Position = nextPos, RemainingMP = current.RemainingMP - moveCost });
                }
            }
        }
        
        return result;
    }
    
    private List<Vector2Int> GetHexDirections()
    {
        // Направления для шестиугольной сетки в аксиальных координатах
        return new List<Vector2Int>
        {
            new Vector2Int(1, 0),   // Восток
            new Vector2Int(1, -1),  // Северо-восток
            new Vector2Int(0, -1),  // Северо-запад
            new Vector2Int(-1, 0),  // Запад
            new Vector2Int(-1, 1),  // Юго-запад
            new Vector2Int(0, 1)    // Юго-восток
        };
    }
    
    public int GetMovementCost(Vector3Int position, Ship ship)
    {
        // Проверяем, что позиция в пределах сетки
        if (!hexGrid.ContainsKey(position))
            return 0;
        
        HexType tileType = hexGrid[position].Type;
        
        // Правила стоимости перемещения
        switch (tileType)
        {
            case HexType.Water:
                return 1;  // Обычная стоимость
            case HexType.Shallow:
                return ship.Type == ShipType.Battleship ? 2 : 1;  // Броненосцам трудно в мелководье
            case HexType.Reef:
                return ship.Type == ShipType.Submarine ? 1 : 2;  // Подлодки проходят сквозь рифы
            case HexType.Island:
            case HexType.Fort:
            case HexType.PlayerBase:
            case HexType.EnemyBase:
                return 0;  // Недоступно
            default:
                return 1;
        }
    }
    
    public bool IsOccupied(Vector3Int position)
    {
        if (!hexGrid.ContainsKey(position))
            return true;
        
        return hexGrid[position].OccupyingShip != null;
    }
    
    public List<Vector3Int> GetAttackRange(Ship ship)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        
        int range = ship.AttackRange;
        Vector3Int center = ship.HexPosition;
        
        // Для шестиугольной сетки проходим по кубическим координатам
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dz = Mathf.Max(-range, -dx - range); dz <= Mathf.Min(range, -dx + range); dz++)
            {
                int dy = -dx - dz;
                Vector3 cube = new Vector3(dx, dy, dz);
                Vector2 axial = CubeToAxial(cube + AxialToCube(new Vector2(center.x, center.z)));
                Vector3Int cell = new Vector3Int(Mathf.RoundToInt(axial.x), 0, Mathf.RoundToInt(axial.y));
                
                // Проверяем, что ячейка в пределах карты
                if (hexGrid.ContainsKey(cell) && cell != center)
                {
                    result.Add(cell);
                }
            }
        }
        
        return result;
    }
    
    public bool CanMoveTo(Vector3Int position, Ship ship)
    {
        // Проверяем, можно ли переместиться на эту позицию
        if (!hexGrid.ContainsKey(position))
            return false;
        
        HexType tileType = hexGrid[position].Type;
        
        // Правила перемещения для разных типов кораблей и ячеек
        switch (tileType)
        {
            case HexType.Island:
            case HexType.Fort:
            case HexType.PlayerBase:
            case HexType.EnemyBase:
                return false;  // Нельзя перемещаться на сушу
            case HexType.Reef:
                if (ship.Type == ShipType.Submarine)
                    return true;  // Подлодки могут проходить сквозь рифы
                return ship.MovementPoints >= 2;  // Другим кораблям нужно минимум 2 очка движения
            case HexType.Shallow:
                if (ship.Type == ShipType.Battleship)
                    return ship.MovementPoints >= 2;  // Броненосцам трудно в мелководье
                return true;
            default:
                return true;
        }
    }
    
    public bool IsInAttackRange(Ship attacker, Ship target)
    {
        int distance = CalculateHexDistance(attacker.HexPosition, target.HexPosition);
        return distance <= attacker.AttackRange;
    }
    
    public int CalculateHexDistance(Vector3Int a, Vector3Int b)
    {
        Vector3 aCube = AxialToCube(new Vector2(a.x, a.z));
        Vector3 bCube = AxialToCube(new Vector2(b.x, b.z));
        
        return (int)Mathf.Max(
            Mathf.Abs(aCube.x - bCube.x),
            Mathf.Abs(aCube.y - bCube.y),
            Mathf.Abs(aCube.z - bCube.z)
        );
    }
    
// В классе HexGrid, добавьте отладочные сообщения в MoveShip:
// Исправленный метод MoveShip в классе HexGrid
public void MoveShip(Ship ship, Vector3Int targetPos, int newDirection)
{
    if (!CanMoveTo(targetPos, ship) || IsOccupied(targetPos))
    {
        Debug.LogWarning($"Невозможно переместить {ship.name} на {targetPos}");
        return;
    }
    
    Debug.Log($"HexGrid.MoveShip вызван: {ship.name} в {targetPos}, направление: {newDirection}");
    
    // Получаем старую позицию
    Vector3Int oldPos = ship.HexPosition;
    
    // Очищаем старую ячейку
    if (hexGrid.ContainsKey(oldPos))
        hexGrid[oldPos].OccupyingShip = null;
    
    // Используем метод MoveToPosition для перемещения корабля с учетом направления
    ship.MoveToPosition(targetPos, newDirection);
    
    // Обновляем ссылку в новой ячейке
    if (hexGrid.ContainsKey(targetPos))
        hexGrid[targetPos].OccupyingShip = ship;
    
    // Обновляем подсветку доступных ходов
    ClearHighlights();
    if (ship.MovementPoints > 0)
        HighlightMovementRange(ship);
    HighlightAttackRange(ship);
}

    
    private System.Collections.IEnumerator MoveShipSmoothly(Ship ship, Vector3 targetPosition)
    {
        Vector3 startPosition = ship.transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float speed = 5.0f;  // Скорость движения
        float distanceCovered = 0.0f;
        
        while (distanceCovered < journeyLength)
        {
            float fractionOfJourney = distanceCovered / journeyLength;
            ship.transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            distanceCovered += speed * Time.deltaTime;
            yield return null;
        }
        
        // Финальная позиция
        ship.transform.position = targetPosition;
    }
    
 // Обновить метод HandleHexClick в классе HexGrid
// Обновленный метод в классе HexGrid
public void HandleHexClick(Vector3Int hexPos)
{
    Debug.Log("Клик по гексу: " + hexPos);
    
    // Получаем выбранный корабль из GameManager
    Ship selectedShip = GameManager.Instance.GetSelectedShip();
    
    // Если у нас есть выбранный корабль
    if (selectedShip != null && GameManager.Instance.IsPlayerTurn)
    {
        // Проверяем, можно ли переместиться на эту клетку
        if (CanMoveTo(hexPos, selectedShip) && !IsOccupied(hexPos))
        {
            // Проверяем, что клетка в списке доступных для перемещения
            List<Vector3Int> availableMoves = GetMovementRange(selectedShip);
            
            bool canMove = false;
            foreach (var move in availableMoves)
            {
                if (move.x == hexPos.x && move.z == hexPos.z)
                {
                    canMove = true;
                    break;
                }
            }
            
            if (canMove)
            {
                // Определяем направление движения
                int newDirection = CalculateDirectionTowards(selectedShip.HexPosition, hexPos);
                
                // Перемещаем корабль с учетом нового направления
                MoveShip(selectedShip, hexPos, newDirection);
                Debug.Log("Корабль " + selectedShip.name + " перемещен на " + hexPos);
                return;
            }
        }
        
        // Проверяем, можем ли мы атаковать цель
        if (selectedShip.CanAttack)
        {
            int distance = CalculateHexDistance(selectedShip.HexPosition, hexPos);
            if (distance <= selectedShip.AttackRange && distance > 0)
            {
                // Атакуем цель
                selectedShip.FireAt(hexPos);
                return;
            }
        }
    }
    
    // Если на гексе есть свой корабль, выбираем его
    Hex hex = null;
    if (hexGrid.TryGetValue(hexPos, out hex) && hex.OccupyingShip != null && hex.OccupyingShip.PlayerOwned)
    {
        hex.OccupyingShip.Select();
    }
}

// Добавляем необходимый метод CalculateDirectionTowards
private int CalculateDirectionTowards(Vector3Int from, Vector3Int to)
{
    // Определяем относительное направление
    Vector3Int delta = to - from;
    
    // Учет смещения для нечетных столбцов
    if (from.x % 2 == 1) 
    {
        // Для нечетной колонки учитываем смещение
        if (delta.x > 0 && delta.z == 0) return 0; // восток
        if (delta.x > 0 && delta.z > 0) return 5; // юго-восток
        if (delta.x < 0 && delta.z > 0) return 4; // юго-запад
        if (delta.x < 0 && delta.z == 0) return 3; // запад
        if (delta.x < 0 && delta.z < 0) return 2; // северо-запад
        if (delta.x > 0 && delta.z < 0) return 1; // северо-восток
    }
    else
    {
        // Для четной колонки
        if (delta.x > 0 && delta.z == 0) return 0; // восток
        if (delta.x > 0 && delta.z < 0) return 1; // северо-восток
        if (delta.x < 0 && delta.z < 0) return 2; // северо-запад
        if (delta.x < 0 && delta.z == 0) return 3; // запад
        if (delta.x < 0 && delta.z > 0) return 4; // юго-запад
        if (delta.x > 0 && delta.z > 0) return 5; // юго-восток
    }
    
    // Если не удалось определить направление, возвращаем текущее направление корабля
    return selectedShip.Direction;
}

// Обработка перемещения
private void HandleMoveAction(Ship ship, Vector3Int targetPos)
{
    if (CanMoveTo(targetPos, ship) && !IsOccupied(targetPos))
    {
        // Получаем все доступные клетки для движения
        List<Vector3Int> availableMoves = GetMovementRange(ship);
        
        // Проверяем, что клетка в списке доступных
        if (availableMoves.Contains(targetPos))
        {
            // Определяем направление движения
            int newDirection = CalculateDirectionTowards(ship.HexPosition, targetPos);
            
            // Перемещаем корабль
            MoveShip(ship, targetPos, newDirection);
        }
    }
}

// Обработка поворота
private void HandleRotateAction(Ship ship, Vector3Int targetPos)
{
    // Для поворота корабль должен оставаться на месте
    if (ship.HexPosition == targetPos)
    {
        // Показываем круговое меню для выбора направления
        ShowRotationMenu(ship);
    }
}

// Добавить в класс HexGrid
public void HighlightSelectedHex(Vector3Int position)
{
    if (hexGrid.ContainsKey(position))
    {
        // Создаем выделение клетки под кораблем
        GameObject selectedHighlight = CreateHighlight(
            hexGrid[position].transform.position,
            movementHighlightMaterial);
        
        // Делаем выделение более заметным
        MeshRenderer renderer = selectedHighlight.GetComponent<MeshRenderer>();
        renderer.material.color = new Color(1f, 1f, 0f, 0.7f); // Яркий желтый
        
        // Поднимаем немного выше, чтобы было заметно
        selectedHighlight.transform.position += Vector3.up * 0.01f;
        
        // Добавляем в список для очистки позже
        highlightedHexes.Add(selectedHighlight);
    }
}

// Добавить в класс HexGrid
public void ShowRotationMenu(Ship ship)
{
    // Получаем текущее направление корабля
    int currentDirection = ship.Direction;
    
    // Определяем доступные направления для поворота
    List<int> availableDirections = new List<int>();
    
    // Проверяем возможность поворота в каждом направлении
    for (int dir = 0; dir < 6; dir++)
    {
        int turnAngle = Mathf.Abs((dir - currentDirection + 6) % 6);
        turnAngle = Mathf.Min(turnAngle, 6 - turnAngle);
        
        if (turnAngle <= ship.MaxTurnAngle)
        {
            availableDirections.Add(dir);
        }
    }
    
    // Подсвечиваем доступные направления
    foreach (int dir in availableDirections)
    {
        // Вычисляем позицию для отображения
        Vector3 position = ship.transform.position;
        
        // Добавляем смещение в зависимости от направления
        float angle = dir * 60f * Mathf.Deg2Rad; // 60 градусов между направлениями
        float distance = 1.0f; // расстояние от корабля
        
        position.x += Mathf.Cos(angle) * distance;
        position.z += Mathf.Sin(angle) * distance;
        
        // Создаем индикатор направления
        GameObject directionIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        directionIndicator.transform.position = position;
        directionIndicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        // Настраиваем материал
        MeshRenderer renderer = directionIndicator.GetComponent<MeshRenderer>();
        renderer.material.color = new Color(0f, 1f, 0f, 0.8f); // Зеленый цвет
        
        // Добавляем компонент для реагирования на клики
        DirectionSelector selector = directionIndicator.AddComponent<DirectionSelector>();
        selector.Initialize(ship, dir);
        
        // Добавляем в список для очистки позже
        highlightedHexes.Add(directionIndicator);
    }
}

// Обработка стрельбы
private void HandleFireAction(Ship ship, Vector3Int targetPos)
{
    // Проверяем, что цель в пределах дальности стрельбы
    int distance = CalculateHexDistance(ship.HexPosition, targetPos);
    
    if (distance <= ship.AttackRange && distance > 0)
    {
        // Стреляем по цели
        ship.FireAt(targetPos);
        
        // Обновляем UI
        ShipActionUI actionUI = FindObjectOfType<ShipActionUI>();
        if (actionUI != null)
        {
            actionUI.ShowForShip(ship); // Обновляем доступность кнопок
        }
    }
}
    
    private class MovementNode
    {
        public Vector3Int Position;
        public int RemainingMP;
    }
}