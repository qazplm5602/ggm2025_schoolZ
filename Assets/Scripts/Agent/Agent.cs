using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    private Dictionary<Type, IAgentComponent> agentComponents;

    void Awake()
    {
        DetectComponents();
    }

    void DetectComponents() {
        agentComponents = new(); // 새겅
        IAgentComponent[] components = GetComponentsInChildren<IAgentComponent>();

        foreach (IAgentComponent component in components) {
            agentComponents[component.GetType()] = component;
        }

        // component 캐싱다 한다음에 준비 되엇다고 해야함
        foreach (IAgentComponent component in components) {
            component.InitAgent(this);
        }
    }
    
    public T GetCompo<T>() where T : class {
        return agentComponents[typeof(T)] as T;
    }
}
