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
        if (count == 0) return;

        Collider tableCol = overlapResult[0];
        TableCore tableCore = tableCol.GetComponent<TableCore>();
        currentTable = tableCore;
    }

    void Update()
    {
        if (!currentTable) return;

        Transform pushSocket = currentTable.PushSocket;
        Vector3 targetPos = transform.position;
        Quaternion targetRot = transform.rotation;

        // TableCore의 기본 회전(예: -90, 0, 0)이 필요하다면 아래처럼 보정
        Quaternion baseRot = Quaternion.Euler(-90, 0, 0); // TableCore 프리팹의 기본 회전값
        Quaternion yRot = Quaternion.Euler(0, targetRot.eulerAngles.y, 0);
        currentTable.transform.rotation = yRot * baseRot;

        Vector3 localOffset = pushSocket.localPosition;
        Vector3 worldOffset = currentTable.transform.rotation * localOffset;
        currentTable.transform.position = targetPos - worldOffset;
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
