using UnityEngine;

public class TowerPlacementZone : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private Vector3 towerOffset = Vector3.up;

    [Header("상태")]
    [SerializeField] private bool isOccupied = false; 

    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }
    public Vector3 GetTowerPosition()
    {
        return transform.position + towerOffset;
    }
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }


    public BaseTower GetPlacedTower()
    {
        if (!isOccupied)
            return null;

        foreach (Transform child in transform)
        {
            BaseTower tower = child.GetComponent<BaseTower>();
            if (tower != null)
            {
                return tower;
            }
        }

        return null;
    }
    public bool IsOccupied()
    {
        return GetPlacedTower() != null;
    }

}
