﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PLProcessModel;
using DCIRDBAccess;
namespace LineNodes
{
    public class NodeCCDCheck:CtlNodeBaseModel
    {
        protected System.DateTime devOpenSt = DateTime.Now;
        protected dbDCIRBll dcirBll = new dbDCIRBll();
        public override bool BuildCfg(System.Xml.Linq.XElement xe, ref string reStr)
        {
            if (!base.BuildCfg(xe, ref reStr))
            {
                return false;
            }
            this.dicCommuDataDB1[1].DataDescription = "A通道读卡结果，1：复位/待机状态,2：RFID读取成功,3：RFID读取失败,4：无绑定数据";
            this.dicCommuDataDB1[2].DataDescription = "B通道读卡结果,1：复位/待机状态,2：RFID读取成功,3：RFID读取失败,4：无绑定数据";
            this.dicCommuDataDB1[3].DataDescription = "A通道,1：复位/待机状态,2：数据读取中,3：数据读取完毕，放行";
            this.dicCommuDataDB1[4].DataDescription = "B通道,1：复位/待机状态,2：数据读取中,3：数据读取完毕，放行";

            this.dicCommuDataDB2[1].DataDescription = "0：无,1：正在运行A通道,2：正在运行B通道";
            this.dicCommuDataDB2[2].DataDescription = "1：A通道无板,2：A通道有板，读卡请求";
            this.dicCommuDataDB2[3].DataDescription = "1：B通道无板,2：B通道有板，读卡请求";
            return true;
        }
        public override bool ExeBusiness(ref string reStr)
        {
            if (!nodeEnabled)
            {
                return true;
            }
            if (!ExeBusinessAB(ref reStr))
            {
                return false;
            }
            switch (currentTaskPhase)
            {
                case 1:
                    {
                        if (!RfidReadAB())
                        {
                            break;
                        }
                     //   db1ValsToSnd[1] = 2;//
                        if (!ProductTraceRecord())
                        {
                            break;
                        }
                        currentTaskPhase++;
                        this.currentTask.TaskParam = rfidUID;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
                case 2:
                    {
                        List<DBAccess.Model.BatteryModuleModel> modList = modBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1", this.rfidUID));
                        if (modList.Count() < 1)
                        {
                            currentTaskPhase = 9;
                            break;
                        }
                       // List<DBAccess.Model.BatteryModuleModel> modList = modBll.GetModelList(string.Format("palletID='{0}'", this.rfidUID));
                        //if(this.nodeID == "OPB003" || this.nodeID == "OPB006" || this.nodeID == "OPB007")
                        //{
                        //    if (!PreMechB(modList, ref reStr))
                        //    {
                        //        Console.WriteLine(string.Format("{0},{1}", nodeName, reStr));
                        //        break;
                        //    }
                        //}
                        //else
                        //{
                            if (!PreMech(modList, ref reStr))
                            {
                                Console.WriteLine(string.Format("{0},{1}", nodeName, reStr));
                                break;
                            }
                        //}
                      
                       
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
                case 3:
                    {
                        //发送启动加工命令

                        List<DBAccess.Model.BatteryModuleModel> modList = modBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1", this.rfidUID));
                        if(modList.Count()<1)
                        {
                            this.db1ValsToSnd[channelIndex - 1] = 4;

                            break;
                        }
                        List<string> products = new List<string>();
                        foreach (DBAccess.Model.BatteryModuleModel m in modList)
                        {
                            products.Add(m.batModuleID);
                        }
                        if (products.Count() > 0)
                        {
                            if (ccdDevAcc != null)
                            {
                                if (!ccdDevAcc.StartDev(products, ccdDevName, ref reStr))
                                {
                                    this.currentTaskDescribe = "发送设备加工启动命令失败:"+reStr;
                                    //logRecorder.AddDebugLog(nodeName, "发送设备加工启动命令失败");
                                  //  Console.WriteLine(nodeName + "发送设备加工启动命令失败");
                                    break;
                                }
                                else
                                {
                                    logRecorder.AddDebugLog(nodeName, "发送设备加工启动命令成功,发送数据:"+reStr);
                                }
                            }
                        }
                        devOpenSt = System.DateTime.Now;
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
                case 4:
                    {
                       if(this.nodeID =="OPB007" )
                       {
                           currentTaskPhase++;
                           break;
                       }
                        System.DateTime cur = System.DateTime.Now;
                        TimeSpan ts = cur - devOpenSt;
                        if (ts.TotalSeconds < 60)
                        {
                            break;
                        }
                        IDictionary<string, string> ccdDataDic = ccdDevAcc.GetData(ccdDevName, ref reStr);
                        if (ccdDataDic == null || ccdDataDic.Keys.Count() < 1)
                        {
                           // logRecorder.AddDebugLog(nodeName, string.Format("获取CCD{0}数据失败,{1}", ccdDevName, reStr));
                            this.currentTaskDescribe = "获取CCD数据失败，返回产品数据为空";
                            //Console.WriteLine("{0},获取CCD{1}数据失败,{2}", nodeName, ccdDevName, reStr);
                            
                            break;
                        }
                        foreach (string keyStr in ccdDataDic.Keys)
                        {
                            string str = string.Format("CCD数据，产品ID:{0}，数据：{1}", keyStr, ccdDataDic[keyStr]);
                            logRecorder.AddDebugLog(nodeName, str);
                            AddProcessRecord(keyStr, "模块", "检测数据", string.Format("读取到{0}检测数据", ccdDevName), ccdDataDic[keyStr]);

                        }
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
                case 5:
                    {
                       if(this.nodeID != "OPB007")//等于dcir工位 要等到在读取完取数据库后再发送end指令
                       {
                           //发送停止加工命令
                           if (!ccdDevAcc.EndDev(this.ccdDevName, ref reStr))
                           {
                               //  logRecorder.AddDebugLog(nodeName, "发送设备停止命令失败");
                               this.currentTaskDescribe = "发送设备停止命令失败:" + reStr;
                               //  Console.WriteLine(nodeName + "发送设备停止命令失败");
                               break;
                           }
                           else
                           {
                               logRecorder.AddDebugLog(nodeName, "发送设备停止命令成功");
                           }
                       
                       }
                      
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
                case 6:
                    {
                        List<DBAccess.Model.BatteryModuleModel> modList = modBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1", this.rfidUID));
                        //List<DBAccess.Model.BatteryModuleModel> modList = modBll.GetModelList(string.Format("palletID='{0}'", this.rfidUID));
                        if (!AfterMech(modList, ref reStr))
                        {
                            //Console.WriteLine(string.Format("{0},{1}", nodeName, reStr));
                            break;
                        }
                       
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
               
                case 7:
                    {
                        this.currentTaskDescribe = "上传MES数据";
                        List<DBAccess.Model.BatteryModuleModel> modList = null;
                        List<dcirMode> dataList = null;
                        modList = modBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1", this.rfidUID));
                        //if (this.nodeID == "OPB003" || this.nodeID == "OPB006")
                        //{
                        //    modList = modBll.GetModelList(string.Format("palletID='{0}'", this.rfidUID));
                        //}
                        //else 
                        //if (this.nodeID == "OPB007")
                        //{
                          
                        //  //  modList = modBll.GetModelList(string.Format("palletID='{0}'", this.rfidUID));
                        //    //dataList = dcirBll.GetModelListByTestTimeASC();
                        //}
                        // if(isWithMes == true)
                        //{
                            string M_WORKSTATION_SN = "";
                            if (this.nodeID == "OPB003")
                            {
                                M_WORKSTATION_SN = "Y00101601";
                            }
                            else if(this.nodeID == "OPB006")
                            {
                                M_WORKSTATION_SN = "Y00101901";
                            }
                            else if(this.nodeID == "OPB007")
                            {
                                M_WORKSTATION_SN = "Y00102101";
                                int temperatureRequire = 0;
                                if(this.plcRW2.ReadDB("D9100",ref temperatureRequire) ==false)
                                {
                                    break;
                                }
                                if(temperatureRequire != 1)//温度读取请求
                                {
                                    break;
                                }
                            }
                            else if(this.nodeID == "OPA009")
                            {
                                M_WORKSTATION_SN = "Y00101401";
                            }

                           
                            for(int i = 0;i<modList.Count;i++)
                            {
                                int M_FLAG = 3;
                                string M_DEVICE_SN = "";
                                string M_SN = modList[i].batModuleID;
                                string M_UNION_SN = "";
                                string M_CONTAINER_SN = "";
                                string M_LEVEL = "";
                                string M_ITEMVALUE = "";
                                if (this.nodeID == "OPB003" || this.nodeID == "OPB006")
                                {
                                    if (modList[i].checkResult == 2)
                                    {
                                        M_ITEMVALUE = "激光清洗结果:" + "NG" + ":";
                                    }
                                    else
                                    {
                                        M_ITEMVALUE = "激光清洗结果:" + "OK" + ":";
                                    }
                                }
                                else if(this.nodeID == "OPB007")
                                {
                                    //int index = dataList.Count-modList.Count + i;
                                    dcirMode dcirData = dcirBll.GetModelByMoudCode(modList[i].batModuleID);
                                    if(dcirData == null)
                                    {
                                        Console.WriteLine("获取dcir数据失败：二维码：" + modList[i].batModuleID);
                                        break;
                                    }
                                    int val = 0;
                                    float valF = (float)val / 10;
                                    int tempValAddr = 9000 + i * 2;
                                    string tempAddrStr = "D" + tempValAddr;
                                    plcRW2.ReadDB(tempAddrStr, ref val); 
                                    M_ITEMVALUE = "DCIR值:" + dcirData.电阻值+ ":mΩ|DCIR测试放电电流1:" + dcirData.放电电流1 + ":A|DCIR测试放电电流2:" + dcirData.放电电流2 + ":A|温度:" + valF.ToString() + ":℃";
                                    //M_ITEMVALUE = "DCIR值:0:mΩ|DCIR测试放电电流1:" + dataList[index].电流.ToString() + ":A|DCIR测试放电电流2:0:A|DCIR测试放电时间:0:s|DCIR测试静置时间:" + dataList[index].相对时间.ToString()+ ":s|温度:" + val.ToString() + ":℃";
                                }
                                else if(this.nodeID == "OPA009")
                                {
                                    if (modList[i].checkResult == 2)
                                    {
                                        M_ITEMVALUE = "导流片安装结果:" + "NG" + ":";
                                    }
                                    else
                                    {
                                        M_ITEMVALUE = "导流片安装结果:" + "OK" + ":";
                                    }
                                }
                               
                                RootObject rObj = new RootObject();
                                string strJson = "";
                                rObj = DevDataUpload(M_FLAG, M_DEVICE_SN, M_WORKSTATION_SN, M_SN, M_UNION_SN, M_CONTAINER_SN, M_LEVEL, M_ITEMVALUE,ref strJson);
                                logRecorder.AddDebugLog(nodeName, string.Format("模组{0} CCD检测结果{1}上传MES，返回{2}", M_SN, M_ITEMVALUE, rObj.RES));
                                this.currentTaskDescribe = string.Format("模组{0}UV结果{1}上传MES，返回{2}", M_SN, M_ITEMVALUE, rObj.RES);
                                foreach (DBAccess.Model.BatteryModuleModel mod in modList)
                                {
                                    if(mod.checkResult == 2)
                                    {
                                        mod.palletBinded = false;
                                        modBll.Update(mod);
                                    }
                                }
                               
                            }
        
                        }

                      
                        currentTaskPhase++;
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    //}
                case 8:
                        {
                            if (this.nodeID == "OPB007")//dcir工位 要等到在读取完取数据库后再发送end指令
                            {
                                if (this.plcRW2.WriteDB("D9100", 2) == false)
                                {
                                    break;
                                }
                                this.logRecorder.AddDebugLog(this.nodeName, "D9100数据写入2成功！");
                                //发送停止加工命令
                                if (!ccdDevAcc.EndDev(this.ccdDevName, ref reStr))
                                {
                                    //  logRecorder.AddDebugLog(nodeName, "发送设备停止命令失败");
                                    this.currentTaskDescribe = "发送设备停止命令失败:" + reStr;
                                    //  Console.WriteLine(nodeName + "发送设备停止命令失败");
                                    break;
                                }
                                else
                                {
                                    logRecorder.AddDebugLog(nodeName, "发送设备停止命令成功");
                                }

                            }

                           
                            if (this.nodeID == "OPA009")
                            {
                                if (!TryUnbind(this.rfidUID, ref reStr))
                                {
                                    this.currentTaskDescribe = "工装板解绑失败:" + reStr;
                                    break;
                                }
                            }
                            currentTaskPhase++;
                            this.currentTask.TaskPhase = this.currentTaskPhase;
                            this.ctlTaskBll.Update(this.currentTask);
                            break;
                        }
                case 9:
                    {
                       
                        db1ValsToSnd[2+this.channelIndex-1] = 3;
                        currentStat.StatDescribe = "流程完成";
                        currentTaskDescribe = "流程完成";
                        this.currentTask.TaskPhase = this.currentTaskPhase;
                        this.ctlTaskBll.Update(this.currentTask);
                        break;
                    }
              
                default:
                    break;
            }
            return true;
        }
    }
}
