// Новый класс для выбора направления
public class DirectionSelector : MonoBehaviour
{
    private Ship targetShip;
    private int direction;
    
    public void Initialize(Ship ship, int dir)
    {
        targetShip = ship;
        direction = dir;
    }
    
    void OnMouseDown()
    {
        if (targetShip != null)
        {
            // Поворачиваем корабль в выбранном направлении
            bool success = targetShip.TryRotate(direction);
            
            if (success)
            {
                Debug.Log($"Корабль {targetShip.name} повернулся в направлении {direction}");
                
                // Уменьшаем очки движения (поворот стоит 1 очко)
                targetShip.MovementPoints--;
                
                // Очищаем подсветку
                GameManager.Instance.hexGrid.ClearHighlights();
                
                // Обновляем UI
                ShipActionUI actionUI = FindObjectOfType<ShipActionUI>();
                if (actionUI != null)
                {
                    actionUI.ShowForShip(targetShip);
                }
            }
        }
    }
}