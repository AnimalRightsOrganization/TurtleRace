﻿using UnityEngine;
using System.Collections.Generic;
using ILRuntime.Other;
using System;
using System.Collections;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.CLR.Method;


public class MonoBehaviourAdapter : CrossBindingAdaptor
{
    public override Type BaseCLRType
    {
        get
        {
            return typeof(MonoBehaviour);
        }
    }

    public override Type AdaptorType
    {
        get
        {
            return typeof(Adaptor);
        }
    }

    public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        return new Adaptor(appdomain, instance);
    }
    //为了完整实现MonoBehaviour的所有特性，这个Adapter还得扩展，这里只抛砖引玉，只实现了最常用的Awake, Start和Update
    public class Adaptor : MonoBehaviour, CrossBindingAdaptorType
    {
        ILTypeInstance instance;
        ILRuntime.Runtime.Enviorment.AppDomain appdomain;

        public Adaptor()
        {

        }

        public Adaptor(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
        {
            this.appdomain = appdomain;
            this.instance = instance;
        }

        public ILTypeInstance ILInstance { get { return instance; } set { instance = value; } }

        public ILRuntime.Runtime.Enviorment.AppDomain AppDomain { get { return appdomain; } set { appdomain = value; } }

        IMethod mAwakeMethod;
        bool mAwakeMethodGot;
        public void Awake()
        {
            //Unity会在ILRuntime准备好这个实例前调用Awake，所以这里暂时先不掉用
            if (instance != null)
            {
                if (!mAwakeMethodGot)
                {
                    mAwakeMethod = instance.Type.GetMethod("Awake", 0);
                    mAwakeMethodGot = true;
                }

                if (mAwakeMethod != null)
                {
                    appdomain.Invoke(mAwakeMethod, instance, null);
                }
            }
        }
        
        IMethod mOnEnableMethod;
        bool mOnEnableMethodGot;
        public void OnEnable()
        {
            if (instance != null)
            {
                if (!mOnEnableMethodGot)
                {
                    mOnEnableMethod = instance.Type.GetMethod("OnEnable", 0);
                    mOnEnableMethodGot = true;
                }

                if (mOnEnableMethod != null)
                {
                    appdomain.Invoke(mOnEnableMethod, instance, null);
                }
            }
        }

        IMethod mOnDisableMethod;
        bool mOnDisableMethodGot;
        void OnDisable()
        {
            if (instance != null)
            {
                if (!mOnDisableMethodGot)
                {
                    mOnDisableMethod = instance.Type.GetMethod("OnDisable", 0);
                    mOnDisableMethodGot = true;
                }

                if (mOnDisableMethod != null)
                {
                    appdomain.Invoke(mOnDisableMethod, instance, null);
                }
            }
        }

        IMethod mStartMethod;
        bool mStartMethodGot;
        void Start()
        {
            if (!mStartMethodGot)
            {
                mStartMethod = instance.Type.GetMethod("Start", 0);
                mStartMethodGot = true;
            }

            if (mStartMethod != null)
            {
                appdomain.Invoke(mStartMethod, instance, null);
            }
        }

        IMethod mUpdateMethod;
        bool mUpdateMethodGot;
        void Update()
        {
            if (!mUpdateMethodGot)
            {
                mUpdateMethod = instance.Type.GetMethod("Update", 0);
                mUpdateMethodGot = true;
            }

            if (mUpdateMethod != null)
            {
                appdomain.Invoke(mUpdateMethod, instance, null);
            }
        }

        IMethod mLateUpdateMethod;
        bool mLateUpdateMethodGot;
        void LateUpdate()
        {
            if (instance != null)
            {
                if (!mLateUpdateMethodGot)
                {
                    mLateUpdateMethod = instance.Type.GetMethod("LateUpdate", 0);
                    mLateUpdateMethodGot = true;
                }

                if (mLateUpdateMethod != null)
                {
                    appdomain.Invoke(mLateUpdateMethod, instance, null);
                }
            }
        }

        IMethod mOnDestroyMethod;
        bool mOnDestroyMethodGot;
        void OnDestroy()
        {
            if (instance != null)
            {
                if (!mOnDestroyMethodGot)
                {
                    mOnDestroyMethod = instance.Type.GetMethod("OnDestroy", 0);
                    mOnDestroyMethodGot = true;
                }

                if (mOnDestroyMethod != null)
                {
                    appdomain.Invoke(mOnDestroyMethod, instance, null);
                }
            }
        }

        IMethod mOnApplicationQuitMethod;
        bool mOnApplicationQuitMethodGot;
        void OnApplicationQuit()
        {
            if (instance != null)
            {
                if (!mOnApplicationQuitMethodGot)
                {
                    mOnApplicationQuitMethod = instance.Type.GetMethod("OnApplicationQuit", 0);
                    mOnApplicationQuitMethodGot = true;
                }

                if (mOnApplicationQuitMethod != null)
                {
                    appdomain.Invoke(mOnApplicationQuitMethod, instance, null);
                }
            }
        }

        public override string ToString()
        {
            IMethod m = appdomain.ObjectType.GetMethod("ToString", 0);
            m = instance.Type.GetVirtualMethod(m);
            if (m == null || m is ILMethod)
            {
                return instance.ToString();
            }
            else
                return instance.Type.FullName;
        }
    }
}
