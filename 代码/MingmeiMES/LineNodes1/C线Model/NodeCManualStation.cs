﻿using PLProcessModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LineNodes
{
    /// <summary>
    /// c线人工工位
    /// </summary>
    public class NodeCManualStation : CtlNodeBaseModel
    {
        public override bool BuildCfg(System.Xml.Linq.XElement xe, ref string reStr)
        {
            if (!base.BuildCfg(xe, ref reStr))
            {
                return false;
            }
            this.dicCommuDataDB1[1].DataDescription = "1:rfid复位2：RFID成功3：读RFID失败4：无绑定数据";
            this.dicCommuDataDB1[2].DataDescription = "1：复位/待机状态,2：数据读取中,3：数据读取完毕，放行";
            this.dicCommuDataDB2[1].DataDescription = "1：无板,2：有板，读卡请求";
            return true;
        }
        public override bool ExeBusiness(ref string reStr)
        {
            if (!ExeBusinessC(ref reStr))
            {
                return false;
            }


            switch (currentTaskPhase)
            {
                case 1:
                    {
                        if (!RfidReadC())
                        {
                            break;
                        }
                        
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.currentTask.TaskParam = rfidUID;
                        this.ctlTaskBll.Update(this.currentTask);
                        currentTaskPhase++;
                       
                        break;
                    }
                case 2:
                    {

                        db1ValsToSnd[1] = 2;//
                        if (!ProductTraceRecord())
                        {
                            break;
                        }
                        db1ValsToSnd[1] = 3;
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
                case 3:
                    {
                       List< DBAccess.Model.BatteryModuleModel > batteryModuleList = modBll.GetBindedMods(this.rfidUID);
                        if(batteryModuleList==null||batteryModuleList.Count==0)
                        {
                            Console.WriteLine(this.nodeName,"此工装板无绑定数据！");
                            break;
                        }
                        if (this.nodeID == "OPC007")
                        {
                            if (UploadToMesData(1, batteryModuleList[0].batPackID, "M00100701", ref reStr) == false)
                            {
                                this.logRecorder.AddDebugLog(this.nodeName, "上传MES数据失败：" + reStr);
                            }
                        }
                        else if (this.nodeID == "OPC008")
                        {
                            if (UploadToMesData(1, batteryModuleList[0].batPackID, "M00100401", ref reStr) == false)
                            {
                                this.logRecorder.AddDebugLog(this.nodeName, "上传MES数据失败：" + reStr);
                            }
                        }
                        else if (this.nodeID == "OPC009")
                        {
                            if (UploadToMesData(3, batteryModuleList[0].batPackID, "M00100501", ref reStr) == false)
                            {
                                this.logRecorder.AddDebugLog(this.nodeName, "上传MES数据失败：" + reStr);
                            }
                        }
                        else if (this.nodeID == "OPC010")
                        {
                            if (UploadToMesData(1, batteryModuleList[0].batPackID, "M00100701", ref reStr) == false)
                            {
                                this.logRecorder.AddDebugLog(this.nodeName, "上传MES数据失败：" + reStr);
                            }
                        }
                        this.logRecorder.AddDebugLog(this.nodeName, "上报MES数据成功：" + batteryModuleList[0].batPackID);
                       
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.currentTask.TaskStatus = EnumTaskStatus.已完成.ToString();
                        this.ctlTaskBll.Update(this.currentTask);
                       
                        break;
                    }
                case 4:
                    {
                        currentTaskDescribe = "流程完成";
                        break;
                    }
                default:
                    break;
            }
            return true;
        }

        protected override bool RfidReadC()
        {
            if (string.IsNullOrWhiteSpace(this.rfidUID))
            {
                //读RFID失败 

                if (this.db1ValsToSnd[0] != 3)
                {
                    //logRecorder.AddDebugLog(nodeName, "读RFID失败");
                    db1ValsToSnd[0] = 3;
                }
                else
                {
                    db1ValsToSnd[0] = 1;
                }
                if (SysCfgModel.SimMode)
                {
                    this.rfidUID = SimRfidUID;
                }
                else
                {
                    this.rfidUID = rfidRW.ReadUID();
                }
                Thread.Sleep(1000);
                this.currentStat.Status = EnumNodeStatus.无法识别;
                this.currentStat.StatDescribe = "读RFID失败";
                this.currentTaskDescribe = "读RFID失败";
                return false;
            }
            else
            {
                //if (!SysCfgModel.SimMode)
                //{
                //    if (this.nodeName != "C线打带")//这个rfid不一样不需要清缓存
                //    {

                //        (rfidRW as DevAccess.RfidCF).ClearBufUID();
                //    }
                //}
            }
            this.currentStat.StatDescribe = "RFID识别完成";
            List<DBAccess.Model.BatteryModuleModel> modList = modBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1", this.rfidUID));
            if (modList == null || modList.Count() < 1)
            {
                db1ValsToSnd[0] = 4;//
                this.currentTaskDescribe = string.Format("工装板{0}绑定数据为空", rfidUID);
                this.currentStat.StatDescribe = "绑定数据为空";
                
            }
            else
            {
                db1ValsToSnd[0] = 2;//读到RFID
            }

            this.currentStat.Status = EnumNodeStatus.设备使用中;
         

            this.currentTaskDescribe = "RFID识别完成:" + this.rfidUID;
            return true;
        }

        private bool UploadToMesData(int flag ,string groupCode,string  workStationNum,ref string reStr)
        {
            RootObject rObj = WShelper.DevDataUpload(flag, "", workStationNum, groupCode, "", "", "", "", ref reStr);
            if (rObj.RES.Contains("OK"))
            {
             
                return true;
            }
            else
            {
                reStr = rObj.RES;
                Console.WriteLine(this.nodeName + "上传MES二维码信息错误：" + rObj.RES);
                return false;
            }
        }
    }
}
