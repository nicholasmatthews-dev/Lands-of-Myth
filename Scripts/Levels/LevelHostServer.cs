using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using LOM.Multiplayer;

namespace LOM.Levels;

/// <summary>
/// Represents the implementation of an <see cref="ILevelHost"/> which the server's game model uses.  
/// </summary>
public class LevelHostServer : ILevelHost, IENetPacketListener
{
    private static bool Debugging = false;
    /// <summary>
    /// The <see cref="ENetClient"/> instance that is used to handle networking. 
    /// </summary>
    private ENetClient eNetClient;
    /// <summary>
    /// A dictionary representing a collection of <see cref="TaskCompletionSource"/>s which indicate
    /// whether or not a given <see cref="LevelCellRequest"/> has been fulfilled by the server. 
    /// </summary>
    private ConcurrentDictionary<LevelCellRequest, (TaskCompletionSource<bool>, LevelCellRequest)> requestWaitHandles = new();
    /// <summary>
    /// A dictionary associating <see cref="LevelCellRequest"/>s with their associated <see cref="Task"/>s
    /// which are awaiting a response from the server.
    /// </summary>
    private ConcurrentDictionary<LevelCellRequest, Task<LevelCellRequest>> activeTasks = new();
    /// <summary>
    /// The channel which this object will communicate on.
    /// </summary>
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;

    public LevelHostServer(ENetClient eNetClient){
        this.eNetClient = eNetClient;
        eNetClient.AddPacketListener(communicationChannel, this);
    }

    /// <summary>
    /// Creates a <see cref="Task"/> to retrieve a <see cref="LevelCell"/> from the server.  
    /// </summary>
    /// <param name="request">The <see cref="LevelCellRequest"/> for the cell to retrieve.</param>
    /// <returns>A <see cref="Task"/> which will return a fulfilled 
    /// <see cref="LevelCellRequest"/></returns>
    private Task<LevelCellRequest> CreateRetrievalTask(LevelCellRequest request){
        if (activeTasks.TryGetValue(request, out Task<LevelCellRequest> output)){
            return output;
        }
        Task<LevelCellRequest> newTask = Task.Run(async () => {
            if (Debugging) Debug.Print(GetType() + ": Creating task for request " + request);
            await MessageServerAndWait(request);
            LevelCellRequest returnedRequest = requestWaitHandles[request].Item2;
            if (Debugging) Debug.Print(GetType() + ": Returned result is " + returnedRequest);
            requestWaitHandles.TryRemove(request, out _);
            return returnedRequest;
        });
        if(!activeTasks.TryAdd(request, newTask)){
            if (Debugging) Debug.Print(GetType() + ": Request task not added for " + request);
        }
        return newTask;
    }

    /// <summary>
    /// Sends a message containing the <see cref="LevelCellRequest"/> to the server and then
    /// asynchronously waits until its created <see cref="TaskCompletionSource"/> is set as
    /// completed.
    /// </summary>
    /// <param name="request">The request to be sent out.</param>
    /// <returns>Returns when a response has been received from the server.</returns>
    private async Task MessageServerAndWait(LevelCellRequest request){
        TaskCompletionSource<bool> waitHandle;
        if (!requestWaitHandles.ContainsKey(request)){
            waitHandle = new(TaskCreationOptions.RunContinuationsAsynchronously);
            requestWaitHandles.TryAdd(request, (waitHandle, null));
            if (Debugging) Debug.Print(GetType() + ": Sending message to server.");
            eNetClient.SendMessage(communicationChannel, request.Serialize());
        }
        else {
            waitHandle = requestWaitHandles[request].Item1;
            if (Debugging) Debug.Print(GetType() + ": Waithandle already found " + waitHandle);
        }
        await waitHandle.Task;
        if (Debugging) Debug.Print(GetType() + ": MessageServerAndWait returning from await for " + request);
        return;
    }

    /// <summary>
    /// Parses a received packet into a <see cref="LevelCellRequest"/> and then releases the 
    /// appropriate wait to signal a message has been received.
    /// </summary>
    /// <param name="packet">The packet to parse.</param>
    private void HandlePacket(byte[] packet){
        LevelCellRequest request = LevelCellRequest.Deserialize(packet);
        if (Debugging) Debug.Print(GetType() + ": Received message from server " + request);
        ReleaseWait(request);
    }

    /// <summary>
    /// Releases the wait for a given <see cref="LevelCellRequest"/> by completing the underlying
    /// <see cref="TaskCompletionSource"/>. 
    /// </summary>
    /// <param name="request"></param>
    private void ReleaseWait(LevelCellRequest request){
        if (requestWaitHandles.TryGetValue(request, out (TaskCompletionSource<bool>, LevelCellRequest) oldTuple)){
            if (requestWaitHandles.TryUpdate(request, (oldTuple.Item1, request), oldTuple)){
                if (requestWaitHandles.TryGetValue(request,out (TaskCompletionSource<bool>, LevelCellRequest) newTuple)){
                    try {
                        newTuple.Item1.SetResult(true);
                        if (Debugging) Debug.Print(GetType() + ": Successfully set " + newTuple.Item1);
                    }
                    catch (Exception e){
                        if (Debugging) Debug.Print(GetType() + ": Failed to set " + newTuple.Item1);
                        if (Debugging) Debug.Print(GetType() + ": Error is \"" + e.Message + "\"");
                    }
                }
            }
            else {
                if (Debugging) Debug.Print(GetType() + ": Packet update failed for " + request);
            }
        }
        else {
            if (Debugging) Debug.Print(GetType() + ": Entry retrieval failed for " + request);
        }
    }

    public void ConnectManager(ILevelManager levelManager)
    {
    }

    public void DisconnectManager(ILevelManager levelManager)
    {
    }

    public Task<LevelCellRequest> GetLevelCell(LevelCellRequest request)
    {
        return CreateRetrievalTask(request);
    }

    public void ReceivePacket(byte[] packet, ENetPacketPeer peer)
    {
        HandlePacket(packet);
    }

    public void SignalDispose(ILevelManager levelManager, LevelCellRequest request)
    {
        throw new System.NotImplementedException();
    }
}