using Microsoft.Azure.SpatialAnchors;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.WSA;

namespace AzureSpatialAnchors
{
    public class AzureSpatialAnchorService : MonoBehaviour
    {
        [Serializable]
        public class AzureSpatialAnchorServiceProfile
        {
            [SerializeField]
            [Tooltip("The account id from the Azure portal for the Azure Spatial Anchors service")]
            string azureAccountId;
            public string AzureAccountId => this.azureAccountId;

            [SerializeField]
            [Tooltip("The access key from the Azure portal for the Azure Spatial Anchors service (for Key authentication)")]
            string azureServiceKey;
            public string AzureServiceKey => this.azureServiceKey;
        }

        [SerializeField]
        [Tooltip("The configuration for the Azure Spatial Anchors Service")]
        AzureSpatialAnchorServiceProfile profile = new AzureSpatialAnchorServiceProfile();
        public AzureSpatialAnchorServiceProfile Profile => this.profile;

        TaskCompletionSource<CloudSpatialAnchor> taskWaitForAnchorLocation;

        CloudSpatialAnchorSession cloudSpatialAnchorSession;

        public AzureSpatialAnchorService()
        {
        }
        public async Task<string> CreateAnchorOnObjectAsync(GameObject gameObjectForAnchor)
        {
            string anchorId = string.Empty;
            try
            {
                this.StartSession();

                var worldAnchor = gameObjectForAnchor.GetComponent<WorldAnchor>();

                if (worldAnchor == null)
                {
                    throw new ArgumentException("Expected a world anchor on the game object parameter");
                }

                // Note - these next 2 waits are highly dubious as they may never happen so
                // a real world solution would have to do more but I'm trying to be 
                // minimal here
                await new WaitUntil(() => worldAnchor.isLocated);

                // As per previous comment.
                while (true)
                {
                    var status = await this.cloudSpatialAnchorSession.GetSessionStatusAsync();

                    if (status.ReadyForCreateProgress >= 1.0f)
                    {
                        break;
                    }
                    await Task.Delay(250);
                }
                var cloudAnchor = new CloudSpatialAnchor();

                cloudAnchor.LocalAnchor = worldAnchor.GetNativeSpatialAnchorPtr();

                await this.cloudSpatialAnchorSession.CreateAnchorAsync(cloudAnchor);

                anchorId = cloudAnchor?.Identifier;
            }
            catch (Exception ex) // TODO: reasonable exceptions here.
            {
                Debug.Log($"Caught {ex.Message}");
            }
            return (anchorId);
        }
        public async Task<bool> PopulateAnchorOnObjectAsync(string anchorId, GameObject gameObjectForAnchor)
        {
            bool anchorLocated = false;

            try
            {
                this.StartSession();

                this.taskWaitForAnchorLocation = new TaskCompletionSource<CloudSpatialAnchor>();

                var watcher = this.cloudSpatialAnchorSession.CreateWatcher(
                    new AnchorLocateCriteria()
                    {
                        Identifiers = new string[] { anchorId },
                        BypassCache = true,
                        Strategy = LocateStrategy.AnyStrategy,
                        RequestedCategories = AnchorDataCategory.Spatial
                    }
                );

                var cloudAnchor = await this.taskWaitForAnchorLocation.Task;

                anchorLocated = cloudAnchor != null;

                if (anchorLocated)
                {
                    gameObjectForAnchor.GetComponent<WorldAnchor>().SetNativeSpatialAnchorPtr(cloudAnchor.LocalAnchor);
                }
                watcher.Stop();
            }
            catch (Exception ex) // TODO: reasonable exceptions here.
            {
                Debug.Log($"Caught {ex.Message}");
            }
            return (anchorLocated);
        }
        /// <summary>
        /// Start the Azure Spatial Anchor Service session
        /// This must be called before calling create, populate or delete methods.
        /// </summary>
        public void StartSession()
        {
            if (this.cloudSpatialAnchorSession == null)
            {
                Debug.Assert(this.cloudSpatialAnchorSession == null);

                this.ThrowOnBadAuthConfiguration();
                // setup the session
                this.cloudSpatialAnchorSession = new CloudSpatialAnchorSession();
                // set the Azure configuration parameters
                this.cloudSpatialAnchorSession.Configuration.AccountId = this.Profile.AzureAccountId;
                this.cloudSpatialAnchorSession.Configuration.AccountKey = this.Profile.AzureServiceKey;
                // register event handlers
                this.cloudSpatialAnchorSession.Error += this.OnCloudSessionError;
                this.cloudSpatialAnchorSession.AnchorLocated += OnAnchorLocated;
                this.cloudSpatialAnchorSession.LocateAnchorsCompleted += OnLocateAnchorsCompleted;

                // start the session
                this.cloudSpatialAnchorSession.Start();
            }
        }
        /// <summary>
        /// Stop the Azure Spatial Anchor Service session
        /// </summary>
        public void StopSession()
        {
            if (this.cloudSpatialAnchorSession != null)
            {
                // stop session
                this.cloudSpatialAnchorSession.Stop();
                // clear event handlers
                this.cloudSpatialAnchorSession.Error -= this.OnCloudSessionError;
                this.cloudSpatialAnchorSession.AnchorLocated -= OnAnchorLocated;
                this.cloudSpatialAnchorSession.LocateAnchorsCompleted -= OnLocateAnchorsCompleted;
                // cleanup
                this.cloudSpatialAnchorSession.Dispose();
                this.cloudSpatialAnchorSession = null;
            }
        }
        void OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            Debug.Log("On Locate Anchors Completed");
            Debug.Assert(this.taskWaitForAnchorLocation != null);

            if (!this.taskWaitForAnchorLocation.Task.IsCompleted)
            {
                this.taskWaitForAnchorLocation.TrySetResult(null);
            }
        }
        void OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            Debug.Log($"On Anchor Located, status is {args.Status} anchor is {args.Anchor?.Identifier}, pointer is {args.Anchor?.LocalAnchor}");
            Debug.Assert(this.taskWaitForAnchorLocation != null);

            this.taskWaitForAnchorLocation.SetResult(args.Anchor);
        }
        void OnCloudSessionError(object sender, SessionErrorEventArgs args)
        {
            Debug.Log($"On Cloud Session Error: {args.ErrorMessage}");
        }
        void ThrowOnBadAuthConfiguration()
        {
            if (string.IsNullOrEmpty(this.Profile.AzureAccountId) ||
                string.IsNullOrEmpty(this.Profile.AzureServiceKey))
            {
                throw new ArgumentNullException("Missing required configuration to connect to service");
            }
        }
    }
}

/*
 *     public class PhotonScript : MonoBehaviourPunCallbacks
    {
        enum RoomStatus
        {
            None,
            CreatedRoom,
            JoinedRoom
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
            PhotonNetwork.JoinOrCreateRoom(ROOM_NAME, roomOptions, null);
        }
        public async override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            if (this.roomStatus == RoomStatus.None)
            {
                this.roomStatus = RoomStatus.JoinedRoom;
            }
        }
        public async override void OnCreatedRoom()
        {
            base.OnCreatedRoom();

            this.roomStatus = RoomStatus.CreatedRoom;
        }
        static readonly string ROOM_NAME = "HardCodedRoomName";
    }
    public class PhotonScript : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();

            PhotonNetwork.JoinOrCreateRoom("HardCodedRoom", null, null);
        }
        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
        }
        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
        }
    }
    public class PhotonScript : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
        }
    }

    */