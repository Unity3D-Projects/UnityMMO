using System;
using System.Collections.Generic;
using Sproto;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Protocol;

namespace UnityMMO {
public class SynchFromNet {
    private static SynchFromNet instance = null;
    Dictionary<SceneInfoKey, Action<Entity, SprotoType.info_item> > changeFuncDic;

    public static SynchFromNet Instance 
    { 
        get  {
            if (instance != null)
                return instance;
            return instance = new SynchFromNet();
        }
    }

    public void Init()
    {
        changeFuncDic = new Dictionary<SceneInfoKey, Action<Entity, SprotoType.info_item>>();
        changeFuncDic[SceneInfoKey.PosChange] = ApplyChangeInfoPos;
        changeFuncDic[SceneInfoKey.TargetPos] = ApplyChangeInfoTargetPos;
    }

    public void StartSynchFromNet()
    {
        ReqSceneObjInfoChange();
        ReqNewFightEvens();
    }

    public void ReqNewFightEvens()
    {
        // Debug.Log("GameVariable.IsNeedSynchSceneInfo : "+GameVariable.IsNeedSynchSceneInfo.ToString());
        if (GameVariable.IsNeedSynchSceneInfo)
        {
            SprotoType.scene_get_objs_info_change.request req = new SprotoType.scene_get_objs_info_change.request();
            NetMsgDispatcher.GetInstance().SendMessage<Protocol.scene_get_objs_info_change>(req, OnAckFightEvents);
        }
        else
        {
            Timer.Register(0.5f, () => ReqNewFightEvens());
        }
    }

    public void OnAckFightEvents(SprotoTypeBase result)
    {
        

    }

    public void ReqSceneObjInfoChange()
    {
        // Debug.Log("GameVariable.IsNeedSynchSceneInfo : "+GameVariable.IsNeedSynchSceneInfo.ToString());
        if (GameVariable.IsNeedSynchSceneInfo)
        {
            SprotoType.scene_get_objs_info_change.request req = new SprotoType.scene_get_objs_info_change.request();
            NetMsgDispatcher.GetInstance().SendMessage<Protocol.scene_get_objs_info_change>(req, OnAckSceneObjInfoChange);
        }
        else
        {
            Timer.Register(0.5f, () => ReqSceneObjInfoChange());
        }
    }

    public void OnAckSceneObjInfoChange(SprotoTypeBase result)
    {
        // Debug.Log("synch from net received OnAckSceneObjInfoChange:"+(result!=null).ToString());
        SprotoType.scene_get_objs_info_change.request req = new SprotoType.scene_get_objs_info_change.request();
        NetMsgDispatcher.GetInstance().SendMessage<Protocol.scene_get_objs_info_change>(req, OnAckSceneObjInfoChange);
        SprotoType.scene_get_objs_info_change.response ack = result as SprotoType.scene_get_objs_info_change.response;
        if (ack==null)
            return;
        int len = ack.obj_infos.Count;
        for (int i = 0; i < len; i++)
        {
            long uid = ack.obj_infos[i].scene_obj_uid;
            Entity scene_obj = SceneMgr.Instance.GetSceneObject(uid);
            var change_info_list = ack.obj_infos[i].info_list;
            int info_len = change_info_list.Count;
            // Debug.Log("uid : "+uid.ToString()+ " info_len:"+info_len.ToString());
            for (int info_index = 0; info_index < info_len; info_index++)
            {
                var cur_change_info = change_info_list[info_index];
                // Debug.Log("cur_change_info.key : "+cur_change_info.key.ToString()+" scene_obj:"+(scene_obj!=Entity.Null).ToString()+ " ContainsKey:"+changeFuncDic.ContainsKey((SceneInfoKey)cur_change_info.key).ToString()+" uid"+uid.ToString()+" value:"+cur_change_info.value.ToString());
                if (cur_change_info.key == (int)SceneInfoKey.EnterScene)
                {
                    if (scene_obj==Entity.Null)
                    {
                        SceneObjectType sceneObjType = (SceneObjectType)Enum.Parse(typeof(SceneObjectType), cur_change_info.value);
                        scene_obj = SceneMgr.Instance.AddSceneObject(uid, sceneObjType);
                    }
                }
                else if(cur_change_info.key == (int)SceneInfoKey.LeaveScene)
                {
                    if (scene_obj!=Entity.Null)
                    {
                        SceneMgr.Instance.RemoveSceneObject(uid);
                        scene_obj = Entity.Null;
                    }
                }
                else if (scene_obj != Entity.Null && changeFuncDic.ContainsKey((SceneInfoKey)cur_change_info.key))
                {
                    changeFuncDic[(SceneInfoKey)cur_change_info.key](scene_obj, cur_change_info);
                }
            }
        }
    }

    private void ApplyChangeInfoPos(Entity entity, SprotoType.info_item change_info)
    {
        string[] pos_strs = change_info.value.Split(',');
        // Debug.Log("SynchFromNet recieve pos value : "+change_info.value);
        if (pos_strs.Length != 3)
        {
            Debug.Log("SynchFromNet recieve a wrong pos value : "+change_info.value);
            return;
        }
        long new_x = Int64.Parse(pos_strs[0]);
        long new_y = Int64.Parse(pos_strs[1]);
        long new_z = Int64.Parse(pos_strs[2]);
        if (SceneMgr.Instance.EntityManager.HasComponent<Transform>(entity))
        {
            Transform trans = SceneMgr.Instance.EntityManager.GetComponentObject<Transform>(entity);
            // Debug.Log("receive new pos"+new_x+" "+new_y+" "+new_z);
            trans.localPosition = new Vector3(new_x/GameConst.RealToLogic, new_y/GameConst.RealToLogic, new_z/GameConst.RealToLogic);
        }
        // SceneMgr.Instance.EntityManager.SetComponentData(entity, new TargetPosition {Value = new float3(new_x/GameConst.RealToLogic, new_y/GameConst.RealToLogic, new_z/GameConst.RealToLogic)});
    }

    private void ApplyChangeInfoTargetPos(Entity entity, SprotoType.info_item change_info)
    {
        string[] pos_strs = change_info.value.Split(',');
        // Debug.Log("SynchFromNet recieve pos value : "+change_info.value);
        if (pos_strs.Length != 3)
        {
            Debug.Log("SynchFromNet recieve a wrong pos value : "+change_info.value);
            return;
        }
        long new_x = Int64.Parse(pos_strs[0]);
        long new_y = Int64.Parse(pos_strs[1]);
        long new_z = Int64.Parse(pos_strs[2]);
        SceneMgr.Instance.EntityManager.SetComponentData(entity, new TargetPosition {Value = new float3(new_x/GameConst.RealToLogic, new_y/GameConst.RealToLogic, new_z/GameConst.RealToLogic)});
    }
}
}