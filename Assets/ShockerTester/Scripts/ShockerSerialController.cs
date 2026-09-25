using UnityEngine;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using System;


public class ShockerSerialController : MonoBehaviour
{
    [Header("COM Settings")]
    [SerializeField] private string portName = "COM13";
    [SerializeField] private int baudRate = 115200;
    
    private SerialPort _port;
    private Thread _serialThread;
    private bool _running;
    
    private ConcurrentQueue<string> _commandQueue;

    private void Awake()
    {
        _commandQueue = new ConcurrentQueue<string>();
    }

    public void CheckConnection(int shockerId)
    {
        EnqueueCommand($"1;{shockerId}");
    }
    public void EnableShocker(int shockerId)
    {
        EnqueueCommand($"2;{shockerId}");
    }

    public void DisableShocker(int shockerId)
    {
        EnqueueCommand($"3;{shockerId}");
    }

    public void Shock(int shockerId, int power)
    {
        EnqueueCommand($"4;{shockerId};{power}");
    }

    private void EnqueueCommand(string command)
    {
        _commandQueue.Enqueue(command);
        Debug.Log($"Команда добавлена в очередь: {command}");
    }
    private void OnEnable()
    {
        StartSerialThread();
    }

    private void OnDisable()
    {
        StopSerialThread();
    }

    private void StartSerialThread()
    {
        if(_running)
            return;
        _running = true;

        _serialThread = new Thread(SerialLoop);
        _serialThread.IsBackground = true;
        _serialThread.Start();
    }

    private void SerialLoop()
    {
        try
        {
            _port = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 100,
                WriteTimeout = 250
            };

            _port.Open();

            Debug.Log($"СОМ-порт {portName} открыт");

            while (_running)
            {
                if (_commandQueue.TryDequeue(out string command))
                {
                    _port.WriteLine(command);
                    Debug.Log($"Отправлено: {command}");
                }
                Thread.Sleep(10);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Не удалость открыть СОМ-порт {portName}: {exception.Message}");
        }
        finally
        {
            if (_port != null)
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                }
                _port.Dispose();
                _port = null;
            }
            Debug.Log($"COM-порт {portName} закрыт");
        }
    }

    private void StopSerialThread()
    {
        _running = false;

        if (_serialThread != null && _serialThread.IsAlive)
        {
            _serialThread.Join(300);
        }

        _serialThread = null;
    }
}
