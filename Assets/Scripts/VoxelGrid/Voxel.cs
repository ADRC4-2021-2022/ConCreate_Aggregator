using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Status of the voxel. Dead are voxels that won't be used,
/// Alive are voxels that are currently activated,
/// Available are voxels that can be activated
/// </summary>
public enum VoxelState { Dead = 0, Alive = 1, Available = 2 }

/// <summary>
/// Data structure for a voxel in a grid
/// </summary>
public class Voxel
{

    #region public fields
    public Vector3Int Index { get; private set; }

    public Corner[] Corners;

    /// <summary>
    /// Change the value of _showVoxel and enable/disable the _goVoxelTrigger 
    /// </summary>
    public bool ShowAliveVoxel
    {
        get
        {
            return _showAliveVoxel;
        }
        set
        {
            _showAliveVoxel = value;
            ChangeVoxelVisibility();
        }
    }

    /// <summary>
    /// Change the value of _showVoxel and enable/disable the _goVoxelTrigger 
    /// </summary>
    public bool ShowAvailableVoxel
    {
        get
        {
            return _showAvailableVoxel;
        }
        set
        {
            _showAvailableVoxel = value;
            ChangeVoxelVisibility();
        }
    }

    /// <summary>
    /// Get and set the status of the voxel. When setting the status, the linked gameobject will be enable or disabled depending on the state.
    /// </summary>
    public VoxelState Status
    {
        get
        {
            return _voxelStatus;
        }
        set
        {
            _voxelStatus = value;
            ChangeVoxelVisibility();
        }
    }

    /// <summary>
    /// Get the centre point of the voxel in worldspace
    /// </summary>
    public Vector3 Centre => _gridOrigin + (Vector3)Index * _voxelSize + Vector3.one * 0.5f * _voxelSize;
    #endregion

    #region private fields
    private GameObject _goVoxelTrigger;
    private VoxelGrid _grid;

    private VoxelState _voxelStatus;
    private bool _showAliveVoxel;
    private bool _showAvailableVoxel;

    private float _scalefactor = 0.95f;
    private float _voxelSize => _grid.VoxelSize;
    private Vector3 _gridOrigin => _grid.Origin;



    #endregion

    #region constructors
    public Voxel(int x, int y, int z, VoxelGrid grid) : this(new Vector3Int(x, y, z), grid) { }

    public Voxel(Vector3Int index, VoxelGrid grid)
    {
        Index = index;
        _grid = grid;
        CreateGameobject();
        ChangeMaterial(grid.MatTrans);


        Status = VoxelState.Available;
        ChangeVoxelVisibility();
    }
    #endregion

    #region private functions

    #endregion

    #region public functions
    public void ChangeVoxelVisibility()
    {
        bool visible = false;
        if (Status == VoxelState.Dead) visible = false;
        if (Status == VoxelState.Available && _showAvailableVoxel) visible = true;
        if (Status == VoxelState.Alive && _showAliveVoxel) visible = true;

        _goVoxelTrigger.SetActive(visible);
    }
    public void CreateGameobject()
    {
        _goVoxelTrigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _goVoxelTrigger.name = $"Voxel {Index}";
        _goVoxelTrigger.tag = "Voxel";
        _goVoxelTrigger.transform.position = Centre;
        _goVoxelTrigger.transform.localScale = _scalefactor * _voxelSize * Vector3.one;
        _goVoxelTrigger.transform.SetParent(_grid.GOGrid.transform);


        VoxelTrigger trigger = _goVoxelTrigger.AddComponent<VoxelTrigger>();
        trigger.AttachedVoxel = this;
    }

    public void ChangeMaterial(Material mat)
    {
        _goVoxelTrigger.GetComponent<MeshRenderer>().material = mat;
    }

    public List<Voxel> GetFaceNeighbourList()
    {
        List<Voxel> neighbours = new List<Voxel>();
        foreach (Vector3Int direction in Util.Directions)
        {
            Vector3Int neighbourIndex = Index + direction;
            if (Util.CheckInBounds(_grid.GridDimensions, neighbourIndex))
            {
                neighbours.Add(_grid.GetVoxelByIndex(neighbourIndex));
            }
        }

        return neighbours;
    }

    /// <summary>
    /// Get all the voxels that exist with relative indices to the this voxel. 
    /// </summary>
    /// <param name="relativeIndices">indexes related to the voxels indices</param>
    /// <returns>List of relative indices. If requested indices are out of bounds, the list will be empty</returns>
    public List<Voxel> GetRelatedVoxels(List<Vector3Int> relativeIndices)
    {
        List<Voxel> relatedVoxels = new List<Voxel>();
        foreach (Vector3Int relativeIndex in relativeIndices)
        {
            Vector3Int newIndex = Index + relativeIndex;
            if (Util.CheckInBounds(_grid.GridDimensions, newIndex))
            {
                relatedVoxels.Add(_grid.GetVoxelByIndex(newIndex));
            }
        }

        return relatedVoxels;
    }

    /// <summary>
    /// Toggle the visibility status of the neighbours
    /// </summary>
    public void ToggleNeighbours()
    {
        List<Voxel> neighbours = GetFaceNeighbourList();

        foreach (var neighbour in neighbours)
        {
            neighbour.ShowAliveVoxel = !neighbour.ShowAliveVoxel;
        }
    }

    public void SetColor(Color color)
    {
        _goVoxelTrigger.GetComponent<MeshRenderer>().material.color = color;
    }

    #endregion
}
