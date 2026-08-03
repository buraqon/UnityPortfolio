using System;
using System.Collections.Generic;
using UnityEngine;

public class PredictedTransform_Debug : MonoBehaviour
{
    [SerializeField] private PredictedTransform predictedTransform;
    [SerializeField] private LineRenderer serverLineRenderer;
    [SerializeField] private LineRenderer clientLineRenderer;
    [SerializeField] private GameObject playerSilhouette;
    [SerializeField] private int TrackingCount = 120;
    [SerializeField] private bool isDebug = false;
    
    private List<Vector3> serverPositions = new();
    private List<Vector3> clientPositions = new();

    private void Start()
    {
        transform.parent = null;
        if (!isDebug)
        {
            Destroy(transform.gameObject);
        }
    }

    private void Update()
    {
        // create a cylinder to follow the player
        var serverPosition = predictedTransform.ServerPosition;
        var serverRotation = predictedTransform.ServerRotation;
        
        serverPositions.Add(serverPosition);
        clientPositions.Add(predictedTransform.transform.position);
        
        if (serverPositions.Count > TrackingCount)
        {
            serverPositions.RemoveAt(0);
            clientPositions.RemoveAt(0);
        }
        
        playerSilhouette.transform.position = serverPosition;
        playerSilhouette.transform.rotation = serverRotation;
        
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        serverLineRenderer.positionCount = serverPositions.Count;
        clientLineRenderer.positionCount = clientPositions.Count;
        
        for (int i = 0; i < serverPositions.Count; i++)
        {
            serverLineRenderer.SetPosition(i, serverPositions[i]);
            clientLineRenderer.SetPosition(i, clientPositions[i]);
        }
    }
}
