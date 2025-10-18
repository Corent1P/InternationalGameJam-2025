using UnityEngine;
using System.Collections.Generic;

public class RoomData : MonoBehaviour
{
    public List<RoomDoor> Doors { get; private set; } = new List<RoomDoor>();

    [Header("Room Info")]
    [SerializeField] private int maxNumberOfAppearances = 1;
    [SerializeField] private string roomName = "";

    public int MaxNumberOfAppearances => maxNumberOfAppearances;
    public string RoomName => roomName;

    private void Awake()
    {
        Doors.Clear();
        Doors.AddRange(GetComponentsInChildren<RoomDoor>());
    }
}
