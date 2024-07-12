﻿/*
Copyright 2015 Pim de Witte All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

/// Author: Pim de Witte (pimdewitte.com) and contributors, https://github.com/PimDeWitte/UnityMainThreadDispatcher
/// <summary>
/// A thread-safe class which holds a queue with actions to execute on the next Update() method. It can be used to make calls to the main thread for
/// things such as UI Manipulation in Unity. It was developed for use in combination with the Firebase Unity plugin, which uses separate threads for event handling
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    static readonly Queue<Action> _executionQueue = new Queue<Action>();
    static UnityMainThreadDispatcher _instance = null;

    public static void Enqueue(IEnumerator action)
    {
        if (_instance == null) throw new NullReferenceException();
        _instance.EnqueueInternal(action);
    }
    public static void Enqueue(Action action)
    {
        if (_instance == null) throw new NullReferenceException();
        _instance.EnqueueInternal(action);
    }
    public static UniTask EnqueueAsync(Action action)
    {
        if (_instance == null) throw new NullReferenceException();
        return _instance.EnqueueAsyncInternal(action);
    }

    void EnqueueInternal(IEnumerator action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => {
                StartCoroutine(action);
            });
        }
    }
    void EnqueueInternal(Action action)
    {
        Enqueue(ActionWrapper(action));
    }
    UniTask EnqueueAsyncInternal(Action action)
    {
        var tcs = new UniTaskCompletionSource<bool>();

        void WrappedAction()
        {
            try
            {
                action();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        Enqueue(ActionWrapper(WrappedAction));
        return tcs.Task;
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
    void OnDestroy()
    {
        _instance = null;
    }

    static IEnumerator ActionWrapper(Action a)
    {
        a();
        yield return null;
    }
}
