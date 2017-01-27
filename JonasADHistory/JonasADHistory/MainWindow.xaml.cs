using System;
using System.Reflection;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace JonasSalesHistory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string SearchBoxBinding = "";

        DataTable JonasHistoryDataTable;
        DataTable BVHistoryDetail;
        DataTable BVHistoryHighLevel;

        DataTable CombinedTable;

        DataTable DisplayTable;

        string customerSearch;
        string partNameSearch;
        string partNoSearch;

        HashSet<TransactionRecord> transactionRecords = new HashSet<TransactionRecord>();

        public MainWindow()
        {
            InitializeComponent();

            customerSearch = "";
            partNameSearch = "";

            LoadData();

            CustomerNameSearchBox.TextChanged += SearchChanged;
            PartNameSearchBox.TextChanged += SearchChanged;
            PartNumberSearchBox.TextChanged += SearchChanged;

            MainData.MouseDoubleClick +=  MainData_MouseDoubleClick;
            MainData.SelectionChanged += MainData_SelectionChanged;
        }

        void MainData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (MainData.SelectedItem == null) return;

            DataRowView dataRow = (DataRowView)MainData.SelectedItem;
            int index = MainData.CurrentCell.Column.DisplayIndex;

            string searchString = dataRow.Row.ItemArray[index].ToString();

            if (index == 2)
            {
                // find the customer info for this customer...
                var customerInfo = from row in BVHistoryHighLevel.AsEnumerable()
                                   where row.Field<string>("Name") == searchString
                                   select row;

                ContextualData.ItemsSource = customerInfo.CopyToDataTable().DefaultView;
            }
            else if (index == 0)
            {
                // display invoice details...
                var invoiceInfo = from row in BVHistoryDetail.AsEnumerable()
                                  where row[0] == searchString
                                  select row;

                ContextualData.ItemsSource = invoiceInfo.CopyToDataTable().DefaultView;
            }
        }

        void MainData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // what did the user click?
           
            DataRowView dataRow = (DataRowView)MainData.SelectedItem;
            int index = MainData.CurrentCell.Column.DisplayIndex;

            if (index == 0)
            {
                
            }
            if (index == 1)
            {

            }
            if (index == 2)
            {
                CustomerNameSearchBox.Text = dataRow.Row.ItemArray[index].ToString();

                // find the customer info for this customer...
                var customerInfo = from row in BVHistoryHighLevel.AsEnumerable()
                                   where row.Field<string>("Name") == dataRow.Row.ItemArray[index].ToString()
                                   select row;

                ContextualData.ItemsSource = customerInfo.CopyToDataTable().DefaultView;

            }
            if (index == 3)
            {
                PartNumberSearchBox.Text = dataRow.Row.ItemArray[index].ToString();
            }
            if (index == 4){
                PartNameSearchBox.Text = dataRow.Row.ItemArray[index].ToString();
            }

        }

        void SearchChanged(object sender, TextChangedEventArgs e)
        {
            customerSearch = CustomerNameSearchBox.Text;
            partNoSearch = PartNumberSearchBox.Text;
            partNameSearch = PartNameSearchBox.Text;

            UpdateQuery();
        }

        void UpdateQuery()
        {

            //if (partSearch == null) partSearch = "";
            //if (customerSearch == null) customerSearch = "";

            //var result = from tr in transactionrecords
            //             where tr.customernumber != null
            //             where tr.partdescription.indexof(partsearch, stringcomparison.ordinalignorecase) >= 0
            //             where tr.customername.indexof(customersearch, stringcomparison.ordinalignorecase) >= 0
            //             select tr;

            //HashSet<TransactionRecord> hs = new HashSet<TransactionRecord>(result);
            
            // search customer records...
            //if (customerSearch == "" && partSearch == "")
            //{
            //    MainData.ItemsSource = CombinedTable.DefaultView;
            //}

            //else if (customerSearch != "" && partSearch == "" && partNoSearch == "")
            //{
            //    var result = from row in CombinedTable.AsEnumerable()
            //                 where row.Field<string>("CustomerName").IndexOf(customerSearch, StringComparison.OrdinalIgnoreCase) > -1
            //                 //&& row.Field<string>("PartDescription").IndexOf(partSearch, StringComparison.CurrentCultureIgnoreCase) > 0
            //                 select row;

            //    DisplayTable = new DataTable();

            //    if (result.Count() > 0)
            //    {
            //        DisplayTable = result.CopyToDataTable();
            //    }
                

            //}
            //else if (partSearch != "" && customerSearch == ""){
            //    var result = from row in CombinedTable.AsEnumerable()
            //                 // row.Field<string>("CustomerName").IndexOf(customerSearch, StringComparison.OrdinalIgnoreCase) > -1
            //                 where row.Field<string>("PartDescription").IndexOf(partSearch, StringComparison.OrdinalIgnoreCase) > -1
            //                 select row;

            //    DisplayTable = new DataTable();

            //    if (result.Count() > 0)
            //    {
            //        DisplayTable = result.CopyToDataTable();
            //    }
            //}
            //else
            //{
                var result = from row in CombinedTable.AsEnumerable()
                             where row.Field<string>("CustomerName").IndexOf(customerSearch, StringComparison.OrdinalIgnoreCase) > -1
                             where row.Field<string>("PartDescription").IndexOf(partNameSearch, StringComparison.OrdinalIgnoreCase) > -1
                             where row.Field<string>("PartNo").IndexOf(partNoSearch, StringComparison.OrdinalIgnoreCase) > -1
                             select row;

                DisplayTable = new DataTable();

                if (result.Count() > 0)
                {
                    DisplayTable = result.CopyToDataTable();
                }
            //}
           

            MainData.ItemsSource = DisplayTable.DefaultView;

        }

        void LoadData()
        {

            // load the raw data
            CsvReader reader = new CsvReader("..\\..\\JonasTransLog.csv");
            JonasHistoryDataTable = reader.Table;

            //reader = new CsvReader("..\\..\\..\\SalesHist.xls - Sales History_Dtl.csv");
            //BVHistoryDetail = reader.Table;
            BVHistoryDetail = ConvertCSVtoDataTable("..\\..\\SalesHist.xls - Sales History_Dtl.csv");

            reader = new CsvReader("..\\..\\SalesHist.xls - Sales History_Hdr.csv");
            BVHistoryHighLevel = reader.Table;

            DataSet mainSet = new DataSet();

            // generate transaction records...
            TransactionRecord tempRecord;
            foreach (DataRow row in BVHistoryDetail.Rows)
            {
                tempRecord = new TransactionRecord();
                tempRecord.invoiceNumber = row[0] as string;

                // find invoice row in bvhighlevel
                DataRow found = BVHistoryHighLevel.AsEnumerable().Where(x => x.Field<string>("Invoice No.") == row.Field<string>("Invoice No.")).FirstOrDefault();

                // can't do anything if there's no invoice...
                if (found != null)
                {
                    tempRecord.customerNumber = found[2] as string;
                    tempRecord.customerName = found[1] as string;
                }

                //tempRecord.customerNumber = ;

                tempRecord.partNumber = row[2] as string;
                tempRecord.partDescription = row[3] as string;
                tempRecord.invoiceDate = DateTime.Now;

                transactionRecords.Add(tempRecord);
            }

            // make new datatable with the info we want...
            DataTable fullTable = new DataTable();

            var q = (from detail in BVHistoryDetail.AsEnumerable()
                     join highLevel in BVHistoryHighLevel.AsEnumerable()
                     on detail.Field<string>("Invoice No.") equals highLevel.Field<string>("Invoice No.")
                     select new
                     {
                         InvoiceNo = detail.Field<string>("Invoice No."),
                         CustomerCode = highLevel.Field<string>("Customer No."),
                         CustomerName = highLevel.Field<string>("Name"),
                         PartNo = detail.Field<string>("Part number"),
                         PartDescription = detail.Field<string>("Part Description"),
                         Quantity = detail.Field<string>("Order qty."),
                         InvoiceDate = highLevel.Field<string>("Invoice Date"),
                     });

            CombinedTable = LINQResultToDataTable(q);


            //DataTable dt = ConvertTRSToDataTable(transactionRecords);

            // final schema:
            // invoice #, part no, part desc, customer no, customer name, quantity, price, date
            MainData.ItemsSource = CombinedTable.DefaultView;
        }

        public DataTable LINQResultToDataTable<T>(IEnumerable<T> Linqlist)
        {
            DataTable dt = new DataTable();


            PropertyInfo[] columns = null;

            if (Linqlist == null) return dt;

            foreach (T Record in Linqlist)
            {

                if (columns == null)
                {
                    columns = ((Type)Record.GetType()).GetProperties();
                    foreach (PropertyInfo GetProperty in columns)
                    {
                        Type colType = GetProperty.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition()
                        == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dt.Columns.Add(new DataColumn(GetProperty.Name, colType));
                    }
                }

                DataRow dr = dt.NewRow();

                foreach (PropertyInfo pinfo in columns)
                {
                    dr[pinfo.Name] = pinfo.GetValue(Record, null) == null ? DBNull.Value : pinfo.GetValue
                    (Record, null);
                }

                dt.Rows.Add(dr);
            }
            return dt;
        }

        //public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        //{
        //    var tb = new DataTable(typeof(T).Name);

        //    PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //    foreach (var prop in props)
        //    {
        //        tb.Columns.Add(prop.Name, prop.PropertyType);
        //    }

        //    foreach (var item in items)
        //    {
        //        var values = new object[props.Length];
        //        for (var i = 0; i < props.Length; i++)
        //        {
        //            values[i] = props[i].GetValue(item, null);
        //        }

        //        tb.Rows.Add(values);
        //    }

        //    return tb;
        //}
        static Dictionary<char, List<int>> tempDic;
        static bool FastContains(string source, string search)
        {
            tempDic = new Dictionary<char, List<int>>();
            source = source.ToLower();
            search = search.ToLower();

            // make dictionary out of source string
            char[] charArray = source.ToCharArray();

            for (int i = 0; i < charArray.Length; i++)
            {
                // add searchable chars to dictionary
                if (!tempDic.ContainsKey(charArray[i])){
                    tempDic.Add(charArray[i], new List<int>());
                    tempDic[charArray[i]].Add(i);
                }
                else tempDic[charArray[i]].Add(i);
            }

            // loop through search string to see if chars show up in dictionary
            char[] searchArray = search.ToCharArray();
            List<int> prevIndexes = new List<int>();
            for (int j = 0; j < searchArray.Length; j++)
            {
                if (j == 0)
                {
                    if (tempDic.ContainsKey(searchArray[j]))
                    {
                        prevIndexes = tempDic[searchArray[j]];
                        continue;
                    }
                    else return false;
                }
                else
                {
                    char tempChar = searchArray[j];
                    if (tempDic.ContainsKey(tempChar)){

                        // does this character occur after the previous character?
                        bool containsNext = ListContainsNext(prevIndexes, tempDic[tempChar]);

                        return containsNext;

                        //bool passFlag = true;
                        //foreach (var prevIndex in prevIndexes)
                        //{
                        //    if (tempDic[tempChar].Contains(prevIndex + 1))
                        //    {
                        //        prevIndexes = tempDic[tempChar];
                        //    }
                            
                        //}

                        //if (passFlag == false) return false;

                        //int k = 0;
                        //while (k < j){
                        //    if (prevIndexes.Contains(j - 1))
                        //    {
                        //        prevIndexes = tempDic[tempChar];
                        //        continue;
                        //    }
                        //}
                    }
                    
                    return false;
                    
                }               
            }

            return true;
        }

        public static bool ListContainsNext(List<int> prev, List<int> next)
        {

            bool trueFlag = true;
            for(int i = 0; i < next.Count(); i ++){
                // this character is contained in sequence...
                if (trueFlag == true && prev.Contains(next[i] -1)){
                    continue;
                }
                else
                {
                    trueFlag = false;
                }
            }

            return trueFlag;
        }

        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            StreamReader sr = new StreamReader(strFilePath);
            string[] headers = sr.ReadLine().Split(',');
            DataTable dt = new DataTable();
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            while (!sr.EndOfStream)
            {
                string[] rows = System.Text.RegularExpressions.Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        static DataTable ConvertTRSToDataTable(HashSet<TransactionRecord> list)
        {
            // New table.
            DataTable table = new DataTable();

            // number of fields.
            int columns = 5;

            // Add columns.
            table.Columns.Add("Invoice No", typeof(string));
            table.Columns.Add("Customer No", typeof(string));
            table.Columns.Add("Customer Name", typeof(string));
            table.Columns.Add("Part Number", typeof(string));
            table.Columns.Add("Part Description", typeof(string));
            table.Columns.Add("Date", typeof(DateTime));

            // Add rows.
            DataRow dr;
            foreach (var array in list)
            {
                dr = table.NewRow();
                dr[0] = array.invoiceNumber;
                dr[1] = array.customerNumber;
                dr[2] = array.customerName;
                dr[3] = array.partNumber;
                dr[4] = array.partDescription;
                dr[5] = array.invoiceDate;

                table.Rows.Add(dr);
            }

            return table;
        }

        private static DataTable ConvertToDatatable<T>(HashSet<T> data)
        {
            PropertyDescriptorCollection props =
                TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    table.Columns.Add(prop.Name, prop.PropertyType.GetGenericArguments()[0]);
                else
                    table.Columns.Add(prop.Name, prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (T item in data)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item);
                }
                table.Rows.Add(values);
            }
            return table;
        }
    }

    public class TransactionRecord
    {
        public string invoiceNumber;
        public string customerNumber;
        public string customerName;
        public string partNumber;
        public string partDescription;
        public int quantity;
        public DateTime invoiceDate;

    }
}
