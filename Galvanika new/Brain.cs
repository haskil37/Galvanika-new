//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using xFunc.Maths;
//using xFunc.Maths.Results;

//namespace Galvanika_new
//{
//    class Brain
//    {
//        string output = "";
//        private string InputPath = "Input.txt";
//        private string MarkerPath = "Marker.txt";
//        private string OutputPath = "Output.txt";

//        private Processor processor = new Processor();

//        string doubleBKT = ""; //переменная для двойной закрывающей скобки
//        public bool Write(ProgramData value)
//        {
//            if (value.Operator.Contains("=") && !value.Operator.Contains("I")) //Запись в какую-то переменную
//                return true;
//            return false;
//        }
//        public void Initialize()
//        {
            
//        }
//        private static string ReverseString(string s)
//        {
//            char[] value = s.ToCharArray();
//            Array.Reverse(value);
//            return new string(value);
//        }
//        public string Calc(Dictionary<int, int> item)
//        {
//            for (int i = item.Key; i <= item.Value; i++)
//            {
//                var value = MainWindow.DataGridTable[i] as ProgramData;
//                if (value == null)
//                    break;
//                if (Write(value))
//                {
//                    //DataWrite(value, output);
//                }
//                else
//                {
//                    string thisOperator = "";
//                    if (value.Operator.Contains(")"))
//                    {
//                        output += ") " + doubleBKT;
//                        doubleBKT = "";
//                    }
//                    else if (value.Operator.Contains("U"))
//                    {
//                        thisOperator = " and ";
//                        if (value.Operator.Contains("("))
//                            thisOperator += " ( ";
//                    }
//                    else if (value.Operator.Contains("O"))
//                    {
//                        thisOperator = " or ";
//                        if (value.Operator.Contains("("))
//                            thisOperator += " ( ";
//                        if (string.IsNullOrEmpty(value.Bit))
//                        {
//                            thisOperator += " ( ";
//                            doubleBKT += ")";
//                        }
//                    }

//                    if (output.Length != 0)
//                    {
//                        if (output.TrimEnd().LastOrDefault() != '(')
//                            output += thisOperator;
//                        else
//                        {
//                            if (thisOperator.TrimEnd().LastOrDefault() == '(')
//                                output += "(";
//                        }
//                    }
//                    else
//                    {
//                        if (thisOperator.Contains("("))
//                            output += " ( ";
//                    }

//                    if (value.Operator.Contains("L")) //Это загрузка
//                    {
//                        var timerData = ValueBool(value);
//                        if (value.AEM.Contains("S5T") || timerData.Contains("s5t")) //Если таймер
//                        {
//                            if (timerData == "0")
//                            {
//                                timerData = value.AEM.ToLower();
//                            }
//                            var temp = processor.Solve<BooleanResult>(output).Result;
//                            ProgramData valueNext = sourceForDataGrid[i + 1] as ProgramData;
//                            if (valueNext.Operator.Contains("SE"))
//                            {
//                                if (temp == false) //Обнуляем таймер если SE 
//                                {
//                                    if (Timer.Keys.Contains(valueNext.Bit.ToString()))
//                                    {
//                                        this.Timer.Remove(valueNext.Bit.ToString());
//                                        MyTimers valueTime = sourceForTimerGrid.Where(u => u.Address == valueNext.Bit).SingleOrDefault() as MyTimers;
//                                        valueTime.Time = 0;
//                                        valueTime.EndTime = 0;
//                                        valueTime.Value = 0;
//                                    }
//                                    break;
//                                }
//                                else //Создаем новый таймер если SE 
//                                {
//                                    if (!Timer.Keys.Contains(valueNext.Bit.ToString()))
//                                    {
//                                        var tempTimerData = timerData.Split('#');
//                                        string tempTime;
//                                        int newTempTime;
//                                        if (tempTimerData[1].Contains("ms"))
//                                        {
//                                            tempTime = tempTimerData[1].Replace("ms", "");
//                                            newTempTime = Convert.ToInt32(tempTime);
//                                        }
//                                        else
//                                        {
//                                            tempTime = tempTimerData[1].Replace("s", "");
//                                            newTempTime = Convert.ToInt32(tempTime);
//                                            newTempTime = newTempTime * 1000;
//                                        }
//                                        this.Timer.Add(valueNext.Bit, newTempTime);
//                                        var containsTimer = sourceForTimerGrid.Where(u => u.Address == valueNext.Bit).SingleOrDefault();
//                                        if (containsTimer == null)
//                                        {
//                                            MyTimers valueTime = new MyTimers(valueNext.Bit, 0, newTempTime, 0);
//                                            sourceForTimerGrid.Add(valueTime);
//                                        }
//                                        else
//                                        {
//                                            containsTimer.EndTime = newTempTime;
//                                            containsTimer.Time = 0;
//                                            containsTimer.Value = 0;
//                                        }
//                                    }
//                                    break;
//                                }
//                            }
//                            else if (valueNext.Operator.Contains("SA")) //если SA то наоборот запускаем
//                            {

//                            }
//                        }
//                        else if (compareValues.Count == 0) //Если сравнение
//                        {
//                            var temp = output.Trim();
//                            temp = temp.Remove(temp.Length - 1, 1);
//                            temp = temp.Trim();
//                            temp = temp.Substring(0, temp.LastIndexOf(' '));

//                            var tempValue = processor.Solve<BooleanResult>(temp).Result;
//                            if (tempValue == false)
//                                break;
//                        }
//                        var currentInt = ValueBool(value);
//                        compareValues.Add(Convert.ToInt32(currentInt));
//                    }

//                    if (value.Operator.Contains("=="))
//                    {
//                        if (compareValues[0] == compareValues[1])
//                            output += "true";
//                        else
//                            output += "false";
//                    }
//                    else if (value.Operator.Contains("<>"))
//                    {
//                        if (compareValues[0] != compareValues[1])
//                            output += "true";
//                        else
//                            output += "false";
//                    }

//                    if (value.Operator.Contains("SPBNB"))
//                    {
//                        var tempValue = processor.Solve<BooleanResult>(output).Result;
//                        tempValue = true;
//                        if (tempValue)
//                        {
//                            ProgramData valueNext = sourceForDataGrid[i + 1] as ProgramData;
//                            var valueToNext = ValueBool(valueNext);
//                            ProgramData valueNext2 = sourceForDataGrid[i + 2] as ProgramData;
//                            var memory = sourceForMemoryGrid.Find(u => u.Address == valueNext2.Bit);
//                            memory.CurrentValue = valueToNext.ToUpper();
//                        }
//                        break;
//                    }

//                    if (value.Operator.Contains("S"))
//                    {
//                        var tempValue = processor.Solve<BooleanResult>(output).Result;
//                        if (tempValue)
//                            DataWrite(value, "true");
//                        else
//                            ValueBool(value);

//                        //Смотрим сл. строку, если там R или S то не обнуляем output
//                        ProgramData valueNext = sourceForDataGrid[i + 1] as ProgramData;
//                        if (!valueNext.Operator.Contains("R"))
//                            if (!valueNext.Operator.Contains("S"))
//                                output = "";
//                    }
//                    else if (value.Operator.Contains("R"))
//                    {
//                        var tempValue = processor.Solve<BooleanResult>(output).Result;
//                        if (tempValue)
//                            DataWrite(value, "false");
//                        else
//                            ValueBool(value);

//                        //Смотрим сл. строку, если там R или S то не обнуляем output
//                        ProgramData valueNext = sourceForDataGrid[i + 1] as ProgramData;
//                        if (!valueNext.Operator.Contains("R"))
//                            if (!valueNext.Operator.Contains("S"))
//                                output = "";
//                    }
//                    else
//                    {
//                        if (!thisOperator.Contains("("))
//                            if (!value.Operator.Contains(")"))
//                                if (!value.Operator.Contains("L"))
//                                    if (!value.Operator.Contains("="))
//                                        if (!value.Operator.Contains("<>"))
//                                            output += ValueBool(value);
//                    }
//                }
//            }
//            return output;
//        }
//        private string ValueBool(ProgramData value)
//        {
            
//            var valueBool = false;

//            if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("E"))
//            {
//                valueBool = DataRead(value.Bit, InputPath);
//                value.Input = Convert.ToInt32(valueBool).ToString();
//            }
//            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("M") && !value.AEM.Contains("MS") && !value.AEM.Contains("ms"))
//            {
//                valueBool = DataRead(value.Bit, MarkerPath);
//                value.Marker = Convert.ToInt32(valueBool).ToString();
//            }
//            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("A"))
//            {
//                valueBool = DataRead(value.Bit, OutputPath);
//                value.Input = Convert.ToInt32(valueBool).ToString();
//            }
//            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("DB"))
//            {
//                string tempValue;
//                if (MainWindow.DB.ContainsKey(value.Bit))
//                    tempValue = MainWindow.DB[value.Bit].ToLower();
//                else
//                {
//                    var split = value.Bit.Split('.');
//                    var olderByte = Convert.ToInt32(split[0]) - 1;
//                    var valueOlderByte = Convert.ToString(Convert.ToInt32(MainWindow.DB[olderByte.ToString()]), 2);
//                    while (valueOlderByte.Length < 8)
//                        valueOlderByte = valueOlderByte.Insert(0, "0");
//                    valueOlderByte = ReverseString(valueOlderByte);
//                    valueOlderByte = valueOlderByte.Substring(Convert.ToInt16(split[1]), 1);
//                    if (valueOlderByte == "0")
//                        tempValue = "false";
//                    else
//                        tempValue = "true";
//                }

//                if (tempValue.Contains("true") || tempValue.Contains("false")) //Пока только булевые из БД
//                    valueBool = Convert.ToBoolean(tempValue);
//                //else
//                //{
//                //    if (!tempValue.Contains("s5t")) // Если не таймер то числа
//                //        return tempValue;
//                //}
//                value.Input = Convert.ToInt32(valueBool).ToString();
//            }
//            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("T") && value.Bit.Length > 0)
//            {
//                var containsTimer = MainWindow.TimerGridTable.Where(u => u.Address == value.Bit).SingleOrDefault();
//                if (containsTimer != null)
//                    valueBool = Convert.ToBoolean(Convert.ToInt32(containsTimer.Value));
//                else
//                    valueBool = false;
//            }
//            else if (!string.IsNullOrEmpty(value.AEM))
//            {
//                //Значит тут наверно число
//                var valueInt = value.AEM;
//                int result;
//                int.TryParse(valueInt, out result);
//                return result.ToString();
//            }
//            //Проверяем на негатив
//            if (value.Operator.Substring(value.Operator.Length - 1, 1) == "N") //не забыть что SPBNB не учитывается как негатив
//                valueBool = processor.Solve<BooleanResult>("~" + valueBool).Result;

//            return valueBool.ToString();
//        }
//        private bool DataRead(string byteAndBit, string path)
//        {
//            var address = byteAndBit.Split('.');
//            var count = -1;
//            var value = "";
//            using (StreamReader fs = new StreamReader(path, Encoding.Default))
//            {
//                int skip = Convert.ToInt32(address[0].ToString());
//                while (count != skip)
//                {
//                    value = fs.ReadLine();
//                    count++;
//                }
//            }
//            var bits = Convert.ToString(Convert.ToInt32(value), 2);
//            while (bits.Length < 8)
//                bits = bits.Insert(0, "0");
//            bits = ReverseString(bits);
//            count = 0;
//            foreach (char ch in bits)
//            {
//                if (count.ToString() == address[1])
//                {
//                    var tempString = ch.ToString();
//                    var tempInt = Convert.ToInt32(tempString);
//                    var tempBool = Convert.ToBoolean(tempInt);
//                    return tempBool;
//                }
//                count++;
//            }
//            return false;
//        }
//    }
//}
