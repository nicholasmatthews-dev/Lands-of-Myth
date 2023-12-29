using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using LOM.Multiplayer;

namespace LOM.Levels;

public class LevelHostServer : ILevelHost, IENetPacketListener
{
    private static bool Debugging = false;
    private ENetClient eNetClient;
    //private ConcurrentDictionary<LevelCellRequest, (EventWaitHandle, LevelCellRequest)> requestWaitHandles = new();
    private ConcurrentDictionary<LevelCellRequest, (TaskCompletionSource<bool>, LevelCellRequest)> requestWaitHandles = new();
    private ConcurrentDictionary<LevelCellRequest, Task<LevelCellRequest>> activeTasks = new();
    private ConcurrentQueue<LevelCellRequest> incomingRequests = new();
    private object incomingPacketWaitLock = new();
    private TaskCompletionSource<bool> incomingPacketWait = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task packetDistributor;
    private bool keepAlive = true;
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;

    public LevelHostServer(ENetClient eNetClient){
        this.eNetClient = eNetClient;
        eNetClient.AddPacketListener(communicationChannel, this);
        //packetDistributor = Task.Run(DistributePacket);
    }

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

    private void HandlePacket(byte[] packet){
        LevelCellRequest request = LevelCellRequest.Deserialize(packet);
        if (Debugging) Debug.Print(GetType() + ": Received message from server " + request);
        /*incomingRequests.Enqueue(request);
        try {
            lock (incomingPacketWaitLock){
                incomingPacketWait.SetResult(true);
            }
            if (Debugging) Debug.Print(GetType() + ": Successfully set incomingPacketWait.");
        }
        catch (Exception e){
            if (Debugging) Debug.Print(GetType() + ": Failed to set incomingPacketWait. Request is " + request);
            if (Debugging) Debug.Print(GetType() + ": Error from setting incoming packet wait is \"" + e.Message + "\"");
        }*/
        ReleaseWait(request);
    }
    
    private async Task DistributePacket(){
        while (keepAlive) {
            await incomingPacketWait.Task;
            if (incomingRequests.TryDequeue(out LevelCellRequest currentRequest)){
                if (Debugging) Debug.Print(GetType() + ": Releasing wait on " + currentRequest);
                ReleaseWait(currentRequest);
            }
            if (Debugging) Debug.Print(GetType() + ": Resetting incomingPacketWait to new instance.");
            lock (incomingPacketWaitLock){
                TaskCompletionSource<bool> oldWait = incomingPacketWait;
                incomingPacketWait = new();
                if (incomingPacketWait.Equals(oldWait)){
                    if (Debugging) Debug.Print(GetType() + ": Error, incoming packet wait was not set to a distinct instance.");
                }
            }
        }
    }

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