using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackupManager
{
    public partial class Form1 : Form
    {
        private List<BackedPath> m_BackUpsCollection = new List<BackedPath>();
        public Form1()
        {
            InitializeComponent();
            //Load Serialized
            try
            {
                using (var reader = new StreamReader(Application.StartupPath + "\\SavedPaths.dat"))
                {
                    if (reader.BaseStream.Length > 0)
                    {
                        m_BackUpsCollection = new List<BackedPath>(new BinaryFormatter().Deserialize(reader.BaseStream) as BackedPath[]);
                        for (int i = 0, length = m_BackUpsCollection.Count; i < length; i++)
                        {
                            dataGridView1.Rows.Insert(i, m_BackUpsCollection[i].Name, PathsToSingleString(m_BackUpsCollection[i].Paths), PathsToSingleString(m_BackUpsCollection[i].Targets));
                            dataGridView1.Rows[i].Cells[3].Value = "Backup";
                        }
                    }
                    reader.Close();
                }
            }
            catch { }
            dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
        }
        private string PathsToSingleString(string[] paths)
        {
            string result = "";
            for (int i = 0, length = paths.Length; i < length; i++)
            {
                result += paths[i] + ((i < length-1) ?",":null);
            }
            return result;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_BackUpsCollection.Count; i++)
            {
                SerializeData();
                BackUp(i);
            }
        }
        private void BackUp(int rowIndex)
        {
            try
            {
                CompressFile(m_BackUpsCollection[rowIndex]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public void CompressFile(BackedPath bp)
        {
            using (ZipFile zip = new ZipFile())
            {
                Task.Factory.StartNew(() =>
                {
                    zip.SaveProgress += Zip_SaveProgress;
                    for (int i = 0; i < bp.Paths.Length; i++)
                    {
                        zip.UseUnicodeAsNecessary = true;
                        zip.AddDirectory((bp.Paths[i]), Path.GetFileName(bp.Paths[i]));
                    }
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.Comment = "Backup Manager file generated at " + System.DateTime.Now.ToString("G");
                    foreach (var target in bp.Targets)
                    {
                        zip.Save(string.Format("{0}.zip", target + "\\" + bp.Name + "---" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString().Replace(':', '-')));
                        DirectoryInfo info = new DirectoryInfo(target);
                        FileInfo[] files = info.GetFiles().Where(x => x.Name.Contains(bp.Name)).OrderBy(p => p.CreationTime).ToArray();

                        for (int i = 0, length = files.Length - 5; i < length; i++)
                        {
                            File.Delete(files[i].FullName);
                            Console.WriteLine(string.Format("File: {0} - Date:{1} Deleted.", files[i].FullName, files[i].LastWriteTime));
                        }
                    }
                });
            }
        }

        private void Zip_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_EntryBytesRead)
            {
                progressBar1.Invoke(new MethodInvoker(delegate { progressBar1.Value = (int)((e.BytesTransferred * 100) / e.TotalBytesToTransfer); }));
            }
            else if (e.EventType == ZipProgressEventType.Saving_Completed)
            {
                MessageBox.Show("Done: " + e.ArchiveName);
            }
        }
        private void SerializeData()
        {
            dataGridView1.CellEndEdit -= dataGridView1_CellEndEdit;
            m_BackUpsCollection.Clear();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[1].Value == null || dataGridView1.Rows[i].Cells[2].Value == null)
                    break;
                string[] paths = dataGridView1.Rows[i].Cells[1].Value.ToString().Split(';');
                string[] targets = dataGridView1.Rows[i].Cells[2].Value.ToString().Split(';');
                
                if (dataGridView1.Rows[i].Cells[0].Value != null && (dataGridView1.Rows[i].Cells[1].Value != null && PathsExists(paths)) 
                    && (dataGridView1.Rows[i].Cells[2].Value != null && PathsExists(targets)))
                {
                    m_BackUpsCollection.Add(new BackedPath(dataGridView1.Rows[i].Cells[0].Value.ToString(), dataGridView1.Rows[i].Cells[1].Value.ToString().Split(';'), dataGridView1.Rows[i].Cells[2].Value.ToString().Split(';')));
                    dataGridView1.Rows[i].Cells[3].Value = "Backup";
                }
            }
            //Serialize
            using (var writer = new FileStream(Application.StartupPath + "\\SavedPaths.dat",FileMode.OpenOrCreate,FileAccess.ReadWrite)) {
                new BinaryFormatter().Serialize(writer, m_BackUpsCollection.ToArray());
                writer.Close();
            }
            dataGridView1.CellEndEdit += dataGridView1_CellEndEdit;
        }
        private bool PathsExists(string[] paths)
        {
            for (int j = 0, length = paths.Length; j < length; j++)
            {
                if (!Directory.Exists(paths[j]))
                    return false;
            }
            return true;
        }
        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.ColumnIndex > 0 &&  e.ColumnIndex < 3)
            {
                selectedColumnIndex = e.ColumnIndex;
                selectedRowIndex = e.RowIndex;
                folderContextMenuStrip.Show(this.ActiveControl, this.ActiveControl.PointToClient(Cursor.Position));
            }
            // Set various options for the context menu
        }

        private int selectedRowIndex, selectedColumnIndex;

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows[selectedRowIndex].Cells[selectedColumnIndex].Value = null;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            Console.WriteLine(e.ColumnIndex);
            var senderGrid = (DataGridView)sender;
            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0)
            {
                SerializeData();
                BackUp(e.RowIndex);
            }
        }

        private void openLocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows[selectedRowIndex].Cells[selectedColumnIndex].Value == null)
                return;
            string[] splitted = dataGridView1.Rows[selectedRowIndex].Cells[selectedColumnIndex].Value.ToString().Split(';');
            for (int i = 0, length = splitted.Length; i < length; i++)
            {
                Process.Start("explorer.exe ", splitted[i]);
            }
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            SerializeData();
        }


        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            Console.WriteLine(e.RowIndex);
            if (e.Button == MouseButtons.Right)
            {
                selectedColumnIndex = e.ColumnIndex;
                selectedRowIndex = e.RowIndex;
                rowContextMenuStrip.Show(this.ActiveControl, this.ActiveControl.PointToClient(Cursor.Position));
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.Rows.RemoveAt(selectedRowIndex);
                SerializeData();
            }
            catch { }
        }

        private void addFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog
            {
                Description = "Select a directory to add.",
                ShowNewFolderButton = true,
                SelectedPath = Properties.Settings.Default.Folder_Path,
            };
            if (folderDialog.ShowDialog(this.Owner) == DialogResult.OK)
            {
                if (dataGridView1.Rows[selectedRowIndex].Cells[selectedColumnIndex].Value != null)
                    dataGridView1.Rows[selectedRowIndex].Cells[selectedColumnIndex].Value += ";" + folderDialog.SelectedPath;
                else
                    dataGridView1.Rows[selectedRowIndex].Cells[selectedColumnIndex].Value = folderDialog.SelectedPath;
                SerializeData();
                Properties.Settings.Default.Folder_Path = folderDialog.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }
    }
}
