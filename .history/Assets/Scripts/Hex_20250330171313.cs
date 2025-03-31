using UnityEngine;

public enum HexType { Water, Shallow, Island, Reef, Fort, PlayerBase, EnemyBase }

public class Hex : MonoBehaviour
{
    public Vector3Int GridPosition { get; private set; }
    public HexType Type { get; private set; }
    public Ship OccupyingShip { get; set; }
    public int OwnerID { get; set; } = -1;
    public int Defense { get; set; } = 0;

    private MeshRenderer meshRenderer;
    private HexGrid hexGrid;

    public void Initialize(Vector3Int position, HexType type)
    {
        GridPosition = position;
        Type = type;
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        hexGrid = FindObjectOfType<HexGrid>();
        
        UpdateVisuals();
    }

    public void SetType(HexType newType)
    {
        Type = newType;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (meshRenderer != null && hexGrid != null)
        {
            meshRenderer.material = hexGrid.GetMaterialForHexType(Type);
        }
    }

    void OnMouseDown()
    {
        // Обработка клика по шестиугольнику
        if (hexGrid != null)
        {
            hexGrid.HandleHexClick(GridPosition);
        }
    }
}