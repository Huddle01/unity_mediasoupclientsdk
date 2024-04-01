using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Huddle01.Utils;

public class AwaitQueueTest : MonoBehaviour
{
    AwaitQueue _awaitQueue = new AwaitQueue();

    private int[] _intervalList = {100,200,500,1000,2000,5000 };

    // Start is called before the first frame update
    async void Start()
    {
        await Task.Delay(_intervalList[Random.Range(0, _intervalList.Length)]);
        await _awaitQueue.Push<bool>(ExecuteTask1, HandleTask1,"Execute Task 1");

        await Task.Delay(_intervalList[Random.Range(0, _intervalList.Length)]);
        await _awaitQueue.Push<int>(ExecuteTask2, HandleTask2, "Execute Task 2");

        await Task.Delay(_intervalList[Random.Range(0, _intervalList.Length)]);
        await _awaitQueue.Push<float>(ExecuteTask3, null, "Execute Task 3");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async Task<bool> ExecuteTask1(params object[] args) 
    {
        Debug.Log("Start executing " + args[0]);
        await Task.Delay(5000);
        Debug.Log(args[0]);
        return true;
    }

    private void HandleTask1(bool isCompleted) 
    {
        Debug.Log("Task 1 ompleted");
    }

    private async Task<int> ExecuteTask2(params object[] args)
    {
        Debug.Log("Start executing " + args[0]);
        await Task.Delay(5000);
        Debug.Log(args[0]);
        return 10;
    }

    private void HandleTask2(int val)
    {
        Debug.Log($"Task 2 completed with {val}");
    }

    private async Task<float> ExecuteTask3(params object[] args)
    {
        Debug.Log("Start executing " + args[0]);
        await Task.Delay(5000);
        Debug.Log(args[0]);
        return 100.1f;
    }

    private void HandleTask3(float val)
    {
        Debug.Log($"Task 3 completed with {val}");
    }

}
