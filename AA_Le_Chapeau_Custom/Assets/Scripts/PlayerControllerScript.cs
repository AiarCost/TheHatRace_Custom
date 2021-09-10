using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerControllerScript : MonoBehaviourPunCallbacks, IPunObservable
{
    [HideInInspector]
    public int id;
    

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;

    [HideInInspector]
    public float curHatTime;

    [Header("Components")]
    public Rigidbody rig;
    public Player PhotonPlayer;
    public Material GhostMat;
    public Material[] HatArray;

    // Start is called before the first frame update
    void Start()
    {
        rig = gameObject.GetComponent<Rigidbody>();
        Debug.Log("Hat Arrany Length " + HatArray.Length);
    }

    // Update is called once per frame
    void Update()
    {

        // the host will check if the player has won
        if (PhotonNetwork.IsMasterClient)
        {
            if(curHatTime >= GameManagerScript.instance.timeToWin && !GameManagerScript.instance.gameEnded)
            {
                GameManagerScript.instance.gameEnded = true;
                GameManagerScript.instance.photonView.RPC("WinGame", RpcTarget.All, id);
            }
        }

        if (photonView.IsMine)
        {

            Move();

            if (Input.GetKeyDown(KeyCode.Space))
                TryJump();

            // track the amount of time we're wearing the hat
            if (hatObject.activeInHierarchy)
                curHatTime += Time.deltaTime;
        }

    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal") * moveSpeed;
        float z = Input.GetAxis("Vertical") * moveSpeed;

        rig.velocity = new Vector3(x, rig.velocity.y, z);
    }

    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 0.7f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    //called hwne the player object is instantiated
    [PunRPC]
    public void Initalize(Player player)
    {
        PhotonPlayer = player;
        id = player.ActorNumber;

        GameManagerScript.instance.players[id - 1] = this;

        transform.position = GameManagerScript.instance.spawnPoints[id].position;

        // give the first player the hat
        if (id == 1)
            GameManagerScript.instance.GiveHat(id, true);

        //if this isn't our local player, diable physics as that's controlled by the user and synced to all other clients
        if (!photonView.IsMine)
            rig.isKinematic = true;

    }

    // sets the player's hat active or not
    public void SetHat(bool hasHat)
    {
        hatObject.SetActive(hasHat);

    }

    public void SetHat(bool hasHat, int HatColor)
    {
        hatObject.SetActive(hasHat);
        //hatObject.GetComponent<Renderer>().material.color = HatArray[HatColor].color;
        Debug.Log("Hat Color is at " + HatColor);
        //create a simple array for the renderers that need the color to be changed in
        Component[] ChildrenRenders = hatObject.GetComponentsInChildren<Renderer>();
        if (HatColor >= 0)
        {
            //changes all components to the color when there is multiple objects
            foreach (Renderer mat in ChildrenRenders)
            {
                mat.material.color = HatArray[HatColor].color;
            }
        }
    }


    // When player "dies" will have a ghost form to move around in the game
    public void GhostForm()
    {
        //We change the Mat to look like a ghost
        GetComponent<Renderer>().material = GhostMat;
        //Change the tag to Ghost so that players cannot pass the hat to the ghost
        gameObject.tag = "Ghost";
        //make sure hat is not on ghost anymore
        SetHat(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (photonView.IsMine)
            return;

        //did we hit another player?
        if (collision.gameObject.CompareTag("Player"))
        {
            
            // do they have the hat?
            if(GameManagerScript.instance.GetPlayer(collision.gameObject).id ==
                GameManagerScript.instance.playerWithHat)
            {
                
                //can we get the hat?
                if (GameManagerScript.instance.CanGetHat())
                {
                    Debug.Log("I have been given the hat " + gameObject.name);
                    //give us the hat
                    GameManagerScript.instance.photonView.RPC("GiveHat", RpcTarget.All, id, false);
                }
            }
        }
    }


    public void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(curHatTime);
        }
        else if (stream.IsReading)
        {
            curHatTime = (float)stream.ReceiveNext();
        }
    }

}
