using UnityEngine;

public class VoxelgridManager : MonoBehaviour
{
    #region Dimensions
    [SerializeField]
    [Range(1, 50)]
    private int _xDimension = 5;

    [SerializeField]
    [Range(1, 50)]
    private int _yDimension = 5;

    [SerializeField]
    [Range(1, 50)]
    private int _zDimension = 5;

    [SerializeField]
    [Range(0, 5)]
    private float _voxelSize = 1f;

    [SerializeField]
    private Vector3 _origin = Vector3.zero;
    #endregion
}
