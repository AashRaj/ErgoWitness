﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is the packetbeat moniotr 
/// </summary>
public class PacketbeatMonitor : MonitorObject {

    private Packetbeat_Json_Data _packetbeatJsonData;
    public bool assumeHttp = false;

    public override void StartMonitor()
    {
        // Make sure that the FSM knows we are starting again
        base.StartMonitor();

        // Send it packetbeat data
        _packetbeatJsonData = new Packetbeat_Json_Data();

        // Start the finite satate machine for the web request
        StartCoroutine(FSM(_packetbeatJsonData));
    }

    /// <summary>
    /// Check if this data is of the packetbeat type.
    /// If it is, then check it like we need to for packetbeat
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    public override void CheckRequestData<T>(T data)
    {
        if (typeof(T) == typeof(Packetbeat_Json_Data))
        {
            // Cast the object as necessary
            _packetbeatJsonData = data as Packetbeat_Json_Data;
            CheckData(_packetbeatJsonData);
        }
        else
        {
            return;
        }
    }

    /// <summary>
    /// Loop through the data that we have and send it to the netflow
    /// controller if we should
    /// </summary>
    /// <param name="packetDataObj"></param>
    private void CheckData(Packetbeat_Json_Data packetDataObj)
    {
        // ================= Check and make sure that our data is valid =====================
        // Make sure that our data is not null
        if (packetDataObj.hits.hits.Length == 0)
        {
            _UseLastSuccess = true;

            // Tell this to use the last successful query
            return;
        }

        // Let this know that we no longer need to bank on the last success
        if (_UseLastSuccess)
        {
            _UseLastSuccess = false;
        }

        // ============= Keep track of stuff to prevent duplicates =======================

        // Set our latest packetbeat time to the most recent one
        _latest_time = packetDataObj.hits.hits[packetDataObj.hits.hits.Length - 1]._source.runtime_timestamp;

        // ============== Actually loop through our hits data  =========================
        for (int i = 0; i < packetDataObj.hits.hits.Length; i++)
        {
            // Set the integer IP values of this object
            SetIntegerValues(packetDataObj.hits.hits[i]._source);

            // As long as what we got from those IP's is valid:
            if (packetDataObj.hits.hits[i]._source.destIpInt != -1 && packetDataObj.hits.hits[i]._source.sourceIpInt != -1)
            {
                // Change the protocol to HTTP if we want to, this is optional because
                // sometimes it is techincally incorrect
                if (assumeHttp && packetDataObj.hits.hits[i]._source.dest.port == 80 ||
                   packetDataObj.hits.hits[i]._source.dest.port == 8080)
                {
                    packetDataObj.hits.hits[i]._source.transport = "http";
                }

                // Send the data to the netflow controller
                NetflowController.currentNetflowController.CheckPacketbeatData(packetDataObj.hits.hits[i]._source);
            }
        }
    }

    /// <summary>
    /// This method will append a filter query to 
    /// </summary>
    public override void AddFilter()
    {
        // Add in the filter

        // Do this by appending a string to the query
    }

}