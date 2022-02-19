using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class Aggregator : MonoBehaviour
{
    #region Serialized fields
    [SerializeField]
    private float connectionTollerance = 500f;

    #endregion

    #region public fields

    #endregion

    #region private fields
    //Library contains all the parts that haven't been placed yet
    public List<Part> _library = new List<Part>();

    //All the parts that are already place in the building
    public List<Part> _buildingParts = new List<Part>();

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
        StartCoroutine(StartFindNextConnection());
    }

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

    private bool FindNextConnection()
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

        if(possibleConnections.Count == 0)
        {
            Debug.Log("No connections found within the dimension range");
            return false;
        }
        //The line below is a shorthand notation for the foreach loop above
        //List<Connection> possibleConnections = _libraryConnections.Where(c => c.Length > minLength && c.Length < maxLength).ToList();

        //Get a random connection out of the available connections list
        int rndPossibleConnectionIndex = Random.Range(0, possibleConnections.Count);
        Connection rndPossibleConnection = possibleConnections[rndPossibleConnectionIndex];

        return rndPossibleConnection.ThisPart.PlacePart(randomConnection, rndPossibleConnection);
    }
    #endregion

    #region public functions
    #endregion

    #region private functions
    #endregion

    #region Canvas function

    #endregion
}
