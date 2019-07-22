namespace Photon.Pun
{
    using UnityEngine;
    [AddComponentMenu("Photon Networking/Photon Transform View")]
    [HelpURL("https://doc.photonengine.com/en-us/pun/v2/gameplay/synchronization-and-state")]
    [RequireComponent(typeof(PhotonView))]
    public class PhotonRelativeTransformView : MonoBehaviour, IPunObservable
    {
        [SerializeField]
        private string relativeTransformGameObjectName;

        private PhotonView m_PhotonView;

        public bool m_SynchronizePosition = true;
        public bool m_SynchronizeRotation = true;

        Vector3 RelativePosition => this.gameObject.transform.position - this.RelativeGameObject.transform.position;

        Quaternion RelativeRotation =>
            Quaternion.Inverse(this.RelativeGameObject.transform.rotation) * this.transform.rotation;

        GameObject RelativeGameObject
        {
            get
            {
                if (this.relativeGameObject == null)
                {
                    this.relativeGameObject = GameObject.Find(this.relativeTransformGameObjectName);
                }
                return (this.relativeGameObject);
            }
        }
        GameObject relativeGameObject;
        public void Awake()
        {
            m_PhotonView = GetComponent<PhotonView>();
        }
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                if (this.m_SynchronizePosition)
                {
                    stream.SendNext(this.RelativePosition);
                }

                if (this.m_SynchronizeRotation)
                {
                    stream.SendNext(this.RelativeRotation);
                }
            }
            else
            {
                if (this.m_SynchronizePosition)
                {
                    this.gameObject.transform.position = this.RelativeGameObject.transform.position + (Vector3)stream.ReceiveNext();
                }
                if (this.m_SynchronizeRotation)
                {
                    this.gameObject.transform.rotation = (Quaternion)stream.ReceiveNext();
                }
            }
        }
    }
}