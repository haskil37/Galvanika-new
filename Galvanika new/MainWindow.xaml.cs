using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using xFunc.Maths;
using xFunc.Maths.Results;
namespace Galvanika_new
{
    public partial class MainWindow : Window
    {
        #region Переменные
        private int readsCount = 0;

        //private string InputPath = "Input.txt";
        //private string MarkerPath = "Marker.txt";
        //private string OutputPath = "Output.txt";

        public static List<MemoryData> MemoryGridTable = new List<MemoryData>();
        public static List<ProgramData> DataGridTable = new List<ProgramData>();
        public static List<MyTimers> TimerGridTable = new List<MyTimers>();

        public static List<int> InputData = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 };
        public static List<int> MarkerData = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static List<int> OutputData = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 };

        public static Dictionary<string, string> DB = new Dictionary<string, string>();
        public static Dictionary<int, int> StartEnd = new Dictionary<int, int>();

        public static Dictionary<string, int> TimerSE = new Dictionary<string, int>();
        public static Dictionary<string, int> TimerSA = new Dictionary<string, int>();

        public static Dictionary<string, int> FrontP = new Dictionary<string, int>();
        public static Dictionary<string, int> FrontN = new Dictionary<string, int>();

        private Processor processor = new Processor();
        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        #endregion
        public MainWindow()
        {
            var OpenFile = new OpenFile();
            if (!OpenFile.Exists())
            {
                MessageBox.Show("Нет файла с программой");
                Application.Current.Shutdown();
            }
            OpenFile.ReadFileDB();
            InitializeComponent();
            memoryGrid.ItemsSource = MemoryGridTable;
            dataGrid.ItemsSource = DataGridTable;
            timerGrid.ItemsSource = TimerGridTable;


            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
            dispatcherTimer.Start();

            DispatcherTimer timerForTimer = new DispatcherTimer();
            timerForTimer.Tick += new EventHandler(timer_Tick);
            timerForTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerForTimer.Start();

            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            backgroundWorker.WorkerSupportsCancellation = true;
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            backgroundWorker.RunWorkerAsync();
            dispatcherTimer.Stop();
        }
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Calculate();
        }
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            readsCount++;
            reads.Content = readsCount;
            backgroundWorker.RunWorkerAsync();
        }

        private void Calculate()
        {
            foreach (var item in StartEnd)
            {
                string output = "";
                string doubleBKT = ""; //переменная для двойной закрывающей скобки
                var compareValues = new List<int>();
                for (int i = item.Key; i <= item.Value; i++)
                {
                    ProgramData value = DataGridTable[i] as ProgramData;
                    if (value == null)
                        break;

                    if (value.Operator.Contains("=") && !value.Operator.Contains("I")) //Вывод
                    {
                        if (!string.IsNullOrEmpty(doubleBKT))
                        {
                            output += doubleBKT;
                            doubleBKT = "";
                        }

                        if (string.IsNullOrEmpty(output))
                            output = "false";

                        DataWrite(value, output);
                    }
                    else //Cчитываем дальше
                    {
                        if (i == 170)
                        { }
                        string thisOperator = "";
                        if (value.Operator.Contains(")"))
                        {
                            if (output.Trim().LastOrDefault() != '(')
                            {
                                output += ") " + doubleBKT;
                                doubleBKT = "";
                            }
                            else
                            {
                                var temp = output.Trim();
                                temp = temp.Remove(temp.Length - 1, 1);
                                temp = temp.Trim();

                                temp = temp.Substring(0, temp.LastIndexOf(' '));
                                output = temp;
                            }
                        }
                        else if (value.Operator.Contains("U"))
                        {
                            thisOperator = " and ";
                            if (value.Operator.Contains("("))
                                thisOperator += " ( ";

                            //Проверяем сл строку на сравнение
                            //ProgramData valueNext = sourceForDataGrid[i + 1] as ProgramData;
                            //if (valueNext.Operator.Contains("L"))
                            //    thisOperator = "("; //Чтобы false не добавился
                        }
                        else if (value.Operator.Contains("O"))
                        {
                            thisOperator = " or ";
                            if (value.Operator.Contains("("))
                                thisOperator += " ( ";
                            if (string.IsNullOrEmpty(value.Bit))
                            {
                                thisOperator += " ( ";
                                doubleBKT += ")";
                            }
                        }

                        if (output.Length != 0)
                        {
                            if (output.TrimEnd().LastOrDefault() != '(')
                                output += thisOperator;
                            else
                            {
                                if (thisOperator.TrimEnd().LastOrDefault() == '(')
                                    output += "(";
                            }
                        }
                        else
                        {
                            if (thisOperator.Contains("("))
                                output += " ( ";
                        }

                        if (value.Operator.Contains("L"))
                        {
                            var timerData = ValueBool(value);
                            var timerFromDB = 0;
                            //Проверим на таймер в бд
                            if (value.AEM.Contains("DB") && DB.ContainsKey(value.Bit)) 
                            {
                                timerData = DB[value.Bit].ToLower();
                                if (timerData.Contains("s5t"))//То это таймер, иначе число
                                    timerFromDB = 1;
                            }
                            if (value.AEM.Contains("S5T") || timerData.Contains("s5t") || timerFromDB == 1) //Если таймер
                            {
                                if (i == 2793)
                                { }

                                if (timerData == "0")
                                {
                                    timerData = value.AEM.ToLower();
                                }
                                var temp = processor.Solve<BooleanResult>(output).Result;
                                ProgramData valueNext = DataGridTable[i + 1] as ProgramData;
                                if (valueNext.Operator.Contains("SE"))
                                {
                                    i = i + 1;

                                    if (temp == false) //Обнуляем таймер если SE 
                                    {
                                        if (TimerSE.Keys.Contains(valueNext.Bit.ToString()))
                                        {
                                            TimerSE.Remove(valueNext.Bit.ToString());
                                            MyTimers valueTime = TimerGridTable.Where(u => u.Address == valueNext.Bit).SingleOrDefault() as MyTimers;
                                            valueTime.Time = 0;
                                            //valueTime.EndTime = 0;
                                            valueTime.Value = 0;
                                        }
                                        //break;
                                    }
                                    else //Создаем новый таймер если SE 
                                    {
                                        if(valueNext.Bit=="4")
                                        { }
                                        if (!TimerSE.Keys.Contains(valueNext.Bit.ToString()))
                                        {
                                            var tempTimerData = timerData.Split('#');
                                            string tempTime;
                                            int newTempTime;
                                            if (tempTimerData[1].Contains("ms"))
                                            {
                                                tempTime = tempTimerData[1].Replace("ms", "");
                                                newTempTime = Convert.ToInt32(tempTime);
                                            }
                                            else
                                            {
                                                tempTime = tempTimerData[1].Replace("s", "");
                                                newTempTime = Convert.ToInt32(tempTime);
                                                newTempTime = newTempTime * 1000;
                                            }
                                            TimerSE.Add(valueNext.Bit, newTempTime);
                                            var containsTimer = TimerGridTable.Where(u => u.Address == valueNext.Bit).SingleOrDefault();
                                            if (containsTimer == null)
                                            {
                                                MyTimers valueTime = new MyTimers(valueNext.Bit, 0, newTempTime, 0);
                                                TimerGridTable.Add(valueTime);
                                            }
                                            else
                                            {
                                                containsTimer.EndTime = newTempTime;
                                                containsTimer.Time = 0;
                                                containsTimer.Value = 0;
                                            }
                                        }
                                        else
                                        {
                                            //var containsTimer = TimerGridTable.Where(u => u.Address == valueNext.Bit).SingleOrDefault();
                                            //if (containsTimer != null && containsTimer.Value ==1)
                                            //    TimerSE.Remove(valueNext.Bit.ToString());

                                            //break;
                                        }
                                    }
                                }
                                else if (valueNext.Operator.Contains("SA")) //если SA то наоборот запускаем
                                {
                                    i = i + 1;

                                    if (temp == true) //Обнуляем таймер если SА тут обнуление это 1
                                    {

                                        if (!TimerSA.Keys.Contains(valueNext.Bit.ToString()))
                                        {
                                            var tempTimerData = timerData.Split('#');
                                            string tempTime;
                                            int newTempTime;
                                            if (tempTimerData[1].Contains("ms"))
                                            {
                                                tempTime = tempTimerData[1].Replace("ms", "");
                                                newTempTime = Convert.ToInt32(tempTime);
                                            }
                                            else
                                            {
                                                tempTime = tempTimerData[1].Replace("s", "");
                                                newTempTime = Convert.ToInt32(tempTime);
                                                newTempTime = newTempTime * 1000;
                                            }
                                            TimerSA.Add(valueNext.Bit, newTempTime);
                                            var containsTimer = TimerGridTable.Where(u => u.Address == valueNext.Bit).SingleOrDefault();
                                            if (containsTimer == null)
                                            {
                                                MyTimers valueTime = new MyTimers(valueNext.Bit, -1, newTempTime, 1);
                                                TimerGridTable.Add(valueTime);
                                            }
                                            else
                                            {
                                                containsTimer.EndTime = newTempTime;
                                                containsTimer.Time = -1;
                                                containsTimer.Value = 1;
                                            }
                                        }
                                        else
                                        {
                                            var containsTimer = TimerGridTable.Where(u => u.Address == valueNext.Bit).SingleOrDefault();
                                            if (containsTimer != null)
                                            {
                                                containsTimer.Time = -1;
                                                containsTimer.Value = 1;
                                            }

                                        }
                                        //break;
                                    }
                                    else
                                    {
                                        if (TimerSA.Keys.Contains(valueNext.Bit.ToString()))
                                        {
                                            var containsTimer = TimerGridTable.Where(u => u.Address == valueNext.Bit).SingleOrDefault();
                                            if (containsTimer.Time == -1)
                                                containsTimer.Time = 0;//Т.к. SA запускается если с 1 стало 0, то мы из таймеров SA должны скопировать в таймеры SE, и потом проверять когда кончится время, то удалить из таймеров SE 
                                           // TimerSE.Add(valueNext.Bit, TimerSA[valueNext.Bit]);
                                        }
                                        //break;
                                    }
                                }
                            }
                            else if (compareValues.Count == 0) //Если сравнение
                            {
                                var temp = output.Trim();
                                temp = temp.Remove(temp.Length - 1, 1);
                                temp = temp.Trim();
                                if (!string.IsNullOrEmpty(temp))
                                {
                                    temp = temp.Substring(0, temp.LastIndexOf(' '));

                                    var tempValue = processor.Solve<BooleanResult>(temp).Result;
                                    if (tempValue == false)
                                        i = i +2;
                                        //break;
                                }
                            }
                            var currentInt = ValueBool(value);
                            if (!currentInt.ToLower().Contains("s5t"))
                                compareValues.Add(Convert.ToInt32(currentInt));
                        }

                        if (value.Operator.Contains("=="))
                        {
                            if (compareValues[0] == compareValues[1])
                                output += "true";
                            else
                                output += "false";
                            //break;
                        }
                        else if (value.Operator.Contains("<>"))
                        {
                            if (compareValues[0] != compareValues[1])
                                output += "true";
                            else
                                output += "false";
                            //break;
                        }
                        else if (value.Operator.Contains("<"))
                        {
                            if (compareValues[0] < compareValues[1])
                                output += "true";
                            else
                                output += "false";
                            //break;
                        }
                        else if (value.Operator.Contains(">"))
                        {
                            if (compareValues[0] > compareValues[1])
                                output += "true";
                            else
                                output += "false";
                            //break;
                        }
                        else if (value.Operator.Contains("+"))
                        {
                            var temp = compareValues[0] + compareValues[1];
                            compareValues[0] = temp;
                        }
                        else if (value.Operator.Contains("-"))
                        {
                            var temp = compareValues[0] - compareValues[1];
                            compareValues[0] = temp;
                        }
                        if (value.Operator.Contains("T"))
                        {
                            if (DB[value.Bit] == "18")
                            { }

                            DB[value.Bit] = compareValues[0].ToString();
                        }

                        if (value.Operator.Contains("SPBNB")) //Типа goto
                        {
                            bool tempValue;
                            if (!string.IsNullOrEmpty(output))
                                tempValue = processor.Solve<BooleanResult>(output).Result;
                            else
                                tempValue = false;

                            if (tempValue) //если перед нами 1 то идем сюда
                            {
                                ProgramData valueNext = DataGridTable[i + 1] as ProgramData;
                                if(valueNext.Key==2293)
                                { }
                                var valueToNext = ValueBool(valueNext);
                                ProgramData valueNext2 = DataGridTable[i + 2] as ProgramData;
                                var memory = MemoryGridTable.Find(u => u.Address == valueNext2.Bit);
                                memory.CurrentValue = valueToNext.ToUpper();
                                DB[valueNext2.Bit] = memory.CurrentValue;

                                if (memory.CurrentValue == "-1")
                                { }
                            }
                            //если нет, то перескакиваем. считаем сколько перескачить
                            int count = 0;
                            for (int j = i; j <= item.Value; j++)
                            {
                                count++;
                                ProgramData valueNext = DataGridTable[j + 1] as ProgramData;
                                if (valueNext.Operator == value.AEM + ":")
                                {
                                    i = i + count;
                                    break;
                                }
                            }
                            //break;
                        }
                        else if (value.Operator.Contains("S"))
                        {
                            bool tempValue;
                            if (!string.IsNullOrEmpty(output))
                                tempValue = processor.Solve<BooleanResult>(output).Result;
                            else
                                tempValue = false;
                            if (tempValue)
                                DataWrite(value, "true");
                            else
                                ValueBool(value);

                            //Смотрим сл. строку, если там R или S то не обнуляем output
                            ProgramData valueNext = DataGridTable[i + 1] as ProgramData;
                            if (!valueNext.Operator.Contains("R"))
                                if (!valueNext.Operator.Contains("S"))
                                    output = "";
                        }
                        else if (value.Operator.Contains("R"))
                        {
                            bool tempValue;
                            if (!string.IsNullOrEmpty(output))
                                tempValue = processor.Solve<BooleanResult>(output).Result;
                            else
                                tempValue = false;
                            if (tempValue)
                                DataWrite(value, "false");
                            else
                                ValueBool(value);

                            //Смотрим сл. строку, если там R или S то не обнуляем output
                            ProgramData valueNext = DataGridTable[i + 1] as ProgramData;
                            if (!valueNext.Operator.Contains("R"))
                                if (!valueNext.Operator.Contains("S"))
                                    output = "";
                        }
                        else if (value.Operator.Contains("FP"))
                        {
                            var tempValue = processor.Solve<BooleanResult>(output).Result;
                            if (Convert.ToInt32(tempValue) != FrontP[value.Key.ToString()])
                            {
                                if (FrontP[value.Key.ToString()] == 0)
                                {
                                    FrontP[value.Key.ToString()] = 1;
                                    DataWrite(value, "true");
                                    //break;
                                }
                                else
                                {
                                    DataWrite(value, "false");
                                    if (FrontP[value.Key.ToString()] == 1)
                                        FrontP[value.Key.ToString()] = 0;
                                    //break;
                                    output = "";
                                    int count = 0;
                                    for (int j = i; j <= item.Value; j++)
                                    {
                                        count++;
                                        ProgramData valueNext = DataGridTable[j + 1] as ProgramData;
                                        if (valueNext.Operator == "SPBNB" || valueNext.Operator == "S" || valueNext.Operator == "R" || valueNext.Operator == "=")
                                        {
                                            i = i + count - 1; //чтоб в нее зашло
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {      //Перескакиваем фронт
                                output = "";
                                int count = 0;
                                for (int j = i; j <= item.Value; j++)
                                {
                                    count++;
                                    ProgramData valueNext = DataGridTable[j + 1] as ProgramData;
                                    if (valueNext.Operator == "SPBNB" || valueNext.Operator == "S" || valueNext.Operator == "R" || valueNext.Operator == "=")
                                    {
                                        i = i + count - 1; //чтоб в нее зашло
                                        break;
                                    }
                                }
                                //i = i + 1;
                                //break;
                            }
                        }
                        else if (value.Operator.Contains("FN"))
                        {
                            var tempValue = processor.Solve<BooleanResult>(output).Result;
                            if (Convert.ToInt32(tempValue) != FrontN[value.Key.ToString()])
                            {
                                if (Convert.ToInt32(tempValue) == 1)
                                //if (FrontN[value.Key.ToString()] == 0)
                                {
                                    FrontN[value.Key.ToString()] = 1;
                                    //DataWrite(value, "true");
                                    //break;
                                    output = "";
                                    int count = 0;
                                    for (int j = i; j <= item.Value; j++)
                                    {
                                        count++;
                                        ProgramData valueNext = DataGridTable[j + 1] as ProgramData;
                                        if (valueNext.Operator == "SPBNB" || valueNext.Operator == "S" || valueNext.Operator == "R" || valueNext.Operator == "=")
                                        {
                                            i = i + count - 1; //чтоб в нее зашло
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    //DataWrite(value, "false");
                                    if (FrontN[value.Key.ToString()] == 1)
                                    {
                                        DataWrite(value, "true");
                                        FrontN[value.Key.ToString()] = 0;
                                        output = "";
                                    }
                                }
                            }
                            else
                            //Перескакиваем в конец
                            {
                                output = "";
                                int count = 0;
                                for (int j = i; j <= item.Value; j++)
                                {
                                    count++;
                                    ProgramData valueNext = DataGridTable[j + 1] as ProgramData;
                                    if (valueNext.Operator == "SPBNB" || valueNext.Operator == "S" || valueNext.Operator == "R" || valueNext.Operator == "=")
                                    {
                                        i = i + count - 1; //чтоб в нее зашло
                                        break;
                                    }
                                }
                            }
                                //break;
                        }
                        else
                        {
                            if (!thisOperator.Contains("("))
                                if (!value.Operator.Contains(")"))
                                    if (!value.Operator.Contains("L"))
                                        if (!value.Operator.Contains("="))
                                            if (!value.Operator.Contains("<>"))
                                                if (!value.Operator.Contains("<"))
                                                    if (!value.Operator.Contains(">"))
                                                        output += ValueBool(value);
                        }
                    }
                }
            }
            if (!backgroundWorker.IsBusy)
                backgroundWorker.RunWorkerAsync();

            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
            (ThreadStart)delegate ()
            {
                dataGrid.Items.Refresh();
            }
            );
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                foreach (var item in TimerSE)
                {
                    var value = TimerGridTable.Where(u => u.Address == item.Key).SingleOrDefault();
                    if (value.Time < value.EndTime && value.Value != 1)
                        value.Time += 100;
                    else
                    {
                        //if (!TimerSA.Keys.Contains(item.Key))
                            value.Value = 1; //Для SE таймера
                        //else
                        //{
                        //    TimerSA.Remove(item.Key);
                        //    value.Value = 0; //Для SA таймера
                        //}
                        value.Time = 0;
                        //TimerSE.Remove(item.Key);
                        //break;
                    }
                }
                foreach (var item in TimerSA)
                {
                    var value = TimerGridTable.Where(u => u.Address == item.Key).SingleOrDefault();
                    if (value.Time < value.EndTime && value.Time != -1 && value.Value != 0)
                        value.Time += 100;
                    else if (value.Time == value.EndTime)
                    {

                        value.Value = 0; //Для SA таймера

                        value.Time = 0;
                        TimerSA.Remove(item.Key);
                    }
                }
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    timerGrid.Items.Refresh();
                }
                );
            }
            catch 
            {
                return;
            }
        }
        #region Вспомогательные функции
        private void showDBTimer(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            switch (button.Name)
            {
                case "button_DB":
                    button_Timer.Content = "Показать таймеры";
                    timerGrid.Visibility = Visibility.Collapsed;
                    dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    button_testWindow.Content = "Показать";
                    testWindow.Visibility = Visibility.Collapsed;
                    dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    if (memoryGrid.Visibility != Visibility.Visible)
                    {
                        button_DB.Content = "Скрыть память";
                        memoryGrid.Visibility = Visibility.Visible;
                        memoryGrid.Height = 300;
                        dataGrid.Margin = new Thickness(10, 350, 10, 10);
                    }
                    else
                    {
                        button_DB.Content = "Показать память";
                        memoryGrid.Visibility = Visibility.Collapsed;
                        dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    }
                    break;
                case "button_Timer":
                    button_DB.Content = "Показать память";
                    memoryGrid.Visibility = Visibility.Collapsed;
                    dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    button_testWindow.Content = "Показать";
                    testWindow.Visibility = Visibility.Collapsed;
                    dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    if (timerGrid.Visibility != Visibility.Visible)
                    {
                        button_Timer.Content = "Скрыть таймеры";
                        timerGrid.Visibility = Visibility.Visible;
                        timerGrid.Height = timerGrid.Items.Count * 20 + 100;
                        dataGrid.Margin = new Thickness(10, timerGrid.Height + 50, 10, 10);
                    }
                    else
                    {
                        button_Timer.Content = "Показать таймеры";
                        timerGrid.Visibility = Visibility.Collapsed;
                        dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    }
                    break;
                case "button_testWindow":
                    button_Timer.Content = "Показать таймеры";
                    timerGrid.Visibility = Visibility.Collapsed;
                    dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    button_DB.Content = "Показать память";
                    memoryGrid.Visibility = Visibility.Collapsed;
                    dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    if (testWindow.Visibility != Visibility.Visible)
                    {
                        button_testWindow.Content = "Скрыть";
                        testWindow.Visibility = Visibility.Visible;
                        dataGrid.Margin = new Thickness(10, 250, 10, 10);
                    }
                    else
                    {
                        button_testWindow.Content = "Показать";
                        testWindow.Visibility = Visibility.Collapsed;
                        dataGrid.Margin = new Thickness(10, 35, 10, 10);
                    }
                    break;
            }
        }
        private static string ReverseString(string s)
        {
            char[] value = s.ToCharArray();
            Array.Reverse(value);
            return new string(value);
        }
        private bool DataRead(string byteAndBit, string path)
        {
            var address = byteAndBit.Split('.');
            var count = 0;//-1
            int value;
            switch (path)
            {
                case "input":
                    value = InputData[Convert.ToInt32(address[0])]; 
                    break;
                case "marker":
                    value = MarkerData[Convert.ToInt32(address[0])];
                    break;
                default: //output
                    value = OutputData[Convert.ToInt32(address[0])];
                    break;
            }
            //using (StreamReader fs = new StreamReader(path, Encoding.Default))
            //{
            //    int skip = Convert.ToInt32(address[0].ToString());
            //    while (count != skip)
            //    {
            //        value = fs.ReadLine();
            //        count++;
            //    }
            //}
            //var bits = Convert.ToString(Convert.ToInt32(value), 2);
            var bits = Convert.ToString(value, 2);
            while (bits.Length < 8)
                bits = bits.Insert(0, "0");
            bits = ReverseString(bits);
            //count = 0;
            foreach (char ch in bits)
            {
                if (count.ToString() == address[1])
                {
                    var tempString = ch.ToString();
                    var tempInt = Convert.ToInt32(tempString);
                    var tempBool = Convert.ToBoolean(tempInt);
                    return tempBool;
                }
                count++;
            }
            return false;
        }
        private bool DataWrite(ProgramData value, string output)
        {
            bool valueBool;
            try
            {
                valueBool = processor.Solve<BooleanResult>(output).Result;
            }
            catch
            {
                //МБ это уже и не нужно, не помню
                output = output.TrimEnd();
                output = output.Substring(0, output.LastIndexOf(' '));
                valueBool = processor.Solve<BooleanResult>(output).Result;
            }
            string path = "";

            if (value.AEM.Contains("A"))
            {
                path = "output";
                value.Output = Convert.ToInt32(valueBool).ToString();
            }
            if (value.AEM.Contains("M"))
            {
                path = "marker";
                value.Marker = Convert.ToInt32(valueBool).ToString();
            }
            if (value.AEM.Contains("E"))
            {
                path = "input";
                value.Input = Convert.ToInt32(valueBool).ToString();
            }
            if (value.AEM.Contains("DB"))
            {
                //Проверяем есть ли такое значение адреса в БД, если нет то это младший байт числа в другом адресе
                if (DB.ContainsKey(value.Bit))
                {
                    if (DB[value.Bit] == "18")
                    { }

                    DB[value.Bit] = valueBool.ToString(); //Записываем пока только булевые

                }
                else
                {
                    var split = value.Bit.Split('.');
                    var olderByte = Convert.ToInt32(split[0]) - 1;
                    var valueOlderByte = Convert.ToString(Convert.ToInt32(DB[olderByte.ToString()]), 2);
                    while (valueOlderByte.Length < 8)
                        valueOlderByte = valueOlderByte.Insert(0, "0");
                    valueOlderByte = ReverseString(valueOlderByte);
                    valueOlderByte = valueOlderByte.Remove(Convert.ToInt16(split[1]), 1);
                    valueOlderByte = valueOlderByte.Insert(Convert.ToInt16(split[1]), Convert.ToInt16(valueBool).ToString());
                    valueOlderByte = ReverseString(valueOlderByte);
                    valueOlderByte = Convert.ToByte(valueOlderByte, 2).ToString();
                    DB[olderByte.ToString()] = valueOlderByte;
                    var memory2 = MemoryGridTable.Find(u => u.Address == olderByte.ToString());
                    memory2.CurrentValue = valueOlderByte.ToLower();
                    if (memory2.CurrentValue == "-1")
                    { }
                    return true;
                }
                value.Output = Convert.ToInt32(valueBool).ToString();
                var memory = MemoryGridTable.Find(u => u.Address == value.Bit);
                if (memory == null)
                {
                    var tempAddress = value.Bit.Split('.');
                    memory = MemoryGridTable.Find(u => u.Address == tempAddress[0]);
                    var tempBits = Convert.ToString(Convert.ToInt32(memory.CurrentValue), 2);
                    while (tempBits.Length < 8)
                        tempBits = tempBits.Insert(0, "0");
                    tempBits = ReverseString(tempBits);
                    tempBits = tempBits.Remove(Convert.ToInt16(tempAddress[1]), 1);
                    tempBits = tempBits.Insert(Convert.ToInt16(tempAddress[1]), value.Output);
                    tempBits = ReverseString(tempBits);
                    memory.CurrentValue = Convert.ToByte(tempBits, 2).ToString();
                    DB[tempAddress[0]] = memory.CurrentValue;
                    if (memory.CurrentValue == "-1")
                    { }//memory.Value = memory.CurrentValue;
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (ThreadStart)delegate ()
                    {
                        memoryGrid.Items.Refresh();
                    }
                    );
                    return true;
                }
                memory.CurrentValue = valueBool.ToString().ToLower();
                DB[value.Bit] = memory.CurrentValue;
                return true;
            }

            var address = value.Bit.Split('.');
            //var count = 0;
            int valueTemp;


            switch (path)
            {
                case "input":
                    valueTemp = InputData[Convert.ToInt32(address[0])];
                    break;
                case "marker":
                    valueTemp = MarkerData[Convert.ToInt32(address[0])];
                    break;
                default: //output
                    valueTemp = OutputData[Convert.ToInt32(address[0])];
                    break;
            }



            //var outputTextFirst = new List<string>();
            //var outputTextSecond = new List<string>();
            //using (StreamReader fs = new StreamReader(path, Encoding.Default))
            //{
            //    int skip = Convert.ToInt32(address[0].ToString());
            //    while (true)
            //    {
            //        string temp = fs.ReadLine();
            //        if (temp == null) break;
            //        if (count <= skip)
            //        {
            //            if (count == skip)
            //                valueTemp = temp;
            //            else
            //                outputTextFirst.Add(temp);
            //            count++;
            //        }
            //        else
            //            outputTextSecond.Add(temp);
            //    }
            //}
            //var bits = Convert.ToString(Convert.ToInt32(valueTemp), 2);
            var bits = Convert.ToString(valueTemp, 2);
            while (bits.Length < 8)
                bits = bits.Insert(0, "0");
            bits = ReverseString(bits);
            bits = bits.Remove(Convert.ToInt16(address[1]), 1);
            bits = bits.Insert(Convert.ToInt16(address[1]), Convert.ToInt16(valueBool).ToString());
            bits = ReverseString(bits);

            var byteToSave = Convert.ToByte(bits, 2);
            switch (path)
            {
                case "input":
                    InputData[Convert.ToInt32(address[0])]= byteToSave;
                    break;
                case "marker":
                    MarkerData[Convert.ToInt32(address[0])] = byteToSave;
                    break;
                default: //output
                    OutputData[Convert.ToInt32(address[0])] = byteToSave;
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (ThreadStart)delegate ()
                    {
                        Output1Byte(OutputData[0]);
                        Output2Byte(OutputData[1]);
                        Output3Byte(OutputData[2]);
                    }
                    );
                    break;
            }
            //using (StreamWriter fs = new StreamWriter(path))
            //{
            //    string outputText = "";
            //    foreach (var inputByte in outputTextFirst)
            //        outputText += inputByte + "\n";
            //    outputText += byteToSave + "\n";
            //    foreach (var inputByte in outputTextSecond)
            //        outputText += inputByte + "\n";
            //    outputText = outputText.Substring(0, outputText.Length - 1);
            //    fs.WriteLineAsync(outputText);
            //}
            return true;
        }
        private string ValueBool(ProgramData value)
        {
            var valueBool = false;

            if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("E"))
            {
                valueBool = DataRead(value.Bit, "input");
                value.Input = Convert.ToInt32(valueBool).ToString();
            }
            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("M") && !value.AEM.Contains("MS") && !value.AEM.Contains("ms"))
            {
                valueBool = DataRead(value.Bit, "marker");
                value.Marker = Convert.ToInt32(valueBool).ToString();
            }
            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("A"))
            {
                valueBool = DataRead(value.Bit, "output");
                value.Output = Convert.ToInt32(valueBool).ToString(); //Стоял input
            }
            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("DB"))
            {
                string tempValue;
                if (DB.ContainsKey(value.Bit))
                {
                    tempValue = DB[value.Bit].ToLower();
                }
                else
                {
                    var split = value.Bit.Split('.');
                    var olderByte = Convert.ToInt32(split[0]) - 1;
                    var valueOlderByte = Convert.ToString(Convert.ToInt32(DB[olderByte.ToString()]), 2);
                    while (valueOlderByte.Length < 8)
                        valueOlderByte = valueOlderByte.Insert(0, "0");
                    valueOlderByte = ReverseString(valueOlderByte);
                    valueOlderByte = valueOlderByte.Substring(Convert.ToInt16(split[1]), 1);
                    if (valueOlderByte == "0")
                        tempValue = "false";
                    else
                        tempValue = "true";
                }

                if (tempValue.Contains("true") || tempValue.Contains("false")) //Пока только булевые из БД
                    valueBool = Convert.ToBoolean(tempValue);
                else
                {
                    //if (!tempValue.Contains("s5t")) // Если не таймер то числа
                        return tempValue;
                    //else
                       // return "-1";
                }
                value.Input = Convert.ToInt32(valueBool).ToString();
            }
            else if (!string.IsNullOrEmpty(value.AEM) && value.AEM.Contains("T") && value.Bit.Length > 0)
            {
                var containsTimer = TimerGridTable.Where(u => u.Address == value.Bit).SingleOrDefault();
                if (containsTimer != null)
                    valueBool = Convert.ToBoolean(Convert.ToInt32(containsTimer.Value));
                else
                    valueBool = false;
            }
            else if (!string.IsNullOrEmpty(value.AEM))
            {
                //Значит тут наверно число
                var valueInt = value.AEM;
                int result;
                int.TryParse(valueInt, out result);
                return result.ToString();
            }
            //Проверяем на негатив
            if (value.Operator.Substring(value.Operator.Length - 1, 1) == "N") //не забыть что SPBNB не учитывается как негатив
                valueBool = processor.Solve<BooleanResult>("~" + valueBool).Result;

            return valueBool.ToString();
        }
        private void Output1Byte(int value)
        {
            var tempBits = Convert.ToString(value, 2);
            while (tempBits.Length < 8)
                tempBits = tempBits.Insert(0, "0");
            tempBits = ReverseString(tempBits);
            if (tempBits[0] != '0')
                Output1Bit0.IsChecked = true;
            else
                Output1Bit0.IsChecked = false;
            if (tempBits[1] != '0')
                Output1Bit1.IsChecked = true;
            else
                Output1Bit1.IsChecked = false;
            if (tempBits[2] != '0')
                Output1Bit2.IsChecked = true;
            else
                Output1Bit2.IsChecked = false;
            if (tempBits[3] != '0')
                Output1Bit3.IsChecked = true;
            else
                Output1Bit3.IsChecked = false;
            if (tempBits[4] != '0')
                Output1Bit4.IsChecked = true;
            else
                Output1Bit4.IsChecked = false;
            if (tempBits[5] != '0')
                Output1Bit5.IsChecked = true;
            else
                Output1Bit5.IsChecked = false;
            if (tempBits[6] != '0')
                Output1Bit6.IsChecked = true;
            else
                Output1Bit6.IsChecked = false;
            if (tempBits[7] != '0')
                Output1Bit7.IsChecked = true;
            else
                Output1Bit7.IsChecked = false;
        }
        private void Output2Byte(int value)
        {
            var tempBits = Convert.ToString(value, 2);
            while (tempBits.Length < 8)
                tempBits = tempBits.Insert(0, "0");
            tempBits = ReverseString(tempBits);
            if (tempBits[0] != '0')
                Output2Bit0.IsChecked = true;
            else
                Output2Bit0.IsChecked = false;
            if (tempBits[1] != '0')
                Output2Bit1.IsChecked = true;
            else
                Output2Bit1.IsChecked = false;
            if (tempBits[2] != '0')
                Output2Bit2.IsChecked = true;
            else
                Output2Bit2.IsChecked = false;
            if (tempBits[3] != '0')
                Output2Bit3.IsChecked = true;
            else
                Output2Bit3.IsChecked = false;
            if (tempBits[4] != '0')
                Output2Bit4.IsChecked = true;
            else
                Output2Bit4.IsChecked = false;
            if (tempBits[5] != '0')
                Output2Bit5.IsChecked = true;
            else
                Output2Bit5.IsChecked = false;
            if (tempBits[6] != '0')
                Output2Bit6.IsChecked = true;
            else
                Output2Bit6.IsChecked = false;
            if (tempBits[7] != '0')
                Output2Bit7.IsChecked = true;
            else
                Output2Bit7.IsChecked = false;
        }
        private void Output3Byte(int value)
        {
            var tempBits = Convert.ToString(value, 2);
            while (tempBits.Length < 8)
                tempBits = tempBits.Insert(0, "0");
            tempBits = ReverseString(tempBits);
            if (tempBits[0] != '0')
                Output3Bit0.IsChecked = true;
            else
                Output3Bit0.IsChecked = false;
            if (tempBits[1] != '0')
                Output3Bit1.IsChecked = true;
            else
                Output3Bit1.IsChecked = false;
            if (tempBits[2] != '0')
                Output3Bit2.IsChecked = true;
            else
                Output3Bit2.IsChecked = false;
            if (tempBits[3] != '0')
                Output3Bit3.IsChecked = true;
            else
                Output3Bit3.IsChecked = false;
            if (tempBits[4] != '0')
                Output3Bit4.IsChecked = true;
            else
                Output3Bit4.IsChecked = false;
            if (tempBits[5] != '0')
                Output3Bit5.IsChecked = true;
            else
                Output3Bit5.IsChecked = false;
            if (tempBits[6] != '0')
                Output3Bit6.IsChecked = true;
            else
                Output3Bit6.IsChecked = false;
            if (tempBits[7] != '0')
                Output3Bit7.IsChecked = true;
            else
                Output3Bit7.IsChecked = false;
        }
        private void InputBit_Checked(object sender, RoutedEventArgs e)
        {
            var value = sender as CheckBox;
            int editBit;
            var temp = value.Name.Replace("Input", "");
            temp = temp.Replace("Bit", "");
            var adress = (int)Char.GetNumericValue(temp[0]) - 1;
            int InputBit = (int)Char.GetNumericValue(temp[1]);
            int InputByte = InputData[adress];

            if (value.IsChecked == true)
                editBit = 1;
            else
                editBit = 0;

            var bits = Convert.ToString(InputByte, 2);
            while (bits.Length < 8)
                bits = bits.Insert(0, "0");
            bits = ReverseString(bits);
            bits = bits.Remove(InputBit, 1);
            bits = bits.Insert(InputBit, editBit.ToString());
            bits = ReverseString(bits);

            var byteToSave = Convert.ToByte(bits, 2);
            InputData[adress] = byteToSave;
        }
        #endregion
    }
}
