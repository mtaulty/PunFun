using System;
using System.Linq;
using System.Threading.Tasks;
using AzureSpatialAnchors;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonScript : MonoBehaviourPunCallbacks
{
    [SerializeField]
    GameObject cubePrefab;

    [SerializeField]
    GameObject statusSphere;

    [SerializeField]
    Material createdAnchorMaterial;

    [SerializeField]
    Material locatedAnchorMaterial;

    [SerializeField]
    Material failedMaterial;

    enum RoomStatus
    {
        None,
        CreatedRoom,
        JoinedRoom,
        JoinedRoomDownloadedAnchor
    }

    public int emptyRoomTimeToLiveSeconds = 120;

    RoomStatus roomStatus = RoomStatus.None;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        var roomOptions = new RoomOptions();
        roomOptions.EmptyRoomTtl = this.emptyRoomTimeToLiveSeconds * 1000;
        roomOptions.CleanupCacheOnLeave = false;
        roomOptions.BroadcastPropsChangeToAll = true;
        PhotonNetwork.JoinOrCreateRoom(ROOM_NAME, roomOptions, null);
    }
    public async override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        // Note that the creator of the room also joins the room...
        if (this.roomStatus == RoomStatus.None)
        {
            this.roomStatus = RoomStatus.JoinedRoom;
        }
        await this.PopulateAnchorAsync();
    }
    public async override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        this.roomStatus = RoomStatus.CreatedRoom;
        await this.CreateAnchorAsync();
    }
    async Task CreateAnchorAsync()
    {
#if !UNITY_EDITOR
        // If we created the room then we will attempt to create an anchor for the parent
        // of the cubes that we are creating.
        var anchorService = this.GetComponent<AzureSpatialAnchorService>();

        var anchorId = await anchorService.CreateAnchorOnObjectAsync(this.gameObject);

        // Put this ID into a custom property so that other devices joining the
        // room can get hold of it.
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable()
            {
                { ANCHOR_ID_CUSTOM_PROPERTY, anchorId }
            }
        );

        this.statusSphere.GetComponent<Renderer>().material = 
            string.IsNullOrEmpty(anchorId) ? this.failedMaterial : this.createdAnchorMaterial;

#endif
    }
    async Task PopulateAnchorAsync()
    {
#if !UNITY_EDITOR
        if (this.roomStatus == RoomStatus.JoinedRoom)
        {
            object keyValue = null;

            // First time around, this property may not be here so we see if is there.
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                ANCHOR_ID_CUSTOM_PROPERTY, out keyValue))
            {
                // If the anchorId property is present then we will try and get the
                // anchor but only once so change the status.
                this.roomStatus = RoomStatus.JoinedRoomDownloadedAnchor;

                // If we didn't create the room then we want to try and get the anchor
                // from the cloud and apply it.
                var anchorService = this.GetComponent<AzureSpatialAnchorService>();

                var located = await anchorService.PopulateAnchorOnObjectAsync(
                    (string)keyValue, this.gameObject);

                this.statusSphere.GetComponent<Renderer>().material =
                    located ? this.locatedAnchorMaterial : this.failedMaterial;
            }
        }
#endif
    }
    public async override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if (propertiesThatChanged.Keys.Contains(ANCHOR_ID_CUSTOM_PROPERTY))
        {
            await this.PopulateAnchorAsync();
        }
    }
    public void OnCreateCube()
    {
        // Position it down the gaze vector
        var position = Camera.main.transform.position + Camera.main.transform.forward.normalized * 1.2f;

        // Create the cube
        var cube = PhotonNetwork.InstantiateSceneObject(this.cubePrefab.name, position, Quaternion.identity);
    }
    static readonly string ANCHOR_ID_CUSTOM_PROPERTY = "anchorId";
    static readonly string ROOM_NAME = "HardCodedRoomName";
}

//using AzureSpatialAnchors;
//using ExitGames.Client.Photon;
//using Photon.Pun;
//using Photon.Realtime;
//using System.Threading.Tasks;
//using UnityEngine;

//public class PhotonScript : MonoBehaviourPunCallbacks
//{
//    enum RoomStatus
//    {
//        None,
//        CreatedRoom,
//        JoinedRoom,
//        JoinedRoomPopulatedAnchor
//    }

//    public GameObject userPrefab;
//    public GameObject cubePrefab;
//    public GameObject cubeParent;
//    public int emptyRoomTimeToLiveSeconds = 120;

//    RoomStatus roomStatus;
//    GameObject userInstance;

//    void Start()
//    {
//        PhotonNetwork.ConnectUsingSettings();
//    }
//    public override void OnConnectedToMaster()
//    {
//        base.OnConnectedToMaster();

//        var roomOptions = new RoomOptions();
//        roomOptions.EmptyRoomTtl = this.emptyRoomTimeToLiveSeconds * 1000;
//        PhotonNetwork.JoinOrCreateRoom(ROOM_NAME, roomOptions, null);
//    }
//    public async override void OnJoinedRoom()
//    {
//        base.OnJoinedRoom();

//        this.userInstance = PhotonNetwork.Instantiate(this.userPrefab.name, Vector3.zero, Quaternion.identity);
//        this.userInstance.transform.parent = Camera.main.transform;

//#if !UNITY_EDITOR
//        if (this.roomStatus == RoomStatus.None)
//        {
//            this.roomStatus = RoomStatus.JoinedRoom;

//            await this.PopulateAnchorAsync();
//        }
//#endif 
//    }
//    public async override void OnCreatedRoom()
//    {
//        base.OnCreatedRoom();

//        this.roomStatus = RoomStatus.CreatedRoom;

//#if !UNITY_EDITOR
//        // If we created the room then we will attempt to create an anchor for the parent
//        // of the cubes that we are creating.
//        var anchorService = this.GetComponent<AzureSpatialAnchorService>();

//        var anchorId = await anchorService.CreateAnchorOnObjectAsync(this.cubeParent);

//        // Put this ID into a custom property so that other devices joining the
//        // room can get hold of it.
//        PhotonNetwork.CurrentRoom.SetCustomProperties(
//            new Hashtable()
//            {
//                { ANCHOR_ID_CUSTOM_PROPERTY, anchorId }
//            }
//        );
//#endif
//    }
//    public void OnCreateCube()
//    {
//        // Position it down the gaze vector
//        var position = Camera.main.transform.position + Camera.main.transform.forward.normalized * 1.2f;

//        // Create the cube
//        var cube = PhotonNetwork.InstantiateSceneObject(this.cubePrefab.name, position, Quaternion.identity);
//    }
//    public async override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
//    {
//        base.OnRoomPropertiesUpdate(propertiesThatChanged);

//#if !UNITY_EDITOR
//        await this.PopulateAnchorAsync();
//#endif 
//    }
//    async Task PopulateAnchorAsync()
//    {
//        if (this.roomStatus == RoomStatus.JoinedRoom)
//        {
//            object keyValue = null;

//            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
//                ANCHOR_ID_CUSTOM_PROPERTY, out keyValue))
//            {
//                // We are only going to try this once so set the status.
//                this.roomStatus = RoomStatus.JoinedRoomPopulatedAnchor;

//                // If we didn't create the room then we want to try and get the anchor
//                // from the cloud and apply it.
//                var anchorService = this.GetComponent<AzureSpatialAnchorService>();

//                await anchorService.PopulateAnchorOnObjectAsync(
//                    (string)keyValue, this.cubeParent);
//            }
//        }
//    }
//    static readonly string ANCHOR_ID_CUSTOM_PROPERTY = "anchorId";
//    static readonly string ROOM_NAME = "HardCodedRoomName";
//}