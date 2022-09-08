using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DisplayUserID : MonoBehaviour
{
    public Text userId;
    [HideInInspector] public PhotonView pv = null;

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        userId.text = pv.Owner.NickName;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
