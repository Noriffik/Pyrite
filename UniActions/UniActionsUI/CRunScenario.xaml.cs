﻿using System;
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
using UniActionsCore.ScenarioCreation;

namespace UniActionsUI
{
    /// <summary>
    /// Interaction logic for CRunScenario.xaml
    /// </summary>
    public partial class CRunScenario : UserControl
    {
        public CRunScenario()
        {
            InitializeComponent();
        }

        private Scenario _scenario;
        public Scenario Scenario
        {
            get
            {
                return _scenario;
            }
            set
            {
                _scenario = value;
                Refresh();
            }
        }

        private void btScenarioRun_Click(object sender, RoutedEventArgs e)
        {
            Run();
            if (NeedClose != null)
                NeedClose(this, new EventArgs());
        }

        private void btScenarioRun_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Run();
        }

        public void Run()
        {
            if (_scenario != null)
            {
                btScenarioRun.IsEnabled = false;
                btScenarioRun.Content = "Выполняется...";
                _scenario.ExecuteAsync((state) =>
                {
                    btScenarioRun.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        btScenarioRun.Content = state;
                        btScenarioRun.IsEnabled = true;
                        btScenarioRun.Focus();
                    }));
                });
            }
        }

        public int Number
        {
            get
            {
                return int.Parse(lblNumber.Content.ToString());
            }
            set
            {
                lblNumber.Content = value;
            }
        }

        public void Refresh()
        {
            _scenario.CheckStateAsync((state) => btScenarioRun.Dispatcher.BeginInvoke(
                new Action(() => btScenarioRun.Content = state)));
        }

        public event Action<object, EventArgs> NeedClose;
    }
}
