using UnityEngine;

public class EnemyRoam3D : MonoBehaviour
{
    [Header("Roaming")]
    [SerializeField] private float movementDistance = 10f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool startMovingLeft = true;

    private Vector3 startPos;
    private float leftEdge;
    private float rightEdge;
    private bool movingLeft;

    public bool IsRoaming { get; private set; } = true;
    public bool MovingLeft => movingLeft;

    private void Start()
    {
        startPos = transform.position;
        leftEdge = startPos.x - movementDistance;
        rightEdge = startPos.x + movementDistance;
        movingLeft = startMovingLeft;
    }

    private void Update()
    {
        if (!IsRoaming) return;

        Vector3 pos = transform.position;

        if (movingLeft)
        {
            pos.x -= moveSpeed * Time.deltaTime;

            if (pos.x <= leftEdge)
            {
                pos.x = leftEdge;
                movingLeft = false;
            }
        }
        else
        {
            pos.x += moveSpeed * Time.deltaTime;

            if (pos.x >= rightEdge)
            {
                pos.x = rightEdge;
                movingLeft = true;
            }
        }

        transform.position = new Vector3(pos.x, startPos.y, startPos.z);
    }

    public void StopRoaming()
    {
        IsRoaming = false;
    }

    public void StartRoaming()
    {
        IsRoaming = true;
    }
}