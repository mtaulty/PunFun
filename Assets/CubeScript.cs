using ExitGames.Client.Photon;
using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.Text;
using UnityEngine;

public class CubeScript : MonoBehaviour, IPunInstantiateMagicCallback, IMixedRealityFocusHandler, IPunOwnershipCallbacks
{
    string ViewIDAsString => this.GetComponent<PhotonView>().ViewID.ToString();

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        var parent = GameObject.Find("Root");
        this.transform.SetParent(parent.transform, true);

        // Do we have a stored transform for this cube within the room?
        if (PhotonNetwork.CurrentRoom.CustomProperties.Keys.Contains(this.ViewIDAsString))
        {
            var transformStringValue = PhotonNetwork.CurrentRoom.CustomProperties[this.ViewIDAsString] as string;

            if (!string.IsNullOrEmpty(transformStringValue))
            {
                StringToLocalTransform(this.transform, transformStringValue);
            }
        }
    }
    public void OnFocusEnter(FocusEventData eventData)
    {
        // ask the photonview for permission
        var photonView = this.GetComponent<PhotonView>();
        photonView?.RequestOwnership();
    }
    public void OnFocusExit(FocusEventData eventData)
    {
    }
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        targetView.TransferOwnership(requestingPlayer);
    }
    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
    }
    public void OnManipulationEnded()
    {
        var photonView = this.GetComponent<PhotonView>();

        if (photonView != null)
        {
            var transformStringValue = LocalTransformToString(this.transform);

            this.SetViewIdCustomRoomProperty(transformStringValue);
        }
    }
    void SetViewIdCustomRoomProperty(string value)
    {
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable()
            {
                    {  this.ViewIDAsString, value }
            }
        );
    }
    public void OnRemove()
    {
        this.SetViewIdCustomRoomProperty(null);
        PhotonNetwork.Destroy(this.gameObject);
    }
    static string LocalTransformToString(Transform transform)
    {
        var builder = new StringBuilder();

        foreach (var value in new float[] {
            transform.localPosition.x,
            transform.localPosition.y,
            transform.localPosition.z,
            transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z,
            transform.localRotation.x,
            transform.localRotation.y,
            transform.localRotation.z,
            transform.localRotation.w
        })
        {
            builder.Append($"{value}{SEPARATOR}");
        }
        return (builder.ToString());
    }
    static void StringToLocalTransform(Transform transform, string propertyValue)
    {
        var pieces = propertyValue.Split(SEPARATOR);
        var pieceCount = 0;

        var localPosition = new Vector3(
            float.Parse(pieces[pieceCount++]), 
            float.Parse(pieces[pieceCount++]), 
            float.Parse(pieces[pieceCount++]));

        var localScale = new Vector3(
            float.Parse(pieces[pieceCount++]), 
            float.Parse(pieces[pieceCount++]), 
            float.Parse(pieces[pieceCount++]));

        var localRotation = new Quaternion(
            float.Parse(pieces[pieceCount++]), 
            float.Parse(pieces[pieceCount++]), 
            float.Parse(pieces[pieceCount++]), 
            float.Parse(pieces[pieceCount++]));

        transform.localScale = localScale;
        transform.localRotation = localRotation;
        transform.localPosition = localPosition;
    }
    static readonly char SEPARATOR = '|';
}