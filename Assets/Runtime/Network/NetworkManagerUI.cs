using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button _server;
    [SerializeField] private Button _host;
    [SerializeField] private Button _client;

    private void Start()
    {
        _server.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            HideButtons();
        });
        _host.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            HideButtons();
        });
        _client.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            HideButtons();
        });
    }

    private void HideButtons()
    {
        _server.gameObject.SetActive(false);
        _host.gameObject.SetActive(false);
        _client.gameObject.SetActive(false);
    }
}
