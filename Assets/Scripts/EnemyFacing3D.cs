using UnityEngine;

public class EnemyFacing3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform modelPivot;

    [Header("Facing")]
    [SerializeField] private float faceLeftY = 0f;
    [SerializeField] private float faceRightY = 180f;

    public void FaceLeft()
    {
        if (modelPivot == null) return;
        modelPivot.localRotation = Quaternion.Euler(0f, faceLeftY, 0f);
    }

    public void FaceRight()
    {
        if (modelPivot == null) return;
        modelPivot.localRotation = Quaternion.Euler(0f, faceRightY, 0f);
    }

    public void FaceByDirection(float directionX)
    {
        if (directionX < 0f)
            FaceLeft();
        else if (directionX > 0f)
            FaceRight();
    }

    public void FaceTarget(Transform target, Transform self)
    {
        if (target == null || self == null) return;

        if (target.position.x < self.position.x)
            FaceLeft();
        else
            FaceRight();
    }
}