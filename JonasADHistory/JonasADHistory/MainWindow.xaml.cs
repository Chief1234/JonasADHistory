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

        DataTable MainTable;

        string customerSearch;
        string partSearch;

        HashSet<TransactionRecord> transactionRecords = new HashSet<TransactionRecord>();

        public MainWindow()
        {
            InitializeComponent();

            customerSearch = "";
            partSearch = "";

            LoadData();

            SearchBox.TextChanged += SearchBox_TextChanged;
            Part_Search.TextChanged += Part_Search_TextChanged;
        }

        void Part_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            partSearch = (sender as TextBox).Text;
            UpdateQuery();
        }

        void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            customerSearch = (sender as TextBox).Text;
            UpdateQuery();
        }

        void UpdateQuery()
        {
            // apply a query to datatable...
            // basic query test
            // //DataView dv = new DataView(BVHistoryHighLevel);
            // //dv.RowFilter = "Name LIKE '%" + customerSearch + "%'";// AND Part Description LIKE '%" + partSearch + "%'";

            // //MainData.ItemsSource = dv;

            // look through hashset...

            if (partSearch == null) partSearch = "";
            if (customerSearch == null) customerSearch = "";

            var result = from tr in transactionRecords
                         where tr.customerNumber != null
                         where tr.partDescription.IndexOf(partSearch, StringComparison.OrdinalIgnoreCase) >= 0
                         where tr.customerName.IndexOf(customerSearch, StringComparison.OrdinalIgnoreCase) >= 0
                         select tr;

            HashSet<TransactionRecord> hs = new HashSet<TransactionRecord>(result);

            // update datagrid...
            DataTable dt = ConvertTRSToDataTable(hs);

            // final schema:
            // invoice #, part no, part desc, customer no, customer name, quantity, price, date
            MainData.ItemsSource = dt.DefaultView;
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
                tempRecord.timeStamp = DateTime.Now;

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

            MainTable = LINQResultToDataTable(q);


            //DataTable dt = ConvertTRSToDataTable(transactionRecords);

            // final schema:
            // invoice #, part no, part desc, customer no, customer name, quantity, price, date
            MainData.ItemsSource = MainTable.DefaultView;
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
                dr[5] = array.timeStamp;

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
        public DateTime timeStamp;
    }
}
