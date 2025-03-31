// Создать новый класс ShipActionUI
using UnityEngine;
using UnityEngine.UI;

public class ShipActionUI : MonoBehaviour
{
    public Button moveButton;
    public Button rotateButton;
    public Button fireButton;
    public Button cancelButton;
    
    private Ship selectedShip;
    private ActionMode currentMode = ActionMode.None;
    
    public enum ActionMode
    {
        None,
        Move,
        Rotate,
        Fire
    }
    
    private void Start()
    {
        // Инициализация кнопок
        moveButton.onClick.AddListener(() => SetMode(ActionMode.Move));
        rotateButton.onClick.AddListener(() => SetMode(ActionMode.Rotate));
        fireButton.onClick.AddListener(() => SetMode(ActionMode.Fire));
        cancelButton.onClick.AddListener(Cancel);
        
        // Скрываем UI изначально
        gameObject.SetActive(false);
    }
    
    public void ShowForShip(Ship ship)
    {
        selectedShip = ship;
        currentMode = ActionMode.None;
        
        // Активируем/деактивируем кнопки в зависимости от возможностей корабля
        moveButton.interactable = ship.MovementPoints > 0;
        fireButton.interactable = ship.CanAttack;
        
        // Показываем UI
        gameObject.SetActive(true);
    }
    
    public void SetMode(ActionMode mode)
    {
        if (selectedShip == null)
            return;
            
        currentMode = mode;
        
        // Подсвечиваем доступные действия в зависимости от режима
        HexGrid hexGrid = GameManager.Instance.hexGrid;
        hexGrid.ClearHighlights();
        
        switch (mode)
        {
            case ActionMode.Move:
                hexGrid.HighlightMovementRange(selectedShip);
                break;
            case ActionMode.Rotate:
                // Подсветка для доступных поворотов
                // (упрощенно - текущая клетка)
                hexGrid.HighlightSelectedHex(selectedShip.HexPosition);
                break;
            case ActionMode.Fire:
                hexGrid.HighlightAttackRange(selectedShip);
                break;
        }
    }
    
    public void Cancel()
    {
        // Снимаем выделение с корабля
        if (selectedShip != null)
        {
            selectedShip.Deselect();
            GameManager.Instance.OnShipDeselected(selectedShip);
        }
        
        // Скрываем UI
        gameObject.SetActive(false);
    }
    
    // Getter для текущего режима
    public ActionMode GetCurrentMode()
    {
        return currentMode;
    }
}