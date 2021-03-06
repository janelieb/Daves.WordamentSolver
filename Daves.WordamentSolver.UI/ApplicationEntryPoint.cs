﻿using Daves.WordamentSolver.UI.Presenters;
using Daves.WordamentSolver.UI.Views;
using System;
using System.Windows.Forms;

namespace Daves.WordamentSolver.UI
{
    internal static class ApplicationEntryPoint
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Solution.SetDictionary();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            var mainForm = new SolverForm();
            var mainFormPresenter = new SolverPresenter(mainForm);
            Application.Run(mainForm);
        }
    }
}
