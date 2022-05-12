using System.Collections.Generic;
using UnityEngine;

public class HouseManager : MonoBehaviour
{
    #region Private variables
    List<Room> _rooms = new List<Room>();
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        CreateRooms();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CreateRooms()
    {
        if (_rooms.Count > 0)
        {
            foreach (Room room in _rooms)
            {
                GameObject.Destroy(room.gameObject);
            }
        }
        _rooms = new List<Room>()
        {
            new Room("Hall", 3, new Vector3(0, 0, 0)),
            new Room("LivingRoom", 24, new Vector3(-3.5f, 0,  3.5f)),
            new Room("Kitchen", 16, new Vector3(-9.5f, 0, 5.5f)),
            new Room("Bedroom1", 16, new Vector3(-2.5f, 0, -3.5f)),
            new Room("EnsuiteBathroom", 6, new Vector3(-2, 0, -7.5f)),
            new Room("Bedroom2", 12, new Vector3(-7.5f, 0, -2)),
            new Room("Storage", 3, new Vector3(2, 0, -2)),
            new Room("Bathroom", 4, new Vector3(3.5f, 0, 0.5f))
        };
    }

    public void GetRoomBounds()
    {
        
    }

}
