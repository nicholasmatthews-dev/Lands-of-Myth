using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using LOM.Multiplayer;

namespace LOM.Levels;

public class LevelHostServer : ILevelHost, IENetPacketListener
{
    private static bool Debugging = true;
    private ENetClient eNetClient;
    private ConcurrentDictionary<LevelCellRequest, (EventWaitHandle, LevelCellRequest)> requestWaitHandles = new();
    private ConcurrentDictionary<LevelCellRequest, Task<LevelCellRequest>> activeTasks = new();
    private int communicationChannel = (int)ENetCommon.ChannelNames.Spaces;

    public LevelHostServer(ENetClient eNetClient){
        this.eNetClient = eNetClient;
        eNetClient.AddPacketListener(communicationChannel, this);
    }

    private Task<LevelCellRequest> CreateRetrievalTask(LevelCellRequest request){
        if (activeTasks.TryGetValue(request, out Task<LevelCellRequest> output)){
            return output;
        }
        Task<LevelCellRequest> newTask = Task.Run(() => {
            if (Debugging) Debug.Print("LevelHostClient: Creating task for request " + request);
            MessageServerAndWait(request);
            LevelCellRequest returnedRequest = requestWaitHandles[request].Item2;
            if (Debugging) Debug.Print("LevelHostClient: Returned result is " + returnedRequest);
            requestWaitHandles.TryRemove(request, out _);
            return returnedRequest;
        });
        activeTasks.TryAdd(request, newTask);
        return newTask;
    }

    private void MessageServerAndWait(LevelCellRequest request){
        EventWaitHandle waitHandle;
        if (!requestWaitHandles.ContainsKey(request)){
            waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            requestWaitHandles.TryAdd(request, (waitHandle, null));
            if (Debugging) Debug.Print("LevelHostClient: Sending message to server.");
            eNetClient.SendMessage(communicationChannel, request.Serialize());
        }
        else {
            waitHandle = requestWaitHandles[request].Item1;
        }
        waitHandle.WaitOne();
    }

    private void HandlePacket(byte[] packet){
        LevelCellRequest request = LevelCellRequest.Deserialize(packet);
        if (Debugging) Debug.Print("LevelHostClient: Received message from server " + request);
        if (requestWaitHandles.TryGetValue(request, out (EventWaitHandle, LevelCellRequest) oldTuple)){
            if (requestWaitHandles.TryUpdate(request, (oldTuple.Item1, request), oldTuple)){
                oldTuple.Item1.Set();
            }
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