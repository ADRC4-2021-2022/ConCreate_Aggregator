using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aggregator : MonoBehaviour
{
    #region public fields

    #endregion

    #region private fields
    //Library contains all the parts that haven't bee place yet
    public List <Part> _library = new List <Part> ();

    //All the parts that are allready place in the building
    public List<Part> _buildingParts = new List <Part> ();

    //All the connection that are available in your building
    //Regenerate this list every time you place a part
    public List<Connection> _connections = new List <Connection> ();

    #endregion
    #region Monobehaviour functions
    // Start is called before the first frame update
    void Start()
    {
        //Gather all the prefabs for the parts and load the connections

        //Place a random part in a random position
    }

    
    #endregion

    #region public functions
    public void SetNextPart()
    {
        //Get the list of available connection in your building

        //Select a random connection

        //find all the parts that have a fitting connection

        //Select a random part out of the fitting list

        //try to place it

        //if success => hooray

        //if failure => remove part from list and try another one

        //if none of the parts work, disable connection and try next connection
    }

    #endregion

    #region private functions

    #endregion

    #region Canvas function

    #endregion
}
