using System;
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

        string customerSearch;
        string partSearch;

        HashSet<TransactionRecord> transactionRecords = new HashSet<TransactionRecord>();

        public MainWindow()
        {
            InitializeComponent();

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
            var result = from tr in transactionRecords
                         where tr.partDescription.Contains(partSearch)
                         where tr.customerName.Contains(customerSearch)
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

            // generate transaction records...
            TransactionRecord tempRecord;
            foreach (DataRow row in BVHistoryDetail.Rows)
            {
                tempRecord = new TransactionRecord();
                tempRecord.invoiceNumber = row[0] as string;

                var results = from myRow in BVHistoryHighLevel.AsEnumerable()
                              where myRow[0] == row[0]
                              select myRow;


                // tempRecord.customerNumber = results[0][2] as string;
                // tempRecord.customerName = results[0][1] as string;

                //tempRecord.customerNumber = ;

                tempRecord.partNumber = row[2] as string;
                tempRecord.partDescription = row[3] as string;
                tempRecord.timeStamp = DateTime.Now;

                transactionRecords.Add(tempRecord);
            }

            DataTable dt = ConvertTRSToDataTable(transactionRecords);

            // final schema:
            // invoice #, part no, part desc, customer no, customer name, quantity, price, date
            MainData.ItemsSource = dt.DefaultView;
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
