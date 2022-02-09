using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class Aggregator : MonoBehaviour
{
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

    #endregion
    #region Monobehaviour functions
    // Start is called before the first frame update
    void Start()
    {
        //Gather all the prefabs for the parts and load the connections
        _library.Add(new Part(Resources.Load("Prefab/01P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefab/02P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefab/03P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefab/04P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefab/05P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefab/06P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefab/07P") as GameObject));
        _library.Add(new Part(Resources.Load("Prefab/08P") as GameObject));

        foreach (var part in _library)
        {
            foreach (var connection in part.Connections)
            {
                _connections.Add(connection);
            }
        }
    }

    //Place a random part in a random position
    //GO.transform.randomPosition etc
    private Voxelgrid _grid;
    bool TryRandomPart()
    {
        int x = Random.Range(1, 50);
        int y = Random.Range(1, 50);
        int z = Random.Range(1, 50);
        Vector3 position = new Vector3(x, y, z);

        return partToPlace.transform.position = position;

    }
#endregion

#region public functions
public void SetNextPart()
{
        //Get the list of available connection in your building
        List<Connection> connections = new List<Connection>();
        List<Connection> Connection = new List<Connection> (connections.Where(c => c.Available).ToList());
        //Part partToPlace = _CONNECTIONS.SHUFFLE.FIRST
        //Select a random connection
        List<Connection> partToPlace = connections.OrderBy(x => Random.value).ToList();
        //ADD SHUFFLE IN UTIL (ADD UTIL SCRIPT)
        //find all the parts that have a fitting connection
        List<Part> parts = new List<Part>();
        List<Part> partsFittingConnection = new List<Part>(parts.Where(c => c.Fitting).ToList());
        //FOREACH PART IN _LIBRARY --> FOREACH CONNECTION IN PART.CONNECTIONS
        //Select a random part out of the fitting list
        //IF CONNECTIONS.NORMAL && % <CONNECTIONS.LENGTH< %
        public Range range1(int n voxels, int n voxels -3);
        public Range range2(int n voxels, int n voxels +3);

        foreach (var part in parts)
        {
            foreach (var connection in connections)
            {
                List<Part> randomParts = parts.OrderBy(x => Random.value).ToList();
                randomParts.First();
                if (connection.Normal && range1 < connection.Length < range2)
                {
                    partToPlace.PlacePart();
                    Coroutine();
                }
                else
                {
                    if (//the part is not connecting with the previous one -->remove it from the list AND go to the next part)
                    {
                        parts.Remove(partToPlace);
                        .....
                    }
                    else if (//none of the parts are connecting -->disable connection AND try with the next available connection)
                    {
                        connection.Available = false;
                        .....
                    }
                }
                Coroutine();
            }
        }
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
