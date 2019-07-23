namespace Photon.Pun
{
    using UnityEngine;

    [RequireComponent(typeof(PhotonView))]
    public class PhotonRelativeTransformView : MonoBehaviour, IPunObservable
    {
        [SerializeField]
        string relativeTransformGameObjectName;

        GameObject relativeGameObject;


        Vector3 RelativePosition
        {
            get
            {
                return (this.gameObject.transform.position - this.RelativeGameObject.transform.position);
            }
            set
            {
                this.gameObject.transform.position = this.RelativeGameObject.transform.position + value;
            }
        }
        Quaternion RelativeRotation
        {
            get
            {
                return (Quaternion.Inverse(this.RelativeGameObject.transform.rotation) * this.transform.rotation);
            }
            set
            {
                this.gameObject.transform.rotation = this.RelativeGameObject.transform.rotation;
                this.gameObject.transform.rotation *= value;
            }
        }

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

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(this.RelativePosition);
                stream.SendNext(this.RelativeRotation);
            }
            else
            {
                this.RelativePosition = (Vector3)stream.ReceiveNext();
                this.RelativeRotation = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}