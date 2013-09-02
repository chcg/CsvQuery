﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Data;
using System.Windows.Forms;

namespace CsvQuery
{
    using System.IO;

    using NppPluginNET;

    public partial class frmMyDlg : Form
    {
        public frmMyDlg()
        {
            InitializeComponent();
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            var sci = PluginBase.GetCurrentScintilla();
            var length = (int)Win32.SendMessage(sci, SciMsg.SCI_GETLENGTH, 0, 0);
            var bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            string text;
            using (Sci_TextRange tr = new Sci_TextRange(0, length, length))
            {
                Win32.SendMessage(sci, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                text = tr.lpstrText;

                //MessageBox.Show("Length: " + length + ", got: " + text.Length + " \n" + text.Substring(0, Math.Min(100, text.Length)));
            }
            var t1 = watch.ElapsedMilliseconds; watch.Restart();
            var csvSettings = CsvAnalyzer.Analyze(text);

            if(csvSettings.Separator == '\0')
            {
                MessageBox.Show("Could not figure out separator");
                return;
            }
            var t2 = watch.ElapsedMilliseconds; watch.Restart();
            dataGrid.DataSource = null;
            var table = new DataTable();
            //table.Rows.
            dataGrid.Rows.Clear();
            dataGrid.Columns.Clear();
            bool first = true;

            //var lines = text.Split('\n');
            var data = new List<string[]>();

            var textreader = new StringReader(text);
            string line;
            int columnsCount = 0;
            while ((line = textreader.ReadLine()) != null)
            //foreach (var line in lines)
            {
                var cols = line.Split(csvSettings.Separator);
                data.Add(cols);

                if (cols.Length > columnsCount)
                    columnsCount = cols.Length;

                //if(first)
                //{
                //    first = false;
                //    //for (int i = 0; i < cols.Length; i++) dataGrid.Columns.Add("col" + i, "Col" + i);
                //    for (int i = 0; i < cols.Length; i++) table.Columns.Add("Col" + i);
                //}
                //table.Rows.Add(cols);
                //dataGrid.Rows.Add(cols);
            }
            var t3 = watch.ElapsedMilliseconds; watch.Restart();


            for (int i = 0; i < columnsCount; i++) table.Columns.Add("Col" + i);
            foreach (var cols in data)
            {
                table.Rows.Add(cols);
            }
            var t5 = watch.ElapsedMilliseconds; watch.Restart();

            dataGrid.DataSource = table;
            var t4 = watch.ElapsedMilliseconds; watch.Restart();

            //dataGrid.Columns.Add("col1", "Column 1");
            //dataGrid.Columns.Add("col2", "Column 2");
            //dataGrid.Rows.Add(new[] { "hej", "san" });
            //dataGrid.Rows.Add(new[] { "qwe", "rty" });
            dataGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            watch.Stop();
            MessageBox.Show("Times: \nGetText: "+t1+"ms\nAnalyze: "+t2+"ms\nTable: "+t3+"ms\nDatabind: "+t4+"ms\nResize: "+watch.ElapsedMilliseconds + "ms\nBuffer ID: " + bufferId );
        }
    }
}