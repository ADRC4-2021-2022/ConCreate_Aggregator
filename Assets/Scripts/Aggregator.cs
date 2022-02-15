using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class Aggregator : MonoBehaviour
{
    #region Serialized fields
    [SerializeField]
    private float connectionTollerance = 100f;

    #endregion

    #region public fields

    #endregion

    #region private fields
    //Library contains all the parts that haven't been placed yet
    public List<Part> _library = new List<Part>();

    //All the parts that are already place in the building
    public List<Part> _buildingParts = new List<Part>();

    //All the connection that are available in your building
    //Regenerate this list every time you place a part
    public List<Connection> _connections = new List<Connection>();

    //Similar connections
    public List<Connection> possibleConnections = new List<Connection>();

    Connection anchorConnection;
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
        //Gather all the prefabs for the parts and load the connections
        _library.Add(new Part(Resources.Load("Prefabs/Parts/01P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefabs/Parts/02P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefabs/Parts/03P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefabs/Parts/04P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefabs/Parts/05P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefabs/Parts/06P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefabs/Parts/07P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefabs/Parts/08P") as GameObject));

        foreach (var part in _library)
        {
            foreach (var connection in part.Connections)
            {
                _connections.Add(connection);
            }
        }
        PlaceFirstBlock();
        FindNextConnection();
        SetNextPart();
    }

    private void PlaceFirstBlock()
    {
        int rndPartIndex = Random.Range(0, _library.Count);
        Part randomPart = _library[rndPartIndex];

        int rndY = Random.Range(0, 360);

        randomPart.PlaceFirstPart(Vector3.zero, Quaternion.Euler(new Vector3(0, rndY, 0)));
    }

    private void FindNextConnection()
    {
        //Get a random connection out of the available connections list
        int rndConnectionIndex = Random.Range(0, _availableConnections.Count);
        Connection randomConnection = _availableConnections[rndConnectionIndex];
        randomConnection.NameGameObject("NextConnection");

        //Get the connection tolerance
        float connectionLength = randomConnection.Length;
        float minLength = connectionLength - connectionTollerance;
        float maxLength = connectionLength + connectionTollerance;

        //Find a similar connection
        List<Connection> possibleConnections = new List<Connection>();

        foreach (var connection in _libraryConnections)
        {
            if (connection.Length > minLength && connection.Length < maxLength)
            {
                possibleConnections.Add(connection);
            }
        }

        //The line below is a shorthand notation for the foreach loop above
        //List<Connection> possibleConnections = _libraryConnections.Where(c => c.Length > minLength && c.Length < maxLength).ToList();

        //Get a random connection out of the available connections list
        int rndPossibleConnectionIndex = Random.Range(0, possibleConnections.Count);
        Connection rndPossibleConnection = possibleConnections[rndPossibleConnectionIndex];

        rndPossibleConnection.ThisPart.PlacePart(randomConnection.Position,/*randomConnection.Normal*/Quaternion.identity, rndPossibleConnection);
    }
    #endregion

    #region public functions
    public void SetNextPart()
    {
        //Get the list of available connections in your building
        List<Connection> connections = _connections;

        for (int i = 0; i < _connections.Count; i++)
        {
            var currentConnection = _connections[i];
            int randomIndex = Random.Range(i, _connections.Count);
            _connections[i] = _connections[randomIndex];
            _connections[randomIndex] = currentConnection;
        }

        //List<Connection> shuffledList = _connections.OrderBy(x => Random.value).ToList();
        //Select a random connection
        //var randomConnection = shuffledList[1];
        //find all the parts that have a fitting connection
        Vector3 position = new Vector3();
        Quaternion rotation = new Quaternion();

        foreach (Part partToPlace in _library)
        {
            foreach (Connection connection in possibleConnections)
            {
                partToPlace.PlacePart(position, rotation, anchorConnection);
                _buildingParts.Add(partToPlace);
            }
        }
        //FOREACH PART IN _LIBRARY --> FOREACH CONNECTION IN PART.CONNECTIONS
        //try to place it
        //partToPlace.PlacePart

        //if success => hooray
        //consider coroutine to move to the next part to try to place (call SetNextPart again)
        //if failure => remove part from list and try another one
        //_library.remove(partToPlace);
        //if none of the parts work, disable connection and try next connection
        //connection.Available = false;
    }

    #endregion

    #region private functions

    #endregion

    #region Canvas function

    #endregion
}
