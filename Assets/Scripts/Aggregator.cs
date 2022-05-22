using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Aggregator : MonoBehaviour
{
    #region Serialized fields
    [SerializeField]
    private float connectionTolerance = 0.1f;

    [SerializeField]
    private float _voxelSize = 0.4f;

    [SerializeField]
    private float _collisionCheckRadius = 3f;

    [SerializeField]
    private int _collisionMaxNeighboursToCheck = 16;

    [SerializeField]
    private int _voxelOffset = 0;

    [SerializeField]
    private int _maxOverlap = 2;

    private VoxelGrid _grid;
    private GameObject _goVoxelGrid;

    private Corner _corner;
    #endregion

    #region private fields
    private Material _matTrans;
    private Collider[] _collisionNeighbours;
    #endregion

    #region public fields
    //Library contains all the parts that haven't been placed yet
    public List<Part> _library = new List<Part>();
    public Material MatTrans
    {
        get
        {
            if (_matTrans == null)
            {
                _matTrans = Resources.Load<Material>("Materials/matTrans");
            }
            return _matTrans;
        }
    }

    //All the parts that are already place in the building
    public List<Part> _buildingParts
    {
        get
        {
            return _library.Where(p => p.Status == PartStatus.Placed).ToList();
        }
    }

    //All the connection that are available in your building
    public List<Connection> _connections = new List<Connection>();

    public List<Connection> _availableConnections
    {
        get
        {
            return _connections.Where(c => c.Available).ToList();
        }
    }

    //All the connections that are not part of a place block
    public List<Connection> _libraryConnections
    {
        get
        {
            return _connections.Where(c => c.LibraryConnection).ToList();
        }
    }
    #endregion

    #region Monobehaviour functions
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 50; i++)
        {
            //Load all the prefabs
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

            //Select the prefabs with tag Part
            _library = prefabs.Where(g => g.CompareTag("Part")).Select(g => new Part(g)).ToList();

            foreach (var part in _library)
            {
                foreach (var connection in part.Connections)
                {
                    _connections.Add(connection);
                }
            }
        }
        _collisionNeighbours = new Collider[_collisionMaxNeighboursToCheck];
        PlaceFirstBlock();
        //StartCoroutine(StartFindNextConnection());
    }
    #endregion

    #region public functions
    #endregion

    #region private functions
    private void PlaceFirstBlock()
    {
        int rndPartIndex = Random.Range(0, _library.Count);
        Part randomPart = _library[rndPartIndex];

        int rndZ = Random.Range(0, 4);
        int rndY = Random.Range(0, 4);

        randomPart.PlaceFirstPart(Vector3.zero, Quaternion.Euler(new Vector3(0, rndY * 90, rndZ * 90)));
        randomPart.Name = $"{randomPart.Name} added {_buildingParts.Count}";
    }

    IEnumerator StartFindNextConnection()
    {
        for (int i = 0; i < _library.Count; i++)
        {
            if (!FindNextConnection())
            {

                StartCoroutine(StartFindNextConnection());
            }
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(1f);
    }

    private bool FindNextConnection()
    {
        if (_availableConnections.Count == 0)
        {
            Debug.Log("There are no more available connections");
            return false;
        }
        //Get a random connection out of the available connections list
        int rndConnectionIndex = Random.Range(0, _availableConnections.Count);
        Connection randomAvailableConnection = _availableConnections[rndConnectionIndex];
        //randomConnection.NameGameObject("NextConnection");

        //Get the connection tolerance
        float connectionLength = randomAvailableConnection.Length;
        float minLength = connectionLength - connectionTolerance;
        float maxLength = connectionLength + connectionTolerance;
        Debug.Log($"minlength is {minLength} and maxlength is {maxLength}");

        float connectionWidth = randomAvailableConnection.Width;
        float minWidth = connectionWidth - connectionTolerance;
        float maxWidth = connectionWidth + connectionTolerance;
        Debug.Log($"minwidth is {minWidth} and maxwidth is {maxWidth}");

        //Find a similar connection
        List<Connection> possibleConnections = new List<Connection>();

        foreach (var connection in _libraryConnections)
        {
            if (connection.Length > minLength && connection.Length < maxLength
                && connection.Width > minWidth && connection.Width < maxWidth)
            {
                possibleConnections.Add(connection);
            }
        }

        if (possibleConnections.Count == 0)
        {
            Debug.Log("No connections found within the dimension range");
            randomAvailableConnection.Available = false;
            return false;
        }
        //The line below is a shorthand notation for the foreach loop above
        //List<Connection> possibleConnections = _libraryConnections.Where(c => c.Length > minLength && c.Length < maxLength).ToList();

        bool partPlaced = false;
        int attemptCounter = 1;

        while (!partPlaced && possibleConnections.Count > 0 && randomAvailableConnection.Available == true)
        {
            // Debug.Log($"Part placement attempt number #{attemptCounter}");
            attemptCounter++;
            //Get a random connection out of the available connections list
            int rndPossibleConnectionIndex = Random.Range(0, possibleConnections.Count);
            Connection connectionToPlace = possibleConnections[rndPossibleConnectionIndex];
            Part currentPart = connectionToPlace.ThisPart;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            currentPart.PositionPart(randomAvailableConnection, connectionToPlace);

            //RegenerateVoxelGrid(currentPart);
            if (CheckCollisionWithPhysicsPenetration(currentPart))
            {
                //Set the part as placed
                currentPart.PlacePart(connectionToPlace);
                randomAvailableConnection.Available = false;
                currentPart.Name = $"{currentPart.Name} added {_buildingParts.Count}";
                partPlaced = true;
                return true;
            }
            else
            {
                //reset the part
                currentPart.ResetPart();
                //remove the tried connection from the list of possible connections
                possibleConnections.Remove(connectionToPlace);
                //RegenerateVoxelGrid(currentPart);
                if (possibleConnections.Count <= 0)
                {
                    randomAvailableConnection.Available = false;
                    Debug.LogWarning($"Connection doesn't have any possible connecting parts");
                    randomAvailableConnection.Visible = true;
                    return false;
                }
            }
        }
        ///End While loop

        if (!partPlaced)
            Debug.Log("No parts could be added");

        return true;
    }

    /// <returns>Return true if the part to check does not collide with the existing building.</returns>
    private bool CheckCollisionWithPhysicsPenetration(Part currentPart)
    {
        var thisCollider = currentPart.Collider;
        if (!thisCollider) return true; // nothing to do without a Collider attached

        int count = Physics.OverlapSphereNonAlloc(thisCollider.gameObject.transform.position, _collisionCheckRadius, _collisionNeighbours);

        for (int i = 0; i < count; ++i)
        {
            var otherCollider = _collisionNeighbours[i];

            if (otherCollider == thisCollider || otherCollider.gameObject.CompareTag("BoundingBox"))
                continue; // skip this collider or bounding box collider

            Vector3 otherPosition = otherCollider.gameObject.transform.position;
            Quaternion otherRotation = otherCollider.gameObject.transform.rotation;
            bool isOverlapping = Physics.ComputePenetration(
                thisCollider, thisCollider.gameObject.transform.position, thisCollider.gameObject.transform.rotation,
                otherCollider, otherPosition, otherRotation,
                out _, out _);

            // draw a line showing the depenetration direction if overlapped
            if (isOverlapping) return false;
        }
        return true;
    }

    /// <summary>
    /// Check if the new positioned part is intersecting with the existing building parts.
    /// </summary>
    /// <param name="partToCheck">The new positioned part</param>
    /// <returns>Return true if the part to check does not collide with the existing building.</returns>
    private bool CheckCollision(Part partToCheck)
    {
        var boundingBoxBounds = GameObject.FindGameObjectWithTag("BoundingBox").GetComponent<MeshCollider>().bounds;
        //if (!boundingBoxBounds.Contains(partToCheck.Collider.bounds.min) && !boundingBoxBounds.Contains(partToCheck.Collider.bounds.max))
        //Check if the corners of the bounds of your part are inside your bounding box to avoid parts sticking out of your bounding box.
        if (!boundingBoxBounds.Intersects(partToCheck.Collider.bounds))
        {
            Debug.Log($"{partToCheck.Name} is outside bounds");
            return false;
        }

        //Set all voxels inactive
        _grid.GetVoxels().ToList().ForEach(v => v.Status = VoxelState.Available);

        //Set the voxels in the placed building parts active, starting from corners and getting the voxels connected to them
        foreach (var part in _buildingParts)
        {
            var buildingPartBounds = part.GOPart.GetComponentInChildren<MeshCollider>().bounds;


            //Activate voxels inside the parts
            foreach (Voxel voxel in _grid.GetVoxels())
            {
                if (IsInsideCentre(voxel, part.Collider))
                {
                    voxel.Status = VoxelState.Alive;
                    voxel.ChangeVoxelVisibility();

                }
            }

            /*
            var cornersInBounds = _grid.GetCorners().Where(c => buildingPartBounds.Contains(c.Index)).ToList();
            //Debug.Log($"cornersInBounds count: {cornersInBounds.Count}");

            foreach (var corner in cornersInBounds)
            {
                corner.GetConnectedVoxels().ToList().ForEach(v => v.Status = VoxelState.Alive);
            }*/
        }
        int voxelsOverlap = 0;
        foreach (var voxel in _grid.GetVoxels())
        {
            if (voxel.Status == VoxelState.Alive && IsInsideCentre(voxel, partToCheck.Collider)) voxelsOverlap++;
        }

        /*
        //Get all the corners in the partToCheck
        List<Corner> partToCheckCorners = _grid.GetCorners().Where(c =>
            partToCheck.GOPart.GetComponentInChildren<MeshCollider>().bounds.Contains(c.Index)).ToList();

        // From the corners get the connected voxels alive and add to the list of voxels inside the part being placed
        List<Voxel> partToCheckActiveVoxels = new List<Voxel>();

        foreach (var corner in partToCheckCorners)
        {
            var voxelsInPartToCheck = corner.GetConnectedVoxels().Where(v => v.Status == VoxelState.Alive);
            foreach (var voxel in voxelsInPartToCheck)
            {
                partToCheckActiveVoxels.Add(voxel);
            }
        }
        //Debug.Log($"partToCheckActiveVoxels count: {partToCheckActiveVoxels.Count}");
        //If (the active voxels in the part to check < maxOverlap) return true
        */
        if (voxelsOverlap < _maxOverlap)
        {
            return true;
        }
        Debug.Log("Part overlapping");
        return false;
    }

    /// <summary>
    /// Create A Voxelgrid for the current building
    /// </summary>
    /// <param name="newPart">the new part to check intersections with</param>
    private void RegenerateVoxelGrid(Part newPart)
    {
        if (_goVoxelGrid != null) GameObject.Destroy(_goVoxelGrid);
        _goVoxelGrid = new GameObject("VoxelGrid");
        Vector3Int gridDimensions;
        Vector3 origin;

        //Get the bounds of your building
        Bounds bounds = newPart.GOPart.GetComponentInChildren<MeshCollider>().bounds;

        foreach (var part in _buildingParts)
        {
            bounds.Encapsulate(part.GOPart.GetComponentInChildren<MeshCollider>().bounds);
            //Debug.Log($"bound extents: {bounds.extents.x *2}, {bounds.extents.y *2}, {bounds.extents.z *2}");
        }

        //Get the grid parameters and create grid
        gridDimensions = (bounds.size / _voxelSize).ToVector3IntRound() + Vector3Int.one * _voxelOffset * 2;
        origin = bounds.center - (Vector3)gridDimensions * _voxelSize / 2;
        _grid = new VoxelGrid(gridDimensions, _voxelSize, origin, _goVoxelGrid, MatTrans);
    }


    /// <summary>
    /// Check if a voxel is inside the mesh, using the Voxel centre
    /// </summary>
    /// <param name="voxel">voxel to check</param>
    /// <returns>true if inside the mesh</returns>
    public bool IsInsideCentre(Voxel voxel, Collider collider)
    {
        var point = voxel.Centre;
        return Util.PointInsideCollider(point, collider);
    }
    #endregion

    #region Canvas functions
    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 120, 200, 50), "Place Next Part"))
        {
            FindNextConnection();
        }
        else if (GUI.Button(new Rect(10, 200, 200, 50), "AutoFiller"))
        {
            StartCoroutine(StartFindNextConnection());
        }
    }
    #endregion
}
