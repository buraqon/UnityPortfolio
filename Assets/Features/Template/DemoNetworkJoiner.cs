using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class DemoNetworkJoiner : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport transport;
    
    [SerializeField] private Transform joinPanel;
    [SerializeField] private bool singlePlayer;
    
    [Header("Buttons")]
    [SerializeField] private Button joinButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    
    [SerializeField] private string ipToJoin = "127.0.0.1";
    [SerializeField] private ushort portToJoin = 7777;

    private void Start()
    {
        if (singlePlayer)
            HostGame();
        else
        {
            joinButton.onClick.AddListener(JoinGame);
            hostButton.onClick.AddListener(HostGame);
        }
        
        ipInputField.text = ipToJoin;
        portInputField.text = portToJoin.ToString();
        
        ipInputField.onValueChanged.AddListener((string newIP) =>
        {
            ipToJoin = newIP;
            ipInputField.text = ipToJoin;
        });
        portInputField.onValueChanged.AddListener((string newPort) =>
        {
            if (ushort.TryParse(newPort, out ushort parsedPort))
            {
                portToJoin = parsedPort;
            }
            
            portInputField.text = portToJoin.ToString();
        });
    }

    private void HostGame()
    {
        networkManager.StartHost();
        joinPanel.gameObject.SetActive(false);
    }
    
    private void JoinGame()
    {
        transport.ConnectionData.Address = ipToJoin;
        transport.ConnectionData.Port = portToJoin;
        networkManager.StartClient();
        joinPanel.gameObject.SetActive(false);
    }
}