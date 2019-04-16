﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiztinGUIsh
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            table.Height = this.Height - 85;
            table.Width = this.Width - 33;
            vScrollBar1.Height = this.Height - 85;
            vScrollBar1.Left = this.Width - 33;
            if (WindowState == FormWindowState.Maximized) UpdateDataGridView();
        }

        private void MainWindow_ResizeEnd(object sender, EventArgs e)
        {
            UpdateDataGridView();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            table.CellValueNeeded += new DataGridViewCellValueEventHandler(dataGridView1_CellValueNeeded);
            table.CellValuePushed += new DataGridViewCellValueEventHandler(dataGridView1_CellValuePushed);
            viewOffset = 0;
            rowsToShow = ((table.Height - table.ColumnHeadersHeight) / table.RowTemplate.Height);
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null,
                table,
                new object[] { true });
        }

        public void UpdateWindowTitle()
        {
            this.Text =
                (Project.unsavedChanges ? "*" : "") + 
                (Project.currentFile == null ? "New Project" : Project.currentFile) +
                " - DiztinGUIsh";
        }

        private bool ContinueUnsavedChanges()
        {
            if (Project.unsavedChanges)
            {
                DialogResult confirm = MessageBox.Show("You have unsaved changes. They will be lost if you continue.", "Unsaved Changes", MessageBoxButtons.OKCancel);
                return confirm == DialogResult.OK;
            }
            return true;
        }

        public void TriggerSaveOptions(bool save, bool saveas)
        {
            saveProjectToolStripMenuItem.Enabled = save;
            saveProjectAsToolStripMenuItem.Enabled = saveas;
        }

        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ContinueUnsavedChanges())
            {
                DialogResult result = openFileDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    if (Project.NewProject(openFileDialog1.FileName))
                    {
                        TriggerSaveOptions(false, true);
                        UpdateWindowTitle();
                        UpdateDataGridView();
                        table.Invalidate();
                    }
                }
            }
        }

        private void openProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ContinueUnsavedChanges())
            {
                DialogResult result = openFileDialog2.ShowDialog();
                if (result == DialogResult.OK)
                {
                    if (Project.TryOpenProject(openFileDialog2.FileName))
                    {
                        TriggerSaveOptions(true, true);
                        UpdateWindowTitle();
                        UpdateDataGridView();
                        table.Invalidate();
                    }
                }
            }
        }

        private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Project.SaveProject(Project.currentFile);
            UpdateWindowTitle();
        }

        private void saveProjectAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK && saveFileDialog1.FileName != "")
            {
                Project.SaveProject(saveFileDialog1.FileName);
                TriggerSaveOptions(true, true);
                UpdateWindowTitle();
            }
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, Application.StartupPath + @"\" + "Help.chm");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ContinueUnsavedChanges())
            {
                Application.Exit();
            }
        }

        private Util.NumberBase DisplayBase = Util.NumberBase.Hexadecimal;
        private Data.FlagType markFlag = Data.FlagType.Data8Bit;

        private void decimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateBase(Util.NumberBase.Decimal);
        }

        private void hexadecimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateBase(Util.NumberBase.Hexadecimal);
        }

        private void binaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateBase(Util.NumberBase.Binary);
        }

        private void UpdateBase(Util.NumberBase noBase)
        {
            DisplayBase = noBase;
            decimalToolStripMenuItem.Checked = noBase == Util.NumberBase.Decimal;
            hexadecimalToolStripMenuItem.Checked = noBase == Util.NumberBase.Hexadecimal;
            binaryToolStripMenuItem.Checked = noBase == Util.NumberBase.Binary;
            table.Invalidate();
        }

        public void UpdatePercent()
        {
            int totalUnreached = 0, size = Data.GetROMSize();
            for (int i = 0; i < size; i++) if (Data.GetFlag(i) == Data.FlagType.Unreached) totalUnreached++;
            percentComplete.Text = string.Format("{0:N3}%", (size - totalUnreached) * 100.0 / size);
        }

        // DataGridView

        private int viewOffset;
        private int rowsToShow;

        private void UpdateDataGridView()
        {
            if (Data.GetROMSize() > 0)
            {
                rowsToShow = ((table.Height - table.ColumnHeadersHeight) / table.RowTemplate.Height);
                if (viewOffset + rowsToShow > Data.GetROMSize()) viewOffset = Data.GetROMSize() - rowsToShow;
                if (viewOffset < 0) viewOffset = 0;
                vScrollBar1.Enabled = true;
                vScrollBar1.Maximum = Data.GetROMSize() - rowsToShow;
                vScrollBar1.Value = viewOffset;
                table.RowCount = rowsToShow;
            }
        }

        private void dataGridView1_MouseWheel(object sender, MouseEventArgs e)
        {
            int selRow = table.CurrentCell.RowIndex + viewOffset, selCol = table.CurrentCell.ColumnIndex;
            int amount = e.Delta / 0x18;
            viewOffset -= amount;
            UpdateDataGridView();
            if (selRow < viewOffset) selRow = viewOffset;
            else if (selRow >= viewOffset + rowsToShow) selRow = viewOffset + rowsToShow - 1;
            table.CurrentCell = table.Rows[selRow - viewOffset].Cells[selCol];
            table.Invalidate();
        }

        private void vScrollBar1_ValueChanged(object sender, EventArgs e)
        {
            int selOffset = table.CurrentCell.RowIndex + viewOffset;
            viewOffset = vScrollBar1.Value;
            UpdateDataGridView();

            if (selOffset < viewOffset) table.CurrentCell = table.Rows[0].Cells[table.CurrentCell.ColumnIndex];
            else if (selOffset >= viewOffset + rowsToShow) table.CurrentCell = table.Rows[rowsToShow - 1].Cells[table.CurrentCell.ColumnIndex];
            else table.CurrentCell = table.Rows[selOffset - viewOffset].Cells[table.CurrentCell.ColumnIndex];

            table.Invalidate();
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (Data.GetROMSize() <= 0) return;

            int offset = table.CurrentCell.RowIndex + viewOffset;
            int newOffset = offset;
            int amount = 0x01;

            Console.WriteLine(e.KeyCode);
            switch (e.KeyCode)
            {
                case Keys.Home:
                case Keys.PageUp:
                case Keys.Up:
                    amount = e.KeyCode == Keys.Up ? 0x01 : e.KeyCode == Keys.PageUp ? 0x10 : 0x100;
                    newOffset = offset - amount;
                    if (newOffset < 0) newOffset = 0;
                    SelectOffset(newOffset);
                    break;
                case Keys.End:
                case Keys.PageDown:
                case Keys.Down:
                    amount = e.KeyCode == Keys.Down ? 0x01 : e.KeyCode == Keys.PageDown ? 0x10 : 0x100;
                    newOffset = offset + amount;
                    if (newOffset >= Data.GetROMSize()) newOffset = Data.GetROMSize() - 1;
                    SelectOffset(newOffset);
                    break;
                case Keys.Left:
                    amount = table.CurrentCell.ColumnIndex;
                    amount = amount - 1 < 0 ? 0 : amount - 1;
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[amount];
                    break;
                case Keys.Right:
                    amount = table.CurrentCell.ColumnIndex;
                    amount = amount + 1 >= table.ColumnCount ? table.ColumnCount - 1 : amount + 1;
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[amount];
                    break;
                case Keys.S: Step(offset); break;
                case Keys.I: StepIn(offset); break;
                case Keys.A: AutoStepSafe(offset); break;
                case Keys.F: GoToEffectiveAddress(offset); break;
                case Keys.U: GoToUnreached(true); break;
                case Keys.N: GoToUnreached(false); break;
                case Keys.K: Mark(offset, 1); break;
                case Keys.L:
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[0];
                    table.BeginEdit(true);
                    break;
                case Keys.B:
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[8];
                    table.BeginEdit(true);
                    break;
                case Keys.D:
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[9];
                    table.BeginEdit(true);
                    break;
                case Keys.M:
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[10];
                    table.BeginEdit(true);
                    break;
                case Keys.X:
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[11];
                    table.BeginEdit(true);
                    break;
                case Keys.C:
                    table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[12];
                    table.BeginEdit(true);
                    break;
            }
            e.Handled = true;
            table.Invalidate();
        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            int row = e.RowIndex + viewOffset;
            if (row >= Data.GetROMSize()) return;
            switch (e.ColumnIndex)
            {
                case 0: e.Value = Data.GetLabel(row); break;
                case 1: e.Value = Util.NumberToBaseString(Util.ConvertPCtoSNES(row), Util.NumberBase.Hexadecimal, 6); break;
                case 2: e.Value = (char)Data.GetROMByte(row); break;
                case 3: e.Value = Util.NumberToBaseString(Data.GetROMByte(row), DisplayBase); break;
                case 4: e.Value = Util.PointToString(Data.GetInOutPoint(row)); break;
                case 5: e.Value = Util.GetInstruction(row); break; // TODO
                case 6:
                    int ea = Util.GetEffectiveAddress(row);
                    if (ea >= 0) e.Value = Util.NumberToBaseString(ea, Util.NumberBase.Hexadecimal, 6);
                    else e.Value = "";
                    break;
                case 7: e.Value = Util.TypeToString(Data.GetFlag(row)); break;
                case 8: e.Value = Util.NumberToBaseString(Data.GetDataBank(row), Util.NumberBase.Hexadecimal, 2); break;
                case 9: e.Value = Util.NumberToBaseString(Data.GetDirectPage(row), Util.NumberBase.Hexadecimal, 4); break;
                case 10: e.Value = Util.BoolToSize(Data.GetMFlag(row)); break;
                case 11: e.Value = Util.BoolToSize(Data.GetXFlag(row)); break;
                case 12: e.Value = Data.GetComment(row); break;
            }
        }

        private void dataGridView1_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            string value = e.Value as string;
            int result;
            int row = e.RowIndex + viewOffset;
            if (row >= Data.GetROMSize()) return;
            switch (e.ColumnIndex)
            {
                case 0: Data.AddLabel(row, value); break;
                case 8: if (int.TryParse(value, NumberStyles.HexNumber, null, out result)) Data.SetDataBank(row, result); break;
                case 9: if (int.TryParse(value, NumberStyles.HexNumber, null, out result)) Data.SetDirectPage(row, result); break;
                case 10: Data.SetMFlag(row, (value == "8" || value == "M")); break;
                case 11: Data.SetXFlag(row, (value == "8" || value == "X")); break;
                case 12: Data.AddComment(row, value); break;
            }
            table.InvalidateRow(e.RowIndex);
        }

        private void SelectOffset(int offset, int column = -1)
        {
            int col = column == -1 ? table.CurrentCell.ColumnIndex : column;
            if (offset < viewOffset)
            {
                viewOffset = offset;
                UpdateDataGridView();
                table.CurrentCell = table.Rows[0].Cells[col];
            } else if (offset >= viewOffset + rowsToShow)
            {
                viewOffset = offset - rowsToShow + 1;
                UpdateDataGridView();
                table.CurrentCell = table.Rows[rowsToShow - 1].Cells[col];
            } else
            {
                table.CurrentCell = table.Rows[offset - viewOffset].Cells[col];
            }
        }

        private void Step(int offset)
        {
            SelectOffset(Manager.Step(offset, false));
        }

        private void StepIn(int offset)
        {
            SelectOffset(Manager.Step(offset, true));
        }

        private void AutoStepSafe(int offset)
        {
            SelectOffset(Manager.AutoStep(offset, false));
        }

        private void AutoStepHarsh(int offset)
        {
            SelectOffset(Manager.AutoStep(offset, true));
        }

        private void Mark(int offset, int count)
        {
            SelectOffset(Manager.Mark(offset, markFlag, count));
        }

        private void GoToEffectiveAddress(int offset)
        {
            int ea = Util.GetEffectiveAddress(offset);
            if (ea >= 0)
            {
                int pc = Util.ConvertSNEStoPC(ea);
                if (pc >= 0)
                {
                    SelectOffset(pc, 1);
                }
            }
        }

        private void GoToUnreached(bool first)
        {
            int unreached = 0;
            if (first)
            {
                for (int i = 0; i < Data.GetROMSize(); i++)
                {
                    if (Data.GetFlag(i) == Data.FlagType.Unreached)
                    {
                        unreached = i;
                        break;
                    }
                }
            } else
            {
                bool inBlock = false;
                for (int i = table.CurrentCell.RowIndex + viewOffset; i >= 0; i--)
                {
                    if (inBlock && Data.GetFlag(i) != Data.FlagType.Unreached)
                    {
                        unreached = i - 1;
                        break;
                    }
                    if (Data.GetFlag(i) == Data.FlagType.Unreached && !inBlock) inBlock = true;
                }
            }
            SelectOffset(unreached, 1);
        }

        private void visualMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // visual map window
        }

        private void graphicsWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // graphics view window
        }

        private void stepOverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Data.GetROMSize() <= 0) return;
            Step(table.CurrentCell.RowIndex + viewOffset);
        }

        private void stepInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Data.GetROMSize() <= 0) return;
            StepIn(table.CurrentCell.RowIndex + viewOffset);
        }

        private void autoStepSafeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Data.GetROMSize() <= 0) return;
            AutoStepSafe(table.CurrentCell.RowIndex + viewOffset);
        }

        private void autoStepHarshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Data.GetROMSize() <= 0) return;
            AutoStepHarsh(table.CurrentCell.RowIndex + viewOffset);
        }

        private void gotoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // goto window
        }

        private void gotoEffectiveAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Data.GetROMSize() <= 0) return;
            GoToEffectiveAddress(table.CurrentCell.RowIndex + viewOffset);
        }

        private void gotoFirstUnreachedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GoToUnreached(true);
        }

        private void gotoNearUnreachedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GoToUnreached(false);
        }

        private void markOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Data.GetROMSize() <= 0) return;
            Mark(table.CurrentCell.RowIndex + viewOffset, 1);
        }

        private void markManyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // mark many window
        }

        private void addLabelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[0];
            table.BeginEdit(true);
        }

        private void setDataBankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // set many data bank window
        }

        private void setDirectPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // set many direct page window
        }

        private void toggleAccumulatorSizeMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // set many M flag window
        }

        private void toggleIndexSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // set many X flag window
        }

        private void addCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            table.CurrentCell = table.Rows[table.CurrentCell.RowIndex].Cells[12];
            table.BeginEdit(true);
        }

        private void fixMisalignedInstructionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO
            // fix misaligned instructions dialog
        }

        private void unreachedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Unreached;
        }

        private void opcodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Opcode;
        }

        private void operandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Operand;
        }

        private void bitDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Data8Bit;
        }

        private void graphicsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Graphics;
        }

        private void musicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Music;
        }

        private void emptyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Empty;
        }

        private void bitDataToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Data16Bit;
        }

        private void wordPointerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Pointer16Bit;
        }

        private void bitDataToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Data24Bit;
        }

        private void longPointerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Pointer24Bit;
        }

        private void bitDataToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Data32Bit;
        }

        private void dWordPointerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Pointer32Bit;
        }

        private void textToolStripMenuItem_Click(object sender, EventArgs e)
        {
            markFlag = Data.FlagType.Text;
        }
    }
}