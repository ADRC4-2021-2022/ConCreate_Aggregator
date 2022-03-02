using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Aggregator : MonoBehaviour
{
    #region Serialized fields
    [SerializeField]
    private float connectionTolerance = 10f;

    [SerializeField]
    private float _voxelSize = 0.75f;

    [SerializeField]
    private int _voxelOffset = 0;

    private VoxelGrid _grid;
    private GameObject _goVoxelGrid;
    #endregion

    #region public fields


    #endregion

    #region private fields
    //Library contains all the parts that haven't been placed yet
    public List<Part> _library = new List<Part>();

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
        //Load all the prefabs
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

        //Select the prefabs with tag Part
        _library = prefabs.Where(g => g.tag == "Part").Select(g => new Part(g)).ToList();


        foreach (var part in _library)
        {
            foreach (var connection in part.Connections)
            {
                _connections.Add(connection);
            }
        }

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

        int rndY = Random.Range(0, 360);

        randomPart.PlaceFirstPart(Vector3.zero, Quaternion.Euler(new Vector3(0, rndY, 0)));
    }

    IEnumerator StartFindNextConnection()
    {
        for (int i = 0; i < _library.Count; i++)
        {
            FindNextConnection();
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(1f);
    }



    private void FindNextConnection()
    {
        
        //Get a random connection out of the available connections list
        int rndConnectionIndex = Random.Range(0, _availableConnections.Count);
        Connection randomAvailableConnection = _availableConnections[rndConnectionIndex];
        //randomConnection.NameGameObject("NextConnection");

        //Get the connection tolerance
        float connectionLength = randomAvailableConnection.Length;
        float minLength = connectionLength - connectionTolerance;
        float maxLength = connectionLength + connectionTolerance;

        //Find a similar connection
        List<Connection> possibleConnections = new List<Connection>();

        foreach (var connection in _libraryConnections)
        {
            if (connection.Length > minLength && connection.Length < maxLength)
            {
                possibleConnections.Add(connection);
            }
        }

        if (possibleConnections.Count == 0)
        {
            Debug.Log("No connections found within the dimension range");
            return;
        }
        //The line below is a shorthand notation for the foreach loop above
        //List<Connection> possibleConnections = _libraryConnections.Where(c => c.Length > minLength && c.Length < maxLength).ToList();


        bool partPlaced = false;
        //______While(partPlaced==false&&possibleConnection.count>0)

        //Get a random connection out of the available connections list
        int rndPossibleConnectionIndex = Random.Range(0, possibleConnections.Count);
        Connection connectionToPlace = possibleConnections[rndPossibleConnectionIndex];
        Part currentPart = connectionToPlace.ThisPart;
        currentPart.PositionPart(randomAvailableConnection, connectionToPlace);

        RegenerateVoxelGrid(currentPart);
        if( CheckCollision(currentPart))
        {
            //Set the part as placed
            partPlaced = true;
        }
        else
        {
            //reset the part
            //remove the tried connection from the list of possible connections
        }


        ///End While loop


        if (!partPlaced)
            Debug.Log("No parts could be added");
    }


    private bool CheckCollision(Part partToCheck)
    {
        //Set all voxels inactive
        //Set the voxels in the placed building parts active
        //Get all the voxels in the partToCheck
        //Check how many of the voxels in partToCheck are active
        //If (the active voxels in the part to check < maxOverlap) retrun true

        return false;
    }

    /// <summary>
    /// Create A Voxelgrid for the current building
    /// </summary>
    /// <param name="newPart">the new part to check intersections with</param>
    private void RegenerateVoxelGrid(Part newPart)
    {
        if(_goVoxelGrid != null)GameObject.Destroy(_goVoxelGrid);
        _goVoxelGrid = new GameObject("VoxelGrid");
        Vector3Int gridDimensions;
        Vector3 origin;

        //Get the bounds of your building
        Bounds bounds = newPart.GOPart.GetComponentInChildren<MeshCollider>().bounds;

        foreach (var part in _buildingParts)
        {
            bounds.Encapsulate(part.GOPart.GetComponentInChildren<MeshCollider>().bounds);
        }

        //Get the grid parameters and create grid
        gridDimensions = (bounds.size / _voxelSize).ToVector3IntRound() + Vector3Int.one * _voxelOffset * 2;
        origin = bounds.center - (Vector3)gridDimensions * _voxelSize / 2;
        _grid = new VoxelGrid(gridDimensions, _voxelSize, origin, _goVoxelGrid);
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
    }
    #endregion
}
