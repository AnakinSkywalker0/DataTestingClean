using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Receives BPM integers from polar_hr_bridge.py over UDP and fires OnHRReceived on the main thread.
public class PolarHRReceiver : MonoBehaviour
{
    public static PolarHRReceiver Instance { get; private set; }
    public static event Action<int> OnHRReceived;

    public int CurrentHR { get; private set; }

    private const int Port = 12345;
    private UdpClient _udp;
    private Thread _thread;
    private readonly Queue<int> _queue = new Queue<int>();
    private readonly object _lock = new object();
    private volatile bool _running;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _udp = new UdpClient(Port);
        _running = true;
        _thread = new Thread(ReceiveLoop) { IsBackground = true };
        _thread.Start();
        Debug.Log($"[PolarHRReceiver] Listening on UDP port {Port}");
    }

    private void ReceiveLoop()
    {
        var ep = new IPEndPoint(IPAddress.Any, Port);
        while (_running)
        {
            try
            {
                byte[] data = _udp.Receive(ref ep);
                string raw = Encoding.UTF8.GetString(data).Trim();
                if (int.TryParse(raw, out int bpm))
                {
                    lock (_lock) { _queue.Enqueue(bpm); }
                }
            }
            catch (SocketException) { /* socket closed on quit */ }
        }
    }

    private void Update()
    {
        lock (_lock)
        {
            while (_queue.Count > 0)
            {
                int bpm = _queue.Dequeue();
                CurrentHR = bpm;
                OnHRReceived?.Invoke(bpm);
            }
        }
    }

    private void OnDestroy()
    {
        _running = false;
        _udp?.Close();
        _thread?.Join(500);
    }
}
