using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Galvanika_new
{
    class OpenFile
    {
        private string Path = "0000000d.AWL";
        private List<string> tempDB = new List<string>();
        private List<string> tempProgramList = new List<string>();
        private Dictionary<string, string> DB = new Dictionary<string, string>();
        private Dictionary<int, int> StartEndTemp = new Dictionary<int, int>();
        List<MemoryData> MemoryGridTable = new List<MemoryData>();
        List<ProgramData> DataGridTable = new List<ProgramData>();
        public OpenFile()
        {

        }
        public bool Exists()
        {
            if (!File.Exists(Path))
                return false;
            return true;
        }
        public void ReadFileDB()
        {
            using (StreamReader fs = new StreamReader(Path, Encoding.Default))
            {
                int start = 0;
                while (true)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    if (temp.Contains("END_STRUCT"))
                        break;

                    if (start == 1)
                        tempDB.Add(temp);

                    if (temp.Contains("STRUCT") && start != 1)
                        start = 1;
                }
                ParseDB();
                start = 0;
                while (true)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    if (temp.Contains("FUNCTION FC") && start != 1)
                        start = 1;
                    if (start == 1 && !temp.Contains("NOP"))
                        tempProgramList.Add(temp);
                    if (temp.Contains(": NOP"))
                        tempProgramList.Add(temp);
                }
            }
            FillGrid();
            MainWindow.DB = DB;
        }
        private void ParseDB()
        {
            foreach (var item in tempDB)
            {
                var itemNew = item;
                if (item.Contains("//"))
                    itemNew = item.Substring(0, item.IndexOf('/'));
                var tempFirstString = itemNew.Split('_');
                var tempSecondString = tempFirstString[1].Split('i');
                string tempIndex = "";
                if (tempSecondString.Count() > 1)
                    tempIndex = tempSecondString[0] + "." + tempSecondString[1];
                else
                    tempIndex = tempSecondString[0];
                var tempThirdString = tempFirstString[tempFirstString.Count() - 1].Split('=');
                if (tempThirdString.Count() > 1)
                {
                    var endOfString = tempThirdString[1].Trim();

                    if (endOfString.Contains(';'))
                        endOfString = endOfString.Remove(endOfString.Length - 1, 1);

                    DB.Add(tempIndex.Trim(), endOfString);
                }
                else
                {
                    if (tempThirdString[0].Contains("BOOL"))
                        DB.Add(tempIndex, "False");
                    else
                        DB.Add(tempIndex, "0");
                }
                var tempString = itemNew.Substring(itemNew.IndexOf('_') + 1); //Дважды удаляем до знака "_"
                tempString = tempString.Substring(tempString.IndexOf('_') + 1);
                var tempNameP = tempString.Split(':');
                MemoryData result = new MemoryData("", "", "", "", "");
                if (tempNameP.Count() > 2)
                {
                    var value = tempNameP[2].Replace('=', ' ');
                    value = value.Replace(';', ' ');
                    if (tempNameP[1].Contains("BOOL"))
                        result = new MemoryData(tempIndex, tempNameP[0].Trim(), "bool", value.Trim().ToLower(), value.Trim().ToLower());
                    if (tempNameP[1].Contains("INT"))
                        result = new MemoryData(tempIndex, tempNameP[0].Trim(), "integer", value.Trim(), value.Trim());
                    if (tempNameP[1].Contains("TIME"))
                        result = new MemoryData(tempIndex, tempNameP[0].Trim(), "timer", value.Trim(), value.Trim());
                }
                else
                {
                    if (tempNameP[1].Contains("BOOL"))
                        result = new MemoryData(tempIndex, tempNameP[0].Trim(), "bool", "false", "false");
                    if (tempNameP[1].Contains("INT"))
                        result = new MemoryData(tempIndex, tempNameP[0].Trim(), "integer", "0", "0");
                    if (tempNameP[1].Contains("TIME"))
                        result = new MemoryData(tempIndex, tempNameP[0].Trim(), "timer", "0", "0");
                }
                MemoryGridTable.Add(result);
            }
            MainWindow.MemoryGridTable = MemoryGridTable;
        }
        private void FillGrid()
        {
            var countKey = 0;
            var countText = 0;
            foreach (string item in tempProgramList) // Загоняем в таблицу данные программы из файла
            {
                try
                {
                    if (item.Contains("NETWORK") || item.Contains("TITLE") || item.Contains("END") || item.Contains("FUNCTION FC") || item.Contains("VERSION") || item.Contains("BEGIN") || item.Contains("AUF   DB"))
                    {
                        var result = new ProgramData(0, item, "", "", "", "", "", "");
                        DataGridTable.Add(result);
                        if (StartEndTemp.Count != 0) //Разбитие на подпрограммы
                        {
                            var lastStart = StartEndTemp.Last();
                            if (lastStart.Key == lastStart.Value)
                                StartEndTemp[lastStart.Key] = countKey + countText - 1;
                        }
                        countText++;
                    }
                    else if (item.Trim().Length != 0)
                    {
                        if (StartEndTemp.Count != 0) //Разбитие на подпрограммы
                        {
                            var lastStart = StartEndTemp.Last();
                            if (lastStart.Key != lastStart.Value)
                                StartEndTemp.Add(countKey + countText, countKey + countText);
                        }
                        else
                            StartEndTemp.Add(countText, countText);

                        var itemSplit = item.Replace(';', ' ');
                        var stringData = itemSplit.Split(' ').ToList();
                        stringData.RemoveAll(RemoveEmpty);
                        countKey++;
                        if (stringData.Count > 2)
                        {
                            var result = new ProgramData(countKey, item, stringData[0], stringData[1], stringData[2], "", "", "");
                            DataGridTable.Add(result);
                            if (stringData.Contains("FP"))
                                MainWindow.FrontP.Add(countKey.ToString(), 0);
                            if (stringData.Contains("FN"))
                                MainWindow.FrontN.Add(countKey.ToString(), 0);
                        }
                        else if (stringData.Count == 2)
                        {
                            if (stringData.Contains("SPBNB"))
                            {
                                var result = new ProgramData(countKey, item, stringData[0], stringData[1], "", "", "", "");
                                DataGridTable.Add(result);
                            }
                            else if (stringData.Contains("S5T"))
                            {
                                var stringTimer = stringData[1].Split('#');
                                var result = new ProgramData(countKey, item, stringData[0], stringTimer[0], stringTimer[1], "", "", "");
                                DataGridTable.Add(result);
                            }
                            else if (stringData.Contains("L"))
                            {
                                var result = new ProgramData(countKey, item, stringData[0], stringData[1], "", "", "", "");
                                DataGridTable.Add(result);
                            }

                        }
                        else
                        {
                            var result = new ProgramData(countKey, item, stringData[0], "", "", "", "", "");
                            DataGridTable.Add(result);
                        }
                    }
                }
                catch
                {
                }
            }
            MainWindow.StartEnd = StartEndTemp;
            MainWindow.DataGridTable = DataGridTable;
        }
        private static bool RemoveEmpty(String s)
        {
            return s.Length == 0;
        }
    }
}
