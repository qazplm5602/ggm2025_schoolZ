using System;
using UnityEngine;

public class TableSocket : MonoBehaviour
{
    [SerializeField] private InputSO input;
    private TableCore currentTable;
    private Collider[] overlapResult = new Collider[1];
    [SerializeField] private LayerMask detectLayer;
    [SerializeField] private Transform detectPoint;
    [SerializeField] private Vector3 detectSize;

    void OnEnable()
    {
        input.OnInteractPress += HandleInteract;
    }

    void OnDisable()
    {
        input.OnInteractPress -= HandleInteract;
    }

    public void AttachNearTable()
    {
        int count = Physics.OverlapBoxNonAlloc(detectPoint.position, detectSize, overlapResult, detectPoint.rotation, detectLayer);
        print($"count: {count}");
    }

    private void HandleInteract()
    {
        if (currentTable)
        {

        }
        else
        {
            AttachNearTable();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(detectPoint.position, detectSize);
        Gizmos.color = Color.white;
    }
}
