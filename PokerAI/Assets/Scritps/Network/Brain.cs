using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(fileName = "Brain", menuName = "Brain/New Brain", order = 1)]
public class Brain : ScriptableObject {
    [Header("Image in editor")]
    [SerializeField] public Texture2D previewImage;

    [Header("New brain settings")]
    [SerializeField] private bool generateNewNetwork;
    [SerializeField] private int[] neuronsInLayers = { 14, 8, 8, 3 };   //standard Layout

    [Header("Do not touch, data is stored from network here")]
    [SerializeField] private float[] networkGenom;
    [SerializeField] private int[] networkStructure;

    public void StoreNetowrk(Network network) {
        //store the structure of the network for reconstruction later
        networkStructure = new int[network.networkStructure.Length];
        for(int i = 0; i < networkStructure.Length; i++) {
            networkStructure[i] = network.networkStructure[i];
        }

        //deep copy genom
        float[] genom = network.GetNetworkGenom();
        networkGenom = new float[genom.Length];
        for(int i = 0; i < networkGenom.Length; i++) {
            networkGenom[i] = genom[i];
        }
    }

    public Network GetNetwork() {
        if(generateNewNetwork) 
            return new Network(neuronsInLayers);

        Network net = new Network(networkStructure);
        net.SetNetworkByGenom(networkGenom);
        return net;
    }
}
