using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    public GameObject respawnPoint;            // where the player should respawn
    public event Action<Checkpoint> OnReached; // fired when this checkpoint is hit
    Collider _col;
    public Collider Col { 
        get { return _col; }
        set { _col = value; } 
    }
    ZeroGravity _zeroG;                       // cached player component

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    public void Initialize(ZeroGravity zeroG, bool active)
    {
        _zeroG = zeroG;
        _col.enabled = active;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_zeroG != null && other.CompareTag("Player"))
        {
            _zeroG.respawnLoc = respawnPoint;
            _col.enabled = false;             // deactivate this checkpoint
            OnReached?.Invoke(this);
        }
    }

    public void ToggleCheckpoint(bool isActive)
    {
        _col.enabled = isActive;
    }

    public void TriggerCheckpointManually()
    {
        _zeroG.respawnLoc = respawnPoint;
        _col.enabled = false;             // deactivate this checkpoint
        OnReached?.Invoke(this);
    }

    void OnDrawGizmos()
    {
        // Draw sphere at respawn point
        if (respawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(respawnPoint.transform.position, 0.5f);
            Gizmos.color = new Color(0, 1, 0, 0.25f);
            Gizmos.DrawSphere(respawnPoint.transform.position, 0.25f);

            // Draw line from checkpoint to respawn
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, respawnPoint.transform.position);

            // Draw arrow showing respawn forward direction
            Gizmos.color = Color.green;
            Vector3 arrowStart = respawnPoint.transform.position;
            Vector3 arrowDir = respawnPoint.transform.forward * 1.5f;
            Gizmos.DrawRay(arrowStart, arrowDir);

            // Draw arrowhead
            Vector3 right = Quaternion.LookRotation(arrowDir) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(arrowDir) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
            Gizmos.DrawRay(arrowStart + arrowDir, right * 0.3f);
            Gizmos.DrawRay(arrowStart + arrowDir, left * 0.3f);
        }

        // Draw the checkpoint trigger area (if it has a collider)
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
        {
            Gizmos.color = Color.cyan;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}
