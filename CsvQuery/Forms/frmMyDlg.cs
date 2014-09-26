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
    using System.Linq;

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
            var codepage = (int)Win32.SendMessage(sci, SciMsg.SCI_GETCODEPAGE, 0, 0);
            var bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            string text;
            using (Sci_TextRange tr = new Sci_TextRange(0, length, length))
            {
                Win32.SendMessage(sci, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);

                switch (codepage)
                {
                    case (int)SciMsg.SC_CP_UTF8:
                        text = tr.GetFromUtf8();
                        break;
                    case (int)SciMsg.SC_CP_DBCS: // Double Byte Character Set, like unicode (utf16) - this never seems to happen, when the text is utc-2 we get SC_CP_UTF8 although that is a lie!
                        text = tr.GetFromUnicode();
                        break;
                    case 0: // ansi?
                        text = tr.lpstrText;
                        break;
                    default:
                        text = tr.lpstrText;
                        break;
                }

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

            dataGrid.Rows.Clear();
            dataGrid.Columns.Clear();

            var data = csvSettings.Parse(text);
            var t3 = watch.ElapsedMilliseconds; watch.Restart();

            DataStorage.SaveData(bufferId, data, null);
            var t_saveToDb = watch.ElapsedMilliseconds; watch.Restart();
            DataStorage.SetActiveTab(bufferId);
            var toshow = DataStorage.ExecuteQueryWithColumnNames("SELECT * FROM THIS");
            var t_getFromDb = watch.ElapsedMilliseconds; watch.Restart();

            //for (int i = 0; i < columnsCount; i++) table.Columns.Add("Col" + i);
            //foreach (var cols in toshow)
            //{
            //    table.Rows.Add(cols);
            //}

            // Create columns
            foreach (var s in toshow[0])
            {
                table.Columns.Add(s);
            }

            // Insert rows
            foreach (var row in toshow.Skip(1))
            {
                table.Rows.Add(row);
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
            MessageBox.Show("Times: \nGetText: " + t1 + "ms\nAnalyze: " + t2 + "ms\nParse: " + t3 + "ms\nTo table:" + t5 + "ms\nDatabind: " +
                            t4 + "ms\nResize: " + watch.ElapsedMilliseconds + "ms\nBuffer ID: " + bufferId +
                            "\nSave to DB: " + t_saveToDb + "ms\nLoad from DB: " + t_getFromDb + "ms");
        }

        private void btnExec_Click(object sender, EventArgs e)
        {
            var sci = PluginBase.GetCurrentScintilla();
            var bufferId = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            DataStorage.SetActiveTab(bufferId);

            var table = new DataTable();
            var query = txbQuery.Text;
            List<string[]> toshow;
            try
            {
                toshow = DataStorage.ExecuteQueryWithColumnNames(query);
            }
            catch (Exception)
            {
                MessageBox.Show("Could not execute query", "Error in query");
                return;
            }

            // Create columns
            foreach (var s in toshow[0])
            {
                table.Columns.Add(s);
            }

            // Insert rows
            foreach (var row in toshow.Skip(1))
            {
                table.Rows.Add(row);
            }

            dataGrid.DataSource = table;
            dataGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void txbQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                btnExec.PerformClick();
            }
        }
    }
}
