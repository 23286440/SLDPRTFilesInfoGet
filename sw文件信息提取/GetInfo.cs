﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SldWorks2018 = SolidWorks.Interop.sldworks;
using SwConst2018 = SolidWorks.Interop.swconst;

namespace sw文件信息提取
{
    internal class GetInfo
    {
        static public SLDFileSummaryInfo Get2018(string file)
        {
            SLDFileSummaryInfo sldFileSummaryInfo = new SLDFileSummaryInfo
            {
                FileName = file
            };
            SldWorks2018.SldWorks swApp = new SldWorks2018.SldWorks();
            swApp.Visible = true;
            SldWorks2018.ModelDoc2 swModel = (SldWorks2018.ModelDoc2)swApp.OpenDoc(file, (int)SwConst2018.swDocumentTypes_e.swDocPART);
            if (swModel == null)
            {
                return sldFileSummaryInfo;
            }
            sldFileSummaryInfo.Title = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoTitle];
            sldFileSummaryInfo.Subject = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoSubject];
            sldFileSummaryInfo.Author = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoAuthor];
            sldFileSummaryInfo.Keywords = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoKeywords];
            sldFileSummaryInfo.Comment = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoComment];
            sldFileSummaryInfo.SavedBy = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoSavedBy];
            sldFileSummaryInfo.DateCreated = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoCreateDate];
            sldFileSummaryInfo.DateSaved = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoSaveDate];
            sldFileSummaryInfo.DateCreated2 = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoCreateDate2];
            sldFileSummaryInfo.DateSaved2 = swModel.SummaryInfo[(int)SwConst2018.swSummInfoField_e.swSumInfoSaveDate2];

            swApp.CloseDoc(file);
            return sldFileSummaryInfo;
        }
    }

}
