using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


public class UdpSender : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 3333;
    private UdpClient client;

    bool isConnect = false;

    void Start()
    {
        client = new UdpClient();
    }

    public void ConnectClient(string host, int port)
    {
        if (isConnect)
        {
            return;
        }
        this.host = host;
        this.port = port;
        client.Connect(host, port);
    }

    public void CloseClient()
    {
        if (!isConnect)
        {
            return;
        }
        client.Close();
    }

    public void Test_Send()
    {
        byte[] dgram = Encoding.UTF8.GetBytes("hello!");
        SendData(dgram);
    }

    public async Task SendData(string json)
    {
        await Task.Run(() => {
            byte[] dgram = Encoding.UTF8.GetBytes(json);
            client.Send(dgram, dgram.Length);
        });
    }

    public async Task SendData(byte[] dgram)
    {
        await Task.Run(() => {
            client.Send(dgram, dgram.Length);
        });
    }

    void OnDisable()
    {
        client.Close();
    }
}
